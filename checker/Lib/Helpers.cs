using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Mono.Cecil;

namespace AsmChecker
{
	public static class Helpers
	{
		public static bool IsUntouchable(this XElement e)
		{
			return e.GetValue("Compatible") == "true";
		}

		public static string GenericsToString(this IGenericParameterProvider self)
		{
			IEnumerable<string> genericsNames = self.GenericParameters.Select(m => m.Name);
			return genericsNames.Count() > 0 ? ("`" + String.Join(",", genericsNames)) : String.Empty;
		}

		public static string CorrectName(this TypeReference self, bool fullName = false)
		{
			string name = fullName ? self.FullName : self.Name;
			return self.HasGenericParameters
					? name.Substring(0, name.IndexOf('`')) + self.GenericsToString()
					: name;
		}

		public static void ProperSave(this XElement source, string filename)
		{
			string fullFileName = Path.GetFullPath(filename);
			using (XmlWriter writer = XmlWriter.Create(
					fullFileName, new XmlWriterSettings() { Indent = true, IndentChars = "\t" }))
			{
				source.Save(writer);
			}
		}

		public static string GetValue(this XElement self, string attributeName)
		{
			if (self.Attribute(attributeName) == null)
				return null;

			return self.Attribute(attributeName).Value.ToLowerInvariant();
		}

		public static IEnumerable<XElement> SelectTypes(this XElement source)
		{
			IEnumerable<string> types = new[] { "Type", "Struct", "Class", "Interface", "Enum" };
			return source.Elements().Where(s => types.Contains(s.Name.LocalName));
		}
	}
}
