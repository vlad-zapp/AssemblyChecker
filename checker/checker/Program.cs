using System;
using System.Collections;
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
				var dumpSrc = args.SingleOrDefault(a => a.ToLowerInvariant().EndsWith(".xml"));
				var dirs = args.Where(a => a == "." || a.EndsWith(@"\") || a.IndexOf('\\') > a.IndexOf('.'));

				foreach (var dir in dirs)
				{
					bool recursive = dir.ToLowerInvariant().StartsWith("-r");
					files.AddRange(Directory.GetFiles(recursive ? dir.Substring(2) : dir, "*.dll"));
					files.AddRange(Directory.GetFiles(recursive ? dir.Substring(2) : dir, "*.exe"));
				}

				if (files.Count() <= 0)
				{
					Usage();
				}

				//TODO: try-catch inside needed
				var assemblies = MakeDumps(files);

				if(args[0].ToLowerInvariant() == "-dump")
				{
					assemblies.ProperSave(dumpSrc ?? "report.xml");
					return 0;
				}

				if (args[0].ToLowerInvariant() == "-check")
				{
					
					//TODO: Return error!
					return 0;
				}
			}
			
			Usage();
			return -1;
		}

		private static void Usage()
		{
			Console.WriteLine("You dont know how to use it!");
		}

		private static XElement MakeDumps(IEnumerable<string> fileList)
		{
			var typesArrays =
				fileList.Select(
					file => AssemblyDefinition.ReadAssembly(file).Modules.SelectMany(m => m.Types).Where(t => t.IsPublic));

			var assembliesXmlNodes = typesArrays.Select(types => MakeTypeXmlProto(types, types.FirstOrDefault() != null ? types.FirstOrDefault().Module.Assembly.FullName : ""));

			XElement theDump = new XElement("TheDump");
			foreach (var assembly in assembliesXmlNodes)
			{
				if (assembly.HasElements)
					theDump.Add(assembly);
			}

			return theDump;
		}

		private static XElement MakeTypeXmlProto(IEnumerable<TypeDefinition> source, string assemblyName = null)
		{
			var rootXml = new XElement("Assembly");
			if (assemblyName != null)
			{
				rootXml.SetAttributeValue("Info", assemblyName);
			}

			foreach (var typeDefinition in source)
			{
				rootXml.Add(MakeTypeXmlNode(typeDefinition));
			}

			return rootXml;
		}

		private static XElement MakeTypeXmlNode(TypeDefinition type)
		{
			//TODO: check existance
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

		private static bool CheckAssemblies(XElement first, XElement second)
		{
			var asms1 = first.Elements("Assembly");
			var asms2 = first.Elements("Assembly");

			foreach (var assembly in asms1)
			{
				
			}

			return true;
		}

		private static void CheckTypeMembers(IEnumerable<XElement> first, IEnumerable<XElement> second)
		{
			foreach (var type in first)
			{
				var analogInSecond = second.FirstOrDefault(m => BasiclyCompatible(type, m));

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
			//TODO: special compatibility check for every entity type
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
				var params1 = first.Element("Parameters").Elements("Parameter").Select(m => m.Attribute("Type")).ToArray();
				var params2 = second.Element("Parameters").Elements("Parameter").Select(m => m.Attribute("Type")).ToArray();

				return Enumerable.SequenceEqual(params1, params2);
			}

			return true;
		}

		private static bool AreFieldsCompatible(XElement first, XElement second)
		{
			if (!BasiclyCompatible(first, second))
				return false;

			//check types
			if (first.Attribute("Type").Value != second.Attribute("Type").Value)
			{
				return false;
			}

			return true;
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
					? name.Substring(0, name.IndexOf('`') - 1) + self.GenericsToString()
					: name;
		}

		static void ProperSave(this XElement source, string filename)
		{
			using (XmlWriter writer = XmlWriter.Create(
					filename, new XmlWriterSettings() { Indent = true, IndentChars = "\t" }))
			{
				source.Save(writer);
			}
		}
		#endregion
	}
}
