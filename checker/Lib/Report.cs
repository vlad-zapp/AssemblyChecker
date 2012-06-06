using System.Linq;
using System.Xml.Linq;

namespace AsmChecker
{
	public static class Report
	{
		public static XElement GenerateReport(XElement source, bool? compatibleValue = false)
		{
			XElement report = new XElement(source);
			MakeReportNode(report, compatibleValue);
			return report;
		}

		public static bool MakeReportNode(XElement source, bool? compatibleValue=false)
		{
			if ((source.GetValue("Compatible")!=null && compatibleValue==null) ||
				(source.GetValue("Compatible") == compatibleValue.ToString().ToLowerInvariant()))
			{
				source.Elements().Where(e => e.Name != "Parameter" && e.Name != "Accessor").Remove();
				return true;
			}

			if (!source.HasElements)
				return false;

			source.Elements().Where(e => !MakeReportNode(e,compatibleValue)).Remove();
			return source.HasElements;
		}
	}
}