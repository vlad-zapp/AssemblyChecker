using System.Linq;
using System.Xml.Linq;

namespace AsmChecker
{
	public static class Report
	{
		public static XElement GenerateReport(XElement source)
		{
			XElement report = new XElement(source);
			MakeReportNode(report);
			return report;
		}

		public static bool MakeReportNode(XElement source)
		{
			if (source.GetValue("Compatible") == "false")
			{
				source.Elements().Where(e => e.Name != "Parameter" && e.Name != "Accessor").Remove();
				return true;
			}

			if (!source.HasElements)
				return false;

			source.Elements().Where(e => !MakeReportNode(e)).Remove();
			return source.HasElements;
		}
	}
}
