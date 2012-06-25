using System.Linq;
using System.Xml.Linq;
using AsmChecker;
using NUnit.Framework;

namespace Tests
{
	[TestFixture]
	public class ReportTests
	{
		private XElement dump;

		[SetUp]
		public void SetUp()
		{
			dump = Dump.MakeDumps(new[] { "testasm.dll" });
		}

		[Test]
		public void EmptyReportTest()
		{
			XElement second = new XElement(dump);
			Check.CheckAssemblies(dump.Elements(),second.Elements());
			XElement report = Report.GenerateReport(dump);
			Assert.That(report.Elements().Count() == 0);
		}

		[Test]
		public void CompleteReportTest()
		{
			XElement second = new XElement("dump2");
			Check.CheckAssemblies(dump.Elements(), second.Elements());
			XElement report = Report.GenerateReport(dump);
			Assert.That(report.Elements().Single().GetValue("Compatible")=="false");
		}

		[Test]
		public void IncompatibleMembersTest()
		{
			XElement second = XElement.Parse("<dump><Assembly Name='TestAsm' /></dump>");
			Check.CheckAssemblies(dump.Elements(), second.Elements());
			XElement report = Report.GenerateReport(dump);
			Assert.That(report.Element("Assembly").Elements().Count() == dump.Element("Assembly").Elements().Count());
			Assert.That(report.Element("Assembly").Elements().All(e=>e.GetValue("Compatible")=="false"));
		}
	}
}
