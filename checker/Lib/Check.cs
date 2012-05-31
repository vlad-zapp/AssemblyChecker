using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace AsmChecker
{
	public static class Check
	{
		public static void CheckAssemblies(IEnumerable<XElement> first, IEnumerable<XElement> second)
		{
			foreach (XElement assembly in first.Where(a => !a.IsUntouchable()))
			{
				XElement analogInSecond = second.FirstOrDefault(a => AreCompatible(assembly, a));

				if (analogInSecond == null)
				{
					assembly.SetAttributeValue("Compatible", "false");
					continue;
				}

				CheckTypes(assembly.SelectTypes().Where(e => !e.IsUntouchable()), analogInSecond.SelectTypes());
			}
		}

		public static void CheckTypes(IEnumerable<XElement> first, IEnumerable<XElement> second)
		{
			foreach (XElement type in first)
			{
				XElement analogInSecond = second.FirstOrDefault(t => AreCompatible(type, t));

				if (analogInSecond == null)
				{
					type.SetAttributeValue("Compatible", "false");
					continue;
				}

				foreach (XElement member in type.Elements().Where(t => !t.IsUntouchable()))
				{
					if (!analogInSecond.Elements(member.Name.LocalName).Any(m => AreCompatible(member, m)))
					{
						member.SetAttributeValue("Compatible", "false");
					}
				}

				CheckTypes(type.Elements("Type").Where(t => !t.IsUntouchable()), analogInSecond.Elements("Type"));
			}
		}
		#region Compatibility checks

		//One generic compatibility check for all. Needed for reports and stuff
		public static bool AreCompatible(XElement first, XElement second)
		{
			//get method parameters 
			IEnumerable<string> params1 = first.Elements("Parameter").Select(m => m.Attribute("Type").Value).ToArray();
			IEnumerable<string> params2 = second.Elements("Parameter").Select(m => m.Attribute("Type").Value).ToArray();
				
			//TODO: optimization?
			return
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
				//enums: check value
				first.GetValue("Value") == second.GetValue("Value") &&
				//methods: check parameters		
				Enumerable.SequenceEqual(params1, params2) &&
				//Check properties acessors
				first.Elements("Acessor").All(m => second.Elements("Acessor").Any(n => AreCompatible(m, n)));	
		}

		#endregion
	}
}
