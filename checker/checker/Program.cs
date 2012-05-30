using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Mono.Cecil;

namespace checker
{
	//TODO: Check for static members and types!!!
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

				foreach (var dir in dirs)
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
					string resultsFile = args.SingleOrDefault(a => a.ToLowerInvariant().StartsWith("results:"));
					string ignoreListFile = args.SingleOrDefault(a => a.ToLowerInvariant().StartsWith("ignore:"));

					XElement storedAssemblies = XElement.Load(xmlSrc);
					CheckAssemblies(storedAssemblies.Elements("Assembly"), assemblies.Elements("Assembly"));

					string defaultName = Path.ChangeExtension(xmlSrc, null);
					string defaultReportFile = Path.GetFullPath(String.Format(@"{0}-report.xml",defaultName));
					string defaultResultsFile = Path.GetFullPath(String.Format(@"{0}-results.xml", defaultName));
					string defaultIgnoreListFile = Path.GetFullPath(String.Format(@"{0}-ignore.xml", defaultName));

					XElement ignoreList = null;
					ignoreListFile = String.IsNullOrEmpty(ignoreListFile)
					                 	? defaultIgnoreListFile
										: ignoreListFile.Substring("ignore:".Length);

					if (File.Exists(ignoreListFile))
					{
						ignoreList = XElement.Load(ignoreListFile);
					}

					XElement report = GenerateReport(storedAssemblies,ignoreList);
					storedAssemblies.ProperSave(String.IsNullOrEmpty(resultsFile) ? defaultResultsFile : resultsFile.Substring("report:".Length));
					report.ProperSave(String.IsNullOrEmpty(reportFile) ? defaultReportFile : reportFile.Substring("results:".Length));
					if (report.HasElements)
					{
						Console.WriteLine("Compatibility test failed!");
						Console.WriteLine("Problems are:");

						foreach (var problem in report.Elements())
						{
							Console.WriteLine(problem.Name+String.Join(" ",problem.Attributes().Select(a=>a.ToString())));
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
			Console.WriteLine("ignore:file.xml - path to ignore file. Default is %dump%-ignore.xml");
			Console.WriteLine("ignore:file.xml - path to report file. Default is %dump%-report.xml");
			Console.WriteLine("ignore:file.xml - path to results file. Default is %dump%-results.xml");
			Console.WriteLine("Where %dump% - is a full name of dump file without extension");
		}

		#region Dump

		private static XElement MakeDumps(IEnumerable<string> fileList)
		{
			IEnumerable<AssemblyDefinition> asmDefinitions =
				fileList.Select(f => AssemblyDefinition.ReadAssembly(f)).Where(it => it != null);

			XElement dumpXml = new XElement("CompatibilityInfo");
			foreach (var assembly in asmDefinitions)
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

			foreach (var typeDefinition in sourceTypes)
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

			
			if(type.IsClass && type.IsSealed && type.IsAbstract)
			{
				typeXml.SetAttributeValue("Static", "true");
			}
			else if (type.IsClass && type.IsAbstract)
			{
				typeXml.SetAttributeValue("Abstract","true");
			}

			if (!type.IsNested)
				typeXml.SetAttributeValue("Path", type.Namespace);

			//adding fields
			foreach (var field in type.Fields.Where(f => f.IsPublic))
			{
				var fieldXml = new XElement("Field");
				fieldXml.SetAttributeValue("Name", field.Name);
				fieldXml.SetAttributeValue("Static",field.IsStatic?"true":null);

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
			foreach (var method in type.Methods.Where(m => m.IsPublic && !m.IsGetter && !m.IsSetter))
			{
				var methodXml = new XElement("Method");
				methodXml.SetAttributeValue("Name", method.Name + method.GenericsToString());
				methodXml.SetAttributeValue("ReturnType", method.ReturnType);
				methodXml.SetAttributeValue("Static", method.IsStatic ? "true" : null);
				methodXml.SetAttributeValue("Virtual", method.IsVirtual ? "true" : null);
				methodXml.SetAttributeValue("Override", method.Overrides!=null ? "true" : null);

				if (method.HasParameters)
				{
					var paramsXml = new XElement("Parameters");
					foreach (var parameter in method.Parameters)
					{
						var paramXml = new XElement("Parameter");
						paramXml.SetAttributeValue("Name", parameter.Name);
						paramXml.SetAttributeValue("Type", parameter.ParameterType);
						paramsXml.Add(paramXml);
					}
					methodXml.Add(paramsXml);
				}

				methodXml.Add();
				typeXml.Add(methodXml);
			}

			//adding properties
			foreach (var property in type.Properties.Where(p => (p.GetMethod != null && p.GetMethod.IsPublic) || (p.SetMethod != null && p.SetMethod.IsPublic)))
			{
				var propertyXml = new XElement("Property");
				propertyXml.SetAttributeValue("Name", property.Name);
				propertyXml.SetAttributeValue("Type", property.PropertyType);

				if (property.GetMethod != null)
				{
					propertyXml.SetAttributeValue("Getter", property.GetMethod.IsPublic ? "public" : "not_public");
					propertyXml.SetAttributeValue("Static", property.GetMethod.IsStatic ? "true" : null);
				}
				if (property.SetMethod != null)
				{
					propertyXml.SetAttributeValue("Setter", property.SetMethod.IsPublic ? "public" : "not_public");
					propertyXml.SetAttributeValue("Static", property.SetMethod.IsStatic ? "true" : null);
				}
				typeXml.Add(propertyXml);
			}

			//adding nested types
			foreach (var nestedType in type.NestedTypes.Where(m => m.IsPublic))
			{
				typeXml.Add(DumpType(nestedType));
			}
			return typeXml;
		}

		#endregion

		#region Check

		private static void CheckAssemblies(IEnumerable<XElement> first, IEnumerable<XElement> second)
		{
			foreach (var assembly in first.Where(a => !IsUntouchable(a)))
			{
				var analogInSecond = second.FirstOrDefault(a => AreCompatible(assembly, a));

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
			foreach (var type in first)
			{
				var analogInSecond = second.FirstOrDefault(t => AreCompatible(type, t));

				if (analogInSecond == null)
				{
					type.SetAttributeValue("Compatible", "false");
					continue;
				}

				foreach (var member in type.Elements().Where(t => !IsUntouchable(t)))
				{
					if (!analogInSecond.Elements().Any(m => AreCompatible(member, m)))
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
			if (first.Element("Parameters") != null && second.Element("Parameters") != null)
			{
				IEnumerable<string> params1 = first.Element("Parameters").Elements("Parameter").Select(m => m.Attribute("Type").Value).ToArray();
				IEnumerable<string> params2 = second.Element("Parameters").Elements("Parameter").Select(m => m.Attribute("Type").Value).ToArray();

				return Enumerable.SequenceEqual(params1, params2);
			}

			return true;
		}

		#endregion

		#endregion

		#region Report

		private static XElement GenerateReport(XElement source, XElement ignoreList = null)
		{
			IEnumerable<XElement> logNodes =
				source.Descendants().Where(d => d.GetValue("Compatible") == "false")
				.Select(node => MakeReportRecord(node));

			if(ignoreList!=null)
			{
				logNodes = logNodes.Where(n => !ignoreList.Elements().Any(m=>AreCompatible(n,m)));
			}

			XElement report = new XElement("Report", logNodes);
			return report;
		}

		private static XElement MakeReportRecord(XElement node)
		{
			XElement logNode = new XElement(node);

			logNode.SetAttributeValue("Path", ResolvePath(node));
			logNode.Elements().Where(e => e.Name.LocalName.ToLowerInvariant() != "parameters").Remove();

			return logNode;
		}

		private static string ResolvePath(XElement node)
		{
			if (node.Attribute("Path") != null)
			{
				return node.Attribute("Path").Value;
			}

			if (node.Parent != null && node.Parent.Attribute("Name") != null)
			{
				string parentPath = ResolvePath(node.Parent);
				return String.Format("{0}{1}{2}", parentPath, parentPath != null ? "." : String.Empty, node.Parent.Attribute("Name").Value);
			}

			return null;
		}

		#endregion

		#region Helpers

		static string GenericsToString(this IGenericParameterProvider self)
		{
			var genericsNames = self.GenericParameters.Select(m => m.Name);
			return genericsNames.Count() > 0 ? ("`" + string.Join(",", genericsNames)) : String.Empty;
		}

		static string CorrectName(this TypeReference self, bool fullName = false)
		{
			var name = fullName ? self.FullName : self.Name;
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
			IEnumerable<string> types = new[] {"Type", "Struct", "Class", "Interface", "Enum"};
			return source.Elements().Where(s => types.Contains(s.Name.LocalName));
		}

		#endregion
	}
}