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
		static void Main(string[] args)
		{
			IEnumerable<string> files =
				Directory.GetFiles(@"D:\code\OpenLABSharedServices\SharedServices\ClientAPI\bin\x86\Debug", "*.dll",
				                   SearchOption.AllDirectories);
			var assemblies = MakeDumps(files);
			assemblies.PropperSave("report.xml");
		}

		private static XElement MakeDumps(IEnumerable<string> fileList)
		{
			var typesArrays =
				fileList.Select(
					file => AssemblyDefinition.ReadAssembly(file).Modules.SelectMany(m => m.Types).Where(t => t.IsPublic));

			var assembliesXmlNodes = typesArrays.Select(types => MakeTypeXmlProto(types,types.FirstOrDefault()!=null?types.FirstOrDefault().Module.Assembly.FullName:""));

			XElement theDump = new XElement("TheDump");
			foreach (var assembly in assembliesXmlNodes)
			{
				if(assembly.HasElements)
					theDump.Add(assembly);
			}

			return theDump;
		}

		private static XElement MakeTypeXmlProto(IEnumerable<TypeDefinition> source, string assemblyName=null)
		{
			var rootXml = new XElement("Assembly");
			if(assemblyName!=null)
			{
				rootXml.SetAttributeValue("Info", assemblyName);
			}
			
			foreach (var typeDefinition in source)
			{
				rootXml.Add(MakeTypeXmlNode(typeDefinition));
			}

			return rootXml;
		}

		public static XElement MakeTypeXmlNode(TypeDefinition type)
		{
			//TODO: check existance
			XElement newElement = new XElement("Type");
			newElement.SetAttributeValue("Name",type.CorrectName());
			if(!type.IsNested)
				newElement.SetAttributeValue("Path",type.Namespace);

			foreach (var field in type.Fields.Where(f=>f.IsPublic))
			{
				var fieldXml = new XElement("Field");
				
				if(type.IsEnum)
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

			foreach (var method in type.Methods.Where(m=>m.IsPublic))
			{
				var methodXml = new XElement("Method");
				methodXml.SetAttributeValue("Name",method.Name+method.GenericsToString());
				methodXml.SetAttributeValue("ReturnType",method.ReturnType.CorrectName(true));
				
				if(method.HasParameters)
				{
					var paramsXml = new XElement("Parameters");
					foreach (var parameter in method.Parameters)
					{
						var paramXml = new XElement("Parameter");
						paramXml.SetAttributeValue("Name",parameter.Name);
						paramXml.SetAttributeValue("Type",parameter.ParameterType.CorrectName(true));
						paramsXml.Add(paramXml);
					}
					methodXml.Add(paramsXml);
				}
				
				methodXml.Add();
				newElement.Add(methodXml);
			}

			foreach (var nestedType in type.NestedTypes.Where(m=>m.IsPublic))
			{
				newElement.Add(MakeTypeXmlNode(nestedType));
			}
			return newElement;
		}

		public static void CompareTypeMembers(IEnumerable<XElement> first, IEnumerable<XElement> second)
		{
			foreach (var type in first)
			{
				var analogInSecond = second.FirstOrDefault(m => BasicCompare(type, m));

				if(analogInSecond==null)
				{
					type.SetAttributeValue("Compatible","false");
					continue;
				}

				foreach (var method in type.Elements("Method"))
				{
					if(!analogInSecond.Elements("Method").Any(m=>BasicCompare(method,m)))
					{
						method.SetAttributeValue("Compatible", "false");
					}
				}

				foreach (var field in type.Elements("Field"))
				{
					if (!analogInSecond.Elements("Field").Any(m => BasicCompare(field, m)))
					{
						field.SetAttributeValue("Compatible", "false");
					}
				}

				CompareTypeMembers(type.Elements("Type"), analogInSecond.Elements("Type"));
			}
		}

		public static bool BasicCompare(XElement first, XElement second)
		{
			//TODO: special compatibility check for every entity type
			return first.Attribute("Name").Value == second.Attribute("Name").Value;
		}

		#region Helpers
		static string GenericsToString(this IGenericParameterProvider self)
		{
			var genericsNames = self.GenericParameters.Select(m => m.Name);
			return genericsNames.Count()>0?("<" + string.Join(",", genericsNames) + ">"):String.Empty;
		}
		
		static string CorrectName(this TypeReference self, bool fullName=false)
		{
			var name = fullName ? self.FullName : self.Name;
			return self.HasGenericParameters
					? name.Substring(0, name.IndexOf('`') - 1) + self.GenericsToString()
					: name;
		}

		static void PropperSave(this XElement source, string filename)
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
