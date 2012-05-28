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
			if (args.Length > 0 && (args[0].ToLowerInvariant() == "-dump" || args[0].ToLowerInvariant() == "-check"))
			{
				var files = args.Where(a => a.ToLowerInvariant().EndsWith(".dll") || a.ToLowerInvariant().EndsWith(".exe")).ToList();
				var xmlSrc = args.SingleOrDefault(a => a.ToLowerInvariant().EndsWith(".xml")) ?? "prototypes.xml";
				var dirs = args.Where(a => a == "." || a.EndsWith(@"\") /*|| a.IndexOf('\\') > a.IndexOf('.')*/);

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

				//TODO: try-catch inside needed
				var assemblies = MakeDumps(files);

				if (args[0].ToLowerInvariant() == "-dump")
				{
					assemblies.ProperSave(xmlSrc);
					return 0;
				}

				if (args[0].ToLowerInvariant() == "-check")
				{
					string resultsFile = args.SingleOrDefault(a => a.ToLowerInvariant().StartsWith("fullreport:"));
					string reportFile = args.SingleOrDefault(a => a.ToLowerInvariant().StartsWith("report:"));

					XElement storedAssemblies = XElement.Load(xmlSrc);
					CheckAssemblies(storedAssemblies, assemblies);

					string defaultResultsFile = Path.GetFullPath(String.Format(@"{0}\results-{1}.xml", Path.GetDirectoryName(xmlSrc), Path.GetFileNameWithoutExtension(xmlSrc)));
					string defaultReportFile = Path.GetFullPath(String.Format(@"{0}\report-{1}.xml", Path.GetDirectoryName(xmlSrc), Path.GetFileNameWithoutExtension(xmlSrc))); 

					storedAssemblies.ProperSave(String.IsNullOrEmpty(resultsFile) ? defaultResultsFile : resultsFile.Substring("fullreport:".Length));
					var report = GenerateReport(storedAssemblies);
					report.ProperSave(String.IsNullOrEmpty(reportFile) ? defaultReportFile : reportFile.Substring("report:".Length));
					if (report.HasElements)
					{
						Console.WriteLine("Compatibility test failed!");
						Console.WriteLine("Problems are:");

						foreach (var problem in report.Elements())
						{
							Console.WriteLine(problem.Name + "\t" + problem.Element("Path") + "." + problem.Name);
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

		//TODO: remove previous default files

		private static XElement MakeDumps(IEnumerable<string> fileList)
		{
			var typesArrays = fileList.Select(file => ReadAssemblyTypes(file)).Where(it => it != null);
			var assembliesXmlNodes = typesArrays.Select(types => MakeTypeXmlProto(types, types.FirstOrDefault() != null ? types.FirstOrDefault().Module.Assembly : null));

			XElement theDump = new XElement("TheDump");
			foreach (var assembly in assembliesXmlNodes)
			{
				if (assembly.HasElements)
					theDump.Add(assembly);
			}

			return theDump;
		}

		private static IEnumerable<TypeDefinition> ReadAssemblyTypes(string file)
		{
			try
			{
				return AssemblyDefinition.ReadAssembly(file).Modules.SelectMany(m => m.Types).Where(t => t.IsPublic);
			}
			catch (Exception)
			{
				//we don't care about broken or empty files
				return null;
			}
		}

		private static XElement MakeTypeXmlProto(IEnumerable<TypeDefinition> source, AssemblyDefinition asmInfo = null)
		{
			var rootXml = new XElement("Assembly");
			if (asmInfo != null)
			{
				rootXml.SetAttributeValue("Name", asmInfo.Name.Name);
				rootXml.SetAttributeValue("Info", asmInfo.FullName);
			}

			foreach (var typeDefinition in source)
			{
				rootXml.Add(MakeTypeXmlNode(typeDefinition));
			}

			return rootXml;
		}

		private static XElement MakeTypeXmlNode(TypeDefinition type)
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

			XElement newElement = new XElement(typeType);
			newElement.SetAttributeValue("Name", type.CorrectName());
			if (!type.IsNested)
				newElement.SetAttributeValue("Path", type.Namespace);

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
					fieldXml.SetAttributeValue("Type", field.FieldType.CorrectName(true));
				}
				newElement.Add(fieldXml);
			}

			//adding methods
			foreach (var method in type.Methods.Where(m => m.IsPublic && !m.IsGetter && !m.IsSetter))
			{
				var methodXml = new XElement("Method");
				methodXml.SetAttributeValue("Name", method.Name + method.GenericsToString());
				methodXml.SetAttributeValue("ReturnType", method.ReturnType.CorrectName(true));

				if (method.HasParameters)
				{
					var paramsXml = new XElement("Parameters");
					foreach (var parameter in method.Parameters)
					{
						var paramXml = new XElement("Parameter");
						paramXml.SetAttributeValue("Name", parameter.Name);
						paramXml.SetAttributeValue("Type", parameter.ParameterType.CorrectName(true));
						paramsXml.Add(paramXml);
					}
					methodXml.Add(paramsXml);
				}

				methodXml.Add();
				newElement.Add(methodXml);
			}

			//adding properties
			foreach (var property in type.Properties.Where(p => (p.GetMethod!=null && p.GetMethod.IsPublic) || (p.SetMethod!=null && p.SetMethod.IsPublic)))
			{
				var propertyXml = new XElement("Property");
				propertyXml.SetAttributeValue("Name", property.Name);
				propertyXml.SetAttributeValue("Type", property.PropertyType.CorrectName());
				if (property.GetMethod != null)
				{
					propertyXml.SetAttributeValue("Getter", property.GetMethod.IsPublic ? "public" : "not_public");
				}
				if (property.SetMethod != null)
				{
					propertyXml.SetAttributeValue("Setter", property.SetMethod.IsPublic ? "public" : "not_public");
				}
				newElement.Add(propertyXml);
			}

			//adding nested types
			foreach (var nestedType in type.NestedTypes.Where(m => m.IsPublic))
			{
				newElement.Add(MakeTypeXmlNode(nestedType));
			}
			return newElement;
		}

		#endregion

		#region Check

		private static void CheckAssemblies(XElement first, XElement second)
		{
			var asms1 = first.Elements("Assembly");
			var asms2 = second.Elements("Assembly");

			foreach (var assembly in asms1.Where(a => !isUntouchable(a)))
			{
				var analogInSecond = asms2.FirstOrDefault(a => BasiclyCompatible(assembly, a));

				if (analogInSecond == null)
				{
					assembly.SetAttributeValue("Compatible", "false");
					continue;
				}

				CheckTypeMembers(assembly.SelectTypes().Where(e => !isUntouchable(e)), analogInSecond.SelectTypes());
			}
		}

		private static void CheckTypeMembers(IEnumerable<XElement> first, IEnumerable<XElement> second)
		{
			foreach (var type in first.Where(t => !isUntouchable(t)))
			{
				var analogInSecond = second.FirstOrDefault(t => BasiclyCompatible(type, t));

				if (analogInSecond == null)
				{
					type.SetAttributeValue("Compatible", "false");
					continue;
				}

				foreach (var method in type.Elements("Method").Where(t => !isUntouchable(t)))
				{
					if (!analogInSecond.Elements("Method").Any(m => AreMethodsCompatible(method, m)))
					{
						method.SetAttributeValue("Compatible", "false");
					}
				}

				foreach (var field in type.Elements("Field").Where(t => !isUntouchable(t)))
				{
					if (!analogInSecond.Elements("Field").Any(m => AreFieldsCompatible(field, m)))
					{
						field.SetAttributeValue("Compatible", "false");
					}
				}

				foreach (var property in type.Elements("Property").Where(t => !isUntouchable(t)))
				{
					if (!analogInSecond.Elements("Property").Any(m => ArePropertiesCompatible(property, m)))
					{
						property.SetAttributeValue("Compatible", "false");
					}
				}

				CheckTypeMembers(type.Elements("Type"), analogInSecond.Elements("Type"));
			}
		}

		#region Compatibility checks

		//TODO: Add assemblies checks here

		private static bool isUntouchable(XElement e)
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
				var params1 = first.Element("Parameters").Elements("Parameter").Select(m => m.Attribute("Type").Value).ToArray();
				var params2 = second.Element("Parameters").Elements("Parameter").Select(m => m.Attribute("Type").Value).ToArray();

				return Enumerable.SequenceEqual(params1, params2);
			}

			return true;
		}

		private static bool ArePropertiesCompatible(XElement first, XElement second)
		{
			if (!BasiclyCompatible(first, second))
				return false;

			if (!AreFieldsCompatible(first, second))
				return false;

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

		#endregion

		#endregion

		#region Report

		private static XElement GenerateReport(XElement source)
		{
			var LogNodes =
				source.Descendants().Where(d => d.Attribute("Compatible") != null && d.Attribute("Compatible").Value == "false")
				.Select(node => MakeLogRecord(node));

			XElement report = new XElement("Report", LogNodes);
			return report;
		}

		private static XElement MakeLogRecord(XElement node)
		{
			if (node.Attribute("Path") == null)
				ResolvePath(node);

			//TODO: refactoring needed)
			if (node.Element("Parameters") != null)
			{
				XElement parameters = new XElement(node.Element("Parameters"));
				node.RemoveNodes();
				node.Add(parameters);
			}
			else
			{
				node.RemoveNodes();
			}

			return node;
		}

		/*
		* this is for the future :)
		* 
		private static XElement ApplyPatch(XElement report, XElement patch)
		{
			foreach (var element in report.Elements())
			{
				var patchNode = patch.Elements().FirstOrDefault(e => CanPatch(e, element));

				if(patchNode!=null)
				{
					if(patchNode.Element("Mode").Value.ToLowerInvariant()=="skip")
					{
						element.Attribute("Compatible").Remove();
					}
					//else if(patchNode.Element("Mode").Value.ToLowerInvariant()=="override")
					//{
					//    if(CanPatch())
					//}
				}
			}

			report.Elements().Where(e=>e.Attribute("Compatible")==null).Remove();
			return report;
		}
		*/

		private static XElement ResolvePath(XElement node)
		{
			if (node.Name == "Assembly")
				node.SetAttributeValue("Path", "");

			if (node.Attribute("Path") != null)
				return node;

			var parent = ResolvePath(node.Parent);
			node.SetAttributeValue("Path", String.Format("{0}.{1}", parent.Attribute("Path").Value, parent.Attribute("Name").Value));
			return node;
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