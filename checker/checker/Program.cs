using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Mono.Cecil;

namespace checker
{
	static class Program
	{
		static int Main(string[] args)
		{
			try
			{
				return Work(args);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return -1;
			}
		}

		static int Work(string[] args)
		{
			if (args.Length > 0 && (args[0].ToLowerInvariant() == "-dump" || args[0].ToLowerInvariant() == "-check"))
			{
				//TODO: improve detection of dirs
				List<string> files = args.Where(a => a.ToLowerInvariant().EndsWith(".dll") || a.ToLowerInvariant().EndsWith(".exe")).ToList();
				IEnumerable<string> dirs = args.Where(a => a == "." || a.EndsWith(@"\") /*|| a.IndexOf('\\') > a.IndexOf('.')*/);
				string xmlSrc = args.SingleOrDefault(a => a.ToLowerInvariant().EndsWith(".xml")) ?? "prototypes.xml";

				foreach (string dir in dirs)
				{
					bool recursive = dir.ToLowerInvariant().StartsWith("-r");
					string dirPath = Path.GetFullPath(recursive ? dir.Substring(2) : dir);
					files.AddRange(Directory.GetFiles(dirPath, "*.dll", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
					files.AddRange(Directory.GetFiles(dirPath, "*.exe", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
				}

				if (files.Count() <= 0)
				{
					Usage();
				}

				XElement assemblies = MakeDumps(files);

				if (args[0].ToLowerInvariant() == "-dump")
				{
					assemblies.ProperSave(xmlSrc);
					return 0;
				}

				if (args[0].ToLowerInvariant() == "-check")
				{
					string reportFile = args.SingleOrDefault(a => a.ToLowerInvariant().StartsWith("report:"));
					string patchFile = args.SingleOrDefault(a => a.ToLowerInvariant().StartsWith("patch:"));

					string xmlSrcFileName = Path.ChangeExtension(xmlSrc, null);
					string defaultReportFile = Path.GetFullPath(String.Format(@"{0}-report.xml", xmlSrcFileName));
					string defaultPatchFile = Path.GetFullPath(String.Format(@"{0}-patch.xml", xmlSrcFileName));

					XElement storedAssemblies = XElement.Load(xmlSrc);

					patchFile = String.IsNullOrEmpty(patchFile)
										? defaultPatchFile
										: patchFile.Substring("patch:".Length);

					if (File.Exists(patchFile))
					{
						XElement xmlPatch = XElement.Load(patchFile);
						ApplyPatch(storedAssemblies, xmlPatch);
					}

					CheckAssemblies(storedAssemblies.Elements("Assembly"), assemblies.Elements("Assembly"));
					GenerateReport(storedAssemblies);
					storedAssemblies.ProperSave(String.IsNullOrEmpty(reportFile) ? defaultReportFile : reportFile.Substring("report:".Length));

					if (storedAssemblies.HasElements)
					{
						Console.WriteLine("Compatibility test failed!");
						Console.WriteLine("Problems are:");

						foreach (XElement problem in storedAssemblies.Elements())
						{
							Console.WriteLine(String.Format("{0} {1}", problem.Name, String.Join(" ", problem.Attributes().Select(a => a.ToString()))));
						}
						return 1;
					}
					else
					{
						Console.WriteLine("Compatibility test passed!");
						return 0;
					}
				}
			}

			Usage();
			return -1;
		}

		private static void Usage()
		{
			Console.WriteLine("Assembly compatibility checker for .NET");
			Console.WriteLine();
			Console.WriteLine("Usage: AsmChecker.exe -dump|-check files|directories [dump file] [options]");
			Console.WriteLine();
			Console.WriteLine("First argument specifies the action:");
			Console.WriteLine("\tDUMP assembly members signatures to a file, or\n\tCHECK assemblies compatibility with some existing dump");
			Console.WriteLine();
			Console.Write("Then you must specify at least one assembly file name, or directory,\nwhere it can be located. ");
			Console.Write("Directory path should always end with \\ symbol.\nIf you specify \"-d\" right berore it - the program will search for the files\nin subdirectories too.\n\n");
			Console.Write("After that you can supply a path to xml file, where the dump will be stored, or where to read it from (depends on specified action). ");
			Console.Write("If no file supplied - \nthe default will be used (prototypes.xml in the application folder)\n\n");
			Console.WriteLine("Options are for checking only.");
			Console.WriteLine("patch:file.xml - path to patch file. Default is %dump%-patch.xml");
			Console.WriteLine("report:file.xml - path to report file. Default is %dump%-report.xml");
			Console.WriteLine("Where %dump% - is a full name of dump file without extension");
		}

		#region Dump

		private static XElement MakeDumps(IEnumerable<string> fileList)
		{
			IEnumerable<AssemblyDefinition> asmDefinitions =
				fileList.Select(f => AssemblyDefinition.ReadAssembly(f)).Where(it => it != null);

			XElement dumpXml = new XElement("CompatibilityInfo");
			foreach (AssemblyDefinition assembly in asmDefinitions)
			{
				XElement assemblyXml = DumpAssembly(assembly);
				if (assemblyXml.HasElements)
					dumpXml.Add(assemblyXml);
			}

			return dumpXml;
		}

		private static XElement DumpAssembly(AssemblyDefinition source)
		{
			IEnumerable<TypeDefinition> sourceTypes;

			try
			{
				sourceTypes = source.Modules.SelectMany(m => m.Types).Where(t => t.IsPublic);
			}
			catch (Exception)
			{
				//we don't care about broken or empty files
				return null;
			}

			XElement asmXml = new XElement("Assembly");

			asmXml.SetAttributeValue("Name", source.Name.Name);
			asmXml.SetAttributeValue("Info", source.FullName);

			foreach (TypeDefinition typeDefinition in sourceTypes)
			{
				asmXml.Add(DumpType(typeDefinition));
			}

			return asmXml;
		}

		private static XElement DumpType(TypeDefinition type)
		{
			string typeType;

			if (type.IsInterface)
				typeType = "Interface";
			else if (type.IsEnum)
				typeType = "Enum";
			else if (type.IsClass)
				typeType = "Class";
			else if (type.IsValueType)
				typeType = "Struct";
			else
				typeType = "Type";

			XElement typeXml = new XElement(typeType);
			typeXml.SetAttributeValue("Name", type.CorrectName());


			if (type.IsClass && type.IsSealed && type.IsAbstract)
			{
				typeXml.SetAttributeValue("Static", "true");
			}
			else if (type.IsClass && type.IsAbstract)
			{
				typeXml.SetAttributeValue("Abstract", "true");
			}

			if (!type.IsNested)
				typeXml.SetAttributeValue("Path", type.Namespace);

			//adding fields
			foreach (FieldDefinition field in type.Fields.Where(f => f.IsPublic))
			{
				XElement fieldXml = new XElement("Field");
				fieldXml.SetAttributeValue("Name", field.Name);
				fieldXml.SetAttributeValue("Static", field.IsStatic ? "true" : null);

				if (type.IsEnum)
				{
					fieldXml.SetAttributeValue("Value", field.Constant);
				}
				else
				{
					fieldXml.SetAttributeValue("Type", field.FieldType);
				}
				typeXml.Add(fieldXml);
			}

			//adding methods
			foreach (MethodDefinition method in type.Methods.Where(m => m.IsPublic && !m.IsGetter && !m.IsSetter))
			{
				typeXml.Add(DumpMethod(method));
			}

			//adding properties
			foreach (PropertyDefinition property in type.Properties.Where(p => (p.GetMethod != null && p.GetMethod.IsPublic) || (p.SetMethod != null && p.SetMethod.IsPublic)))
			{
				XElement propertyXml = new XElement("Property");
				propertyXml.SetAttributeValue("Name", property.Name);
				propertyXml.SetAttributeValue("Type", property.PropertyType);

				if (property.GetMethod != null)
				{
					propertyXml.Add(DumpMethod(property.GetMethod));
				}
				if (property.SetMethod != null)
				{
					propertyXml.Add(DumpMethod(property.SetMethod));
				}
				typeXml.Add(propertyXml);
			}

			//adding nested types
			foreach (TypeDefinition nestedType in type.NestedTypes.Where(m => m.IsPublic))
			{
				typeXml.Add(DumpType(nestedType));
			}
			return typeXml;
		}

		private static XElement DumpMethod(MethodDefinition method)
		{
			XElement methodXml = new XElement("Method");
			methodXml.SetAttributeValue("Name", method.Name + method.GenericsToString());
			methodXml.SetAttributeValue("ReturnType", method.ReturnType);
			methodXml.SetAttributeValue("Static", method.IsStatic ? "true" : null);
			methodXml.SetAttributeValue("Virtual", method.IsVirtual ? "true" : null);
			methodXml.SetAttributeValue("Override", method.IsVirtual && !method.IsNewSlot ? "true" : null);

			if (method.HasParameters)
			{
				XElement paramsXml = new XElement("Parameters");
				foreach (ParameterDefinition parameter in method.Parameters)
				{
					XElement paramXml = new XElement("Parameter");
					paramXml.SetAttributeValue("Name", parameter.Name);
					paramXml.SetAttributeValue("Type", parameter.ParameterType);
					paramsXml.Add(paramXml);
				}
				methodXml.Add(paramsXml);
			}
			return methodXml;
		}

		private static void ApplyPatch(XElement source, XElement patch)
		{
			if (patch == null)
				return;

			foreach (XAttribute attribute in patch.Attributes())
			{
				source.SetAttributeValue(attribute.Name, attribute.Value);
			}

			foreach (XElement element in source.Elements())
			{
				ApplyPatch(element, patch.Elements(element.Name.LocalName).SingleOrDefault(e => AreCompatible(element, e)));
			}
		}

		#endregion

		#region Check

		private static void CheckAssemblies(IEnumerable<XElement> first, IEnumerable<XElement> second)
		{
			foreach (XElement assembly in first.Where(a => !IsUntouchable(a)))
			{
				XElement analogInSecond = second.FirstOrDefault(a => AreCompatible(assembly, a));

				if (analogInSecond == null)
				{
					assembly.SetAttributeValue("Compatible", "false");
					continue;
				}

				CheckTypes(assembly.SelectTypes().Where(e => !IsUntouchable(e)), analogInSecond.SelectTypes());
			}
		}

		private static void CheckTypes(IEnumerable<XElement> first, IEnumerable<XElement> second)
		{
			foreach (XElement type in first)
			{
				XElement analogInSecond = second.FirstOrDefault(t => AreCompatible(type, t));

				if (analogInSecond == null)
				{
					type.SetAttributeValue("Compatible", "false");
					continue;
				}

				foreach (XElement member in type.Elements().Where(t => !IsUntouchable(t)))
				{
					if (!analogInSecond.Elements(member.Name.LocalName).Any(m => AreCompatible(member, m)))
					{
						member.SetAttributeValue("Compatible", "false");
					}
				}

				CheckTypes(type.Elements("Type").Where(t => !IsUntouchable(t)), analogInSecond.Elements("Type"));
			}
		}

		#region Compatibility checks

		private static bool IsUntouchable(XElement e)
		{
			return e.GetValue("Compatible") == "true";
		}

		//One generic compatibility check for all. Needed for reports and stuff
		private static bool AreCompatible(XElement first, XElement second)
		{
			bool compatible =
				//whatever: check tag names 
				first.Name.LocalName == second.Name.LocalName &&
				//whatever: check names 
				first.GetValue("Name") == second.GetValue("Name") &&
				//whatever: static or not
				first.GetValue("Static") == second.GetValue("Static") &&
				//classes: abstract or not
				first.GetValue("Abstract") == second.GetValue("Abstract") &&
				//fields&properties: check type
				first.GetValue("Type") == second.GetValue("Type") &&
				//methods: check return type
				first.GetValue("ReturnType") == second.GetValue("ReturnType") &&
				//methods: virtual or not
				first.GetValue("Virtual") == second.GetValue("Virtual") &&
				//methods: override
				first.GetValue("Override") == second.GetValue("Override") &&
				//properties: getter and setter 
				!(first.GetValue("Getter") == "public" && first.GetValue("Getter") != second.GetValue("Getter")) &&
				!(first.GetValue("Setter") == "public" && first.GetValue("Setter") != second.GetValue("Setter")) &&
				//enums: check value
				first.GetValue("Value") == second.GetValue("Value");

			if (!compatible)
			{
				return false;
			}

			//methods: check parameters
			if (first.Element("Parameters") == null ^ second.Element("Parameters") == null)
			{
				return false;
			}
			else if (first.Element("Parameters") != null)
			{
				IEnumerable<string> params1 = first.Element("Parameters").Elements("Parameter").Select(m => m.Attribute("Type").Value).ToArray();
				IEnumerable<string> params2 = second.Element("Parameters").Elements("Parameter").Select(m => m.Attribute("Type").Value).ToArray();

				return Enumerable.SequenceEqual(params1, params2);
			}

			//Check properties acessors
			if (first.Name.LocalName == "Property" && second.Name.LocalName == "Property")
			{
				return first.Elements("Method").All(m => second.Elements("Method").Any(n => AreCompatible(m, n)));
			}

			return true;
		}

		#endregion

		#endregion

		#region Report

		private static bool GenerateReport(XElement source)
		{
			if (source.GetValue("Compatible") == "false")
			{
				source.Elements().Where(e=>e.Name!="Parameters").Remove();
				return true;
			}

			if (!source.HasElements)
				return false;

			source.Elements().Where(e=>!GenerateReport(e)).Remove();
			return source.HasElements;
		}

		#endregion

		#region Helpers

		static string GenericsToString(this IGenericParameterProvider self)
		{
			IEnumerable<string> genericsNames = self.GenericParameters.Select(m => m.Name);
			return genericsNames.Count() > 0 ? ("`" + string.Join(",", genericsNames)) : String.Empty;
		}

		static string CorrectName(this TypeReference self, bool fullName = false)
		{
			string name = fullName ? self.FullName : self.Name;
			return self.HasGenericParameters
					? name.Substring(0, name.IndexOf('`')) + self.GenericsToString()
					: name;
		}

		static void ProperSave(this XElement source, string filename)
		{
			string fullFileName = Path.GetFullPath(filename);
			using (XmlWriter writer = XmlWriter.Create(
					fullFileName, new XmlWriterSettings() { Indent = true, IndentChars = "\t" }))
			{
				source.Save(writer);
			}
		}

		static string GetValue(this XElement self, string attributeName)
		{
			if (self.Attribute(attributeName) == null)
				return null;

			return self.Attribute(attributeName).Value.ToLowerInvariant();
		}

		static IEnumerable<XElement> SelectTypes(this XElement source)
		{
			IEnumerable<string> types = new[] { "Type", "Struct", "Class", "Interface", "Enum" };
			return source.Elements().Where(s => types.Contains(s.Name.LocalName));
		}

		#endregion
	}
}