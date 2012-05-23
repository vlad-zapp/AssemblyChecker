using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace checker
{
	static class Program
	{
		private static XElement logElement;
		private static XElement rootElement;

		static void Main(string[] args)
		{
			IEnumerable<string> files =
				Directory.GetFiles(@"D:\code\OpenLABSharedServices\SharedServices\ClientAPI\bin\x86\Debug", "*.dll",
				                   SearchOption.AllDirectories);
			var assemblies = MakeDumps(files);
			assemblies.PropperSave("report.xml");
			//rootElement = new XElement("Assembly");
			//string file1 = "testLib1.dll";
			//string file2 = "testLib2.dll";
			//var oldAssembly = AssemblyDefinition.ReadAssembly(file1);
			//var newAssembly = AssemblyDefinition.ReadAssembly(file2);

			//if (oldAssembly != null && newAssembly != null)
			//{
			//    var oldTypes = oldAssembly.Modules.SelectMany(m => m.Types).Where(t => t.IsPublic);
			//    var newTypes = newAssembly.Modules.SelectMany(m => m.Types).Where(t => t.IsPublic);

			//    using (XmlWriter writer = XmlWriter.Create(
			//        "results.xml",new XmlWriterSettings() { Indent = true, IndentChars = "\t" }))
			//    {
			//        var x = MakeXmlDump(oldTypes);
			//        var y = MakeXmlDump(newTypes);

			//        CompareTypes(x.Elements("Type"), y.Elements("Type"));
			//        x.Save(writer);
			//        //using (XmlWriter writer2 = XmlWriter.Create(
			//        //"results-new.xml", new XmlWriterSettings() { Indent = true, IndentChars = "\t" }))
			//        //y.Save(writer2);
			//    }
			//}
		}

		public static XElement MakeDumps(IEnumerable<string> fileList)
		{
			var typesArrays =
				fileList.Select(
					file => AssemblyDefinition.ReadAssembly(file).Modules.SelectMany(m => m.Types).Where(t => t.IsPublic));

			var assembliesXmlNodes = typesArrays.Select(types => MakeXmlDump(types,types.FirstOrDefault()!=null?types.FirstOrDefault().Module.Assembly.FullName:""));

			XElement theDump = new XElement("TheDump");
			foreach (var assembly in assembliesXmlNodes)
			{
				if(assembly.HasElements)
					theDump.Add(assembly);
			}

			return theDump;
		}

		static void PropperSave(this XElement source, string filename)
		{
			using (XmlWriter writer = XmlWriter.Create(
					filename, new XmlWriterSettings() { Indent = true, IndentChars = "\t" }))
			{
				source.Save(writer);
			}
		}

		static XElement MakeXmlDump(IEnumerable<TypeDefinition> source, string assemblyName=null)
		{
			var rootXml = new XElement("Assembly");
			if(assemblyName!=null)
			{
				rootXml.SetAttributeValue("Info", assemblyName);
			}
			
			foreach (var typeDefinition in source)
			{
				rootXml.Add(MakeTypeNode(typeDefinition));
			}

			return rootXml;
		}

		public static XElement MakeTypeNode(TypeDefinition type)
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
				newElement.Add(MakeTypeNode(nestedType));
			}
			return newElement;
		}

		public static void CompareTypes(IEnumerable<XElement> first, IEnumerable<XElement> second)
		{
			foreach (var type in first)
			{
				var analogInSecond = second.FirstOrDefault(m => Compare(type, m));

				if(analogInSecond==null)
				{
					type.SetAttributeValue("Compatible","false");
					continue;
				}

				foreach (var method in type.Elements("Method"))
				{
					if(!analogInSecond.Elements("Method").Any(m=>Compare(method,m)))
					{
						method.SetAttributeValue("Compatible", "false");
					}
				}

				foreach (var field in type.Elements("Field"))
				{
					if (!analogInSecond.Elements("Field").Any(m => Compare(field, m)))
					{
						field.SetAttributeValue("Compatible", "false");
					}
				}

				CompareTypes(type.Elements("Type"), analogInSecond.Elements("Type"));
			}

			

		}

		public static bool Compare(XElement first, XElement second)
		{
			return first.Attribute("Name").Value == second.Attribute("Name").Value;
		}

		//this all is trash!
		static void CheckAssemblies(string file1, string file2)
		{
			var oldAssembly = AssemblyDefinition.ReadAssembly(file1);
			var newAssembly = AssemblyDefinition.ReadAssembly(file2);

			if(oldAssembly!=null && newAssembly!=null)
			{
				var oldTypes = oldAssembly.Modules.SelectMany(m => m.Types).Where(t => t.IsPublic);
				var newTypes = newAssembly.Modules.SelectMany(m => m.Types).Where(t => t.IsPublic);

				CheckTypes(oldTypes, newTypes);
			}
		}

		#region compatibility checks for entities

		static bool AreCompatible(IGenericParameterProvider first, IGenericParameterProvider second)
		{
			if ((first as MemberReference).FullName != (second as MemberReference).FullName)
				return false;

			var firstGenerics = first.GenericParameters.Select(p => p.Name).ToList();
			var secondGenerics = second.GenericParameters.Select(p => p.Name).ToList();
			
			if (firstGenerics.Any(m => secondGenerics[firstGenerics.IndexOf(m)] != m))
				return false;

			return true;
		}

		static bool MethodsAreCompatible(MethodReference first, MethodReference second)
		{
			if(!AreCompatible(first,second))
				return false;
			
			if (!AreCompatible(first.ReturnType, second.ReturnType))
				return false;
			
			if(first.Parameters.Any(m=>second.Parameters.FirstOrDefault(n=>AreCompatible(m.ParameterType,n.ParameterType))==null))
				return false;
			
			return true;
		}

		static bool FieldsAreCompatible(FieldDefinition first, FieldDefinition second)
		{
			if (first.FullName != second.FullName)
				return false;

			if (AreCompatible(first.FieldType, second.FieldType))
				return false;

			return true;
		}

		#endregion

		#region compatibility checks for entity members

		static void CheckTypes(IEnumerable<TypeDefinition> newTypes, IEnumerable<TypeDefinition> oldTypes)
		{
			var addedTypes = newTypes.Where(m => oldTypes.FirstOrDefault(n => AreCompatible(n, m)) == null);
			var removedTypes = oldTypes.Where(m => newTypes.FirstOrDefault(n => AreCompatible(n, m)) == null);

			LogChanges(new[] { addedTypes, removedTypes }, String.Empty, "Type");

			foreach (var type in newTypes.Except(addedTypes))
			{
				var oldType = oldTypes.First(t => AreCompatible(t, type));

				var checkedMethods = CheckMethods(oldType.Methods, type.Methods);
				LogChanges(checkedMethods, oldType.FullName,"Method");
				
				var checkedFields = CheckFields(oldType.Fields, type.Fields);
				LogChanges(checkedFields, oldType.FullName,"Field");

				CheckTypes(oldType.NestedTypes, type.NestedTypes);
			}
		}

		static IEnumerable<MethodDefinition>[] CheckMethods(IEnumerable<MethodDefinition> first, IEnumerable<MethodDefinition> second)
		{
			var firstMethods = first.Where(m => m.IsPublic);
			var secondMethods = second.Where(m => m.IsPublic);

			var addedMembers = firstMethods.Where(m => secondMethods.FirstOrDefault(n => MethodsAreCompatible(n, m)) == null);
			var removedMembers = secondMethods.Where(m => firstMethods.FirstOrDefault(n => MethodsAreCompatible(n, m)) == null);

			return new[] { addedMembers, removedMembers };
		}

		static IEnumerable<FieldDefinition>[] CheckFields(IEnumerable<FieldDefinition> first, IEnumerable<FieldDefinition> second)
		{
			var firstFileds = first.Where(m => m.IsPublic);
			var secondFields = second.Where(m => m.IsPublic);

			var addedMembers = firstFileds.Where(m => secondFields.FirstOrDefault(n => FieldsAreCompatible(n, m)) == null);
			var removedMembers = secondFields.Where(m => firstFileds.FirstOrDefault(n => FieldsAreCompatible(n, m)) == null);

			return new [] { addedMembers, removedMembers };
		}

		/*
		 * Not needed now. Acessors are checking as methods.
		 * 
		static string CheckProperties(TypeDefinition first, TypeDefinition second)
		{
			//TODO: Filter them! isPublic() not working here
			var firstProperties = first.Properties;
			var secondProperties = second.Properties;

			var addedMembers = firstProperties.Where(m => secondProperties.FirstOrDefault(n => PropertiesAreCompatible(n, m)) == null).Select(m => m.ToString());
			var removedMembers = secondProperties.Where(m => firstProperties.FirstOrDefault(n => PropertiesAreCompatible(n, m)) == null).Select(m => m.ToString());

		}
		 */

		#endregion

		public static void LogChanges(IEnumerable<IMemberDefinition>[] results, string parentName, string entityType = "Member")
		{
			var addedEntites = results[0].Select(m => m.Name +
				(
					m is IGenericParameterProvider ?
						(m as IGenericParameterProvider).GenericsToString()
						: String.Empty)
				);

			var removedEntities = results[1].Select(m => m.Name +
				(
					m is IGenericParameterProvider ?
						(m as IGenericParameterProvider).GenericsToString()
						: String.Empty)
				);

			XElement result =
				logElement.Descendants().FirstOrDefault(
					n => n.Name == "Changes" && n.Attribute("in") != null && n.Attribute("in").Value == parentName);
			
			if(result==null)
			{
				result =  new XElement("Changes");
				result.SetAttributeValue("in", parentName);
				logElement.Add(result);
			}
			
			result.Add(removedEntities.Select(m => new XElement(String.Format("Remove-{0}", entityType), m)));
			result.Add(addedEntites.Select(m => new XElement(String.Format("Add-{0}", entityType), m)));
		}

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
	}
}
