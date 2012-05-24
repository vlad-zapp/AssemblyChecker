using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Mono.Cecil;

namespace checker
{
	static class Program
	{
		//for debug
		private static int i = 0;
		private static int total;

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
					files.AddRange(Directory.GetFiles(recursive ? dir.Substring(2) : dir, "*.dll", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
					files.AddRange(Directory.GetFiles(recursive ? dir.Substring(2) : dir, "*.exe", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
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

					//TODO: Add patching functionality
					//TODO: Verbose error output

					var report = GenerateReport(storedAssemblies);
					if (report.HasElements)
					{
						storedAssemblies.ProperSave(String.IsNullOrEmpty(resultsFile) ? ("results-" + xmlSrc) : resultsFile.Substring("fullreport:".Length));
						report.ProperSave(String.IsNullOrEmpty(reportFile) ? ("report-" + xmlSrc) : resultsFile.Substring("report:".Length));
						
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
			Console.ReadKey();
		}

		#region Dump

		private static XElement MakeDumps(IEnumerable<string> fileList)
		{
			total = fileList.Count();
			i = 0;

			var typesArrays = fileList.Select(file => ReadAssemblyTypes(file)).Where(it=>it!=null);
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
			Console.WriteLine(++i + " / " +total);

			try
			{
				return AssemblyDefinition.ReadAssembly(file).Modules.SelectMany(m => m.Types).Where(t => t.IsPublic);
			} 
			catch(Exception)
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
			XElement newElement = new XElement("Type");
			newElement.SetAttributeValue("Name", type.CorrectName());
			if (!type.IsNested)
				newElement.SetAttributeValue("Path", type.Namespace);

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

			foreach (var method in type.Methods.Where(m => m.IsPublic))
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

			total = asms1.Count();
			i = 0;

			foreach (var assembly in asms1)
			{
				Console.WriteLine(++i + " / " + total);

				var analogInSecond = asms2.FirstOrDefault(a => BasiclyCompatible(assembly, a));

				if (analogInSecond == null)
				{
					assembly.SetAttributeValue("Compatible", "false");
					continue;
				}

				CheckTypeMembers(assembly.Elements("Type"), analogInSecond.Elements("Type"));
			}
		}

		private static void CheckTypeMembers(IEnumerable<XElement> first, IEnumerable<XElement> second)
		{
			foreach (var type in first)
			{
				var analogInSecond = second.FirstOrDefault(t => BasiclyCompatible(type, t));

				if (analogInSecond == null)
				{
					type.SetAttributeValue("Compatible", "false");
					continue;
				}

				foreach (var method in type.Elements("Method"))
				{
					if (!analogInSecond.Elements("Method").Any(m => AreMethodsCompatible(method, m)))
					{
						method.SetAttributeValue("Compatible", "false");
					}
				}

				foreach (var field in type.Elements("Field"))
				{
					if (!analogInSecond.Elements("Field").Any(m => AreFieldsCompatible(field, m)))
					{
						field.SetAttributeValue("Compatible", "false");
					}
				}

				CheckTypeMembers(type.Elements("Type"), analogInSecond.Elements("Type"));
			}
		}

		#region Compatibility checks

		//TODO: Add assemblies checks

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

			node.RemoveNodes();
			return node;
		}

		private static XElement ResolvePath(XElement node)
		{
			if (node.Name == "Assembly")
				node.SetAttributeValue("Path","");

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
			return genericsNames.Count() > 0 ? ("<" + string.Join(",", genericsNames) + ">") : String.Empty;
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
			using (XmlWriter writer = XmlWriter.Create(
					filename, new XmlWriterSettings() { Indent = true, IndentChars = "\t"}))
			{
				//writer.WriteRaw(source.ToString());
				source.Save(writer);
			}
		}
		#endregion
	}
}