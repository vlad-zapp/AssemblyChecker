using System;
using System.Collections.Generic;
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
					string resultsFile = args.SingleOrDefault(a => a.ToLowerInvariant().StartsWith("complete-report"));
					string ignoreListFile = args.SingleOrDefault(a => a.ToLowerInvariant().StartsWith("ignore-list:"));

					XElement storedAssemblies = XElement.Load(xmlSrc);
					CheckAssemblies(storedAssemblies.Elements("Assembly"), assemblies.Elements("Assembly"));

					string defaultName = Path.ChangeExtension(xmlSrc, null);
					string defaultReportFile = Path.GetFullPath(String.Format(@"{0}-report.xml",defaultName));
					string defaultResultsFile = Path.GetFullPath(String.Format(@"{0}-fullReport.xml", defaultName));
					string defaultIgnoreListFile = Path.GetFullPath(String.Format(@"{0}-ignoreList.xml", defaultName));

					XElement ignoreList = null;
					ignoreListFile = String.IsNullOrEmpty(ignoreListFile)
					                 	? defaultIgnoreListFile
					                 	: ignoreListFile.Substring("ignore-list:".Length);

					if (File.Exists(ignoreListFile))
					{
						ignoreList = XElement.Load(ignoreListFile);
					}

					XElement report = GenerateReport(storedAssemblies,ignoreList);
					storedAssemblies.ProperSave(String.IsNullOrEmpty(resultsFile) ? defaultResultsFile : resultsFile.Substring("fullreport:".Length));
					report.ProperSave(String.IsNullOrEmpty(reportFile) ? defaultReportFile : reportFile.Substring("report:".Length));
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
			Console.WriteLine("You dont know how to use it!");
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

			if (!type.IsNested)
				typeXml.SetAttributeValue("Path", type.Namespace);

			//adding fields
			foreach (var field in type.Fields.Where(f => f.IsPublic))
			{
				var fieldXml = new XElement("Field");

				if (type.IsEnum)
				{
					fieldXml.SetAttributeValue("Name", field.Name);
					fieldXml.SetAttributeValue("Value", field.Constant);
				}
				else
				{
					fieldXml.SetAttributeValue("Name", field.Name);
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
				}
				if (property.SetMethod != null)
				{
					propertyXml.SetAttributeValue("Setter", property.SetMethod.IsPublic ? "public" : "not_public");
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
				var analogInSecond = second.FirstOrDefault(a => BasiclyCompatible(assembly, a));

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
				var analogInSecond = second.FirstOrDefault(t => BasiclyCompatible(type, t));

				if (analogInSecond == null)
				{
					type.SetAttributeValue("Compatible", "false");
					continue;
				}

				foreach (var method in type.Elements("Method").Where(t => !IsUntouchable(t)))
				{
					if (!analogInSecond.Elements("Method").Any(m => AreMethodsCompatible(method, m)))
					{
						method.SetAttributeValue("Compatible", "false");
					}
				}

				foreach (var field in type.Elements("Field").Where(t => !IsUntouchable(t)))
				{
					if (!analogInSecond.Elements("Field").Any(m => AreFieldsCompatible(field, m)))
					{
						field.SetAttributeValue("Compatible", "false");
					}
				}

				foreach (var property in type.Elements("Property").Where(t => !IsUntouchable(t)))
				{
					if (!analogInSecond.Elements("Property").Any(m => ArePropertiesCompatible(property, m)))
					{
						property.SetAttributeValue("Compatible", "false");
					}
				}

				CheckTypes(type.Elements("Type").Where(t => !IsUntouchable(t)), analogInSecond.Elements("Type"));
			}
		}

		#region Compatibility checks

		private static bool IsUntouchable(XElement e)
		{
			return e.Attribute("Compatible") != null && e.Attribute("Compatible").Value.ToLowerInvariant() == "true";
		}

		private static bool BasiclyCompatible(XElement first, XElement second)
		{
			return first.Attribute("Name").Value == second.Attribute("Name").Value;
		}

		private static bool AreMethodsCompatible(XElement first, XElement second)
		{
			if (!BasiclyCompatible(first, second))
				return false;

			//check return types
			if (first.Attribute("ReturnType").Value != second.Attribute("ReturnType").Value)
			{
				return false;
			}

			//check parameters
			if (first.Element("Parameters") != null && second.Element("Parameters") != null)
			{
				IEnumerable<string> params1 = first.Element("Parameters").Elements("Parameter").Select(m => m.Attribute("Type").Value).ToArray();
				IEnumerable<string> params2 = second.Element("Parameters").Elements("Parameter").Select(m => m.Attribute("Type").Value).ToArray();

				return Enumerable.SequenceEqual(params1, params2);
			}

			return true;
		}

		private static bool AreFieldsCompatible(XElement first, XElement second)
		{
			if (!BasiclyCompatible(first, second))
				return false;

			//check field type
			if (first.Attribute("Type") != null && second.Attribute("Type") != null)
				if (first.Attribute("Type").Value != second.Attribute("Type").Value)
				{
					return false;
				}

			//check enum fields
			if (first.Attribute("Value") != null && second.Attribute("Value") != null)
				if (first.Attribute("Value").Value != second.Attribute("Value").Value)
				{
					return false;
				}

			return true;
		}

		private static bool ArePropertiesCompatible(XElement first, XElement second)
		{
			if (!BasiclyCompatible(first, second))
				return false;

			if (!AreFieldsCompatible(first, second))
				return false;

			//check accessors and their visibility

			if (first.Attribute("Getter") != null && first.Attribute("Getter").Value.ToLowerInvariant() == "public" &&
				(second.Attribute("Getter") == null || second.Attribute("Getter").Value.ToLowerInvariant() != "public"))
				return false;

			if (first.Attribute("Setter") != null && first.Attribute("Setter").Value.ToLowerInvariant() == "public" &&
				(second.Attribute("Setter") == null || second.Attribute("Setter").Value.ToLowerInvariant() != "public"))
				return false;

			return true;
		}

		#endregion

		#endregion

		#region Report

		private static XElement GenerateReport(XElement source, XElement ignoreList = null)
		{
			IEnumerable<XElement> logNodes =
				source.Descendants().Where(d => d.Attribute("Compatible") != null && d.Attribute("Compatible").Value == "false")
				.Select(node => MakeLogRecord(node));

			if(ignoreList!=null)
			{
				logNodes = logNodes.Where(n => !ignoreList.Elements().Contains(n));
			}

			XElement report = new XElement("Report", logNodes);
			return report;
		}

		private static XElement MakeLogRecord(XElement node)
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

		static IEnumerable<XElement> SelectTypes(this XElement source)
		{
			return source.Elements().Where(s => (new[] { "Type", "Struct", "Class", "Interface", "Enum" }).Contains(s.Name.LocalName));
		}

		#endregion
	}
}