using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Mono.Cecil;

namespace AsmChecker
{
	public static class Dump
	{
		public static XElement MakeDumps(IEnumerable<string> fileList)
		{
			XElement dumpXml = new XElement("CompatibilityInfo");
			dumpXml.SetAttributeValue("Name", "Compatibility info dump");

			foreach (string assemblyFile in fileList)
			{
				XElement assemblyXml = DumpAssembly(assemblyFile);
				if (assemblyXml.HasElements)
					dumpXml.Add(assemblyXml);
			}

			return dumpXml;
		}

		public static XElement DumpAssembly(string assemblyFile)
		{
			AssemblyDefinition source = AssemblyDefinition.ReadAssembly(assemblyFile);
			
			if (source == null)
			{
				return null;
			}
			
			List<TypeDefinition> sourceTypes;
			
			try
			{
				sourceTypes = source.Modules.SelectMany(m => m.Types).Where(t => t.IsPublic).ToList();
				(source.MainModule.AssemblyResolver as DefaultAssemblyResolver).AddSearchDirectory(Path.GetDirectoryName(assemblyFile));
				sourceTypes.AddRange(source.Modules.SelectMany(m => m.ExportedTypes).Select(r => r.Resolve()).Where(t => t.IsPublic));
			}
			catch (Exception e)
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

		public static XElement DumpType(TypeDefinition type)
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

		public static XElement DumpMethod(MethodDefinition method)
		{
			XElement methodXml = new XElement(method.IsGetter || method.IsSetter ? "Accessor" : "Method");
			methodXml.SetAttributeValue("Name", method.Name + method.GenericsToString());
			methodXml.SetAttributeValue("ReturnType", method.ReturnType);
			methodXml.SetAttributeValue("Static", method.IsStatic ? "true" : null);
			methodXml.SetAttributeValue("Virtual", method.IsVirtual ? "true" : null);
			methodXml.SetAttributeValue("Override", method.IsVirtual && !method.IsNewSlot ? "true" : null);

			if (method.HasParameters)
			{
				foreach (ParameterDefinition parameter in method.Parameters)
				{
					XElement paramXml = new XElement("Parameter");
					paramXml.SetAttributeValue("Name", parameter.Name);
					paramXml.SetAttributeValue("Type", parameter.ParameterType);
					methodXml.Add(paramXml);
				}
			}
			return methodXml;
		}

		public static void ApplyPatch(XElement source, XElement patch)
		{
			if (patch == null)
				return;

			source.SetAttributeValue("Compatible", patch.GetValue("Compatible"));

			foreach (XElement element in source.Elements().ExceptAccessorsAndParameters())
			{
				ApplyPatch(element, patch.Elements(element.Name.LocalName).
										ExceptAccessorsAndParameters().SingleOrDefault(e => Check.AreCompatible(element, e)));

			}
		}

		public static void ClearPatches(XElement source)
		{
			foreach (XElement element in source.Descendants().Where(e => e.Attribute("Compatible") != null))
			{
				element.SetAttributeValue("Compatible",null);
			}
		}
	}
}
