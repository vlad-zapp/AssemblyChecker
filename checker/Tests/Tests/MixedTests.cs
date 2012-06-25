using System.Linq;
using System.Xml.Linq;
using AsmChecker;
using Mono.Cecil;
using NUnit.Framework;

namespace Tests
{
	[TestFixture]
	class MixedTests
	{
		private XElement dump;

		[SetUp]
		public void SetUp()
		{
			dump = Dump.MakeDumps(new[] { "testasm.dll" });
		}

		[Test]
		public void ForwardedTypeDumpTest()
		{
			XElement intForwarding = dump.Element("Assembly").Elements("Struct").Single(c => c.Attribute("Name").Value == "Int32" && c.Attribute("Path").Value == "System");
			Assert.That(intForwarding, Is.Not.Null);

			//dump real Int32 and compare the results
			TypeReference RealInt32 = ModuleDefinition.CreateModule("dummy", ModuleKind.Dll).Import(typeof(int));
			Assert.That(Check.AreCompatible(AsmChecker.Dump.DumpType(RealInt32.Resolve()), intForwarding));
		}

		[Test]
		public void TripleDumpAndCheckTest()
		{
			XElement dump1 = Dump.MakeDumps(new[] { "testasm.dll" });
			XElement dump2 = Dump.MakeDumps(new[] { "testasm.dll" });

			Assert.That(dump1.ToString()==dump2.ToString());
			Assert.That(dump1.ToString() == dump.ToString());
			Assert.That(Check.AreCompatible(dump1,dump2));
			Assert.That(Check.AreCompatible(dump1,dump));
			Assert.That(Check.AreCompatible(dump2,dump));
		}

		[Test]
		public void ApplySimplePatchTest()
		{
			Assert.That(dump.Element("Assembly").Attribute("Compatible") == null);
			XElement patch = XElement.Parse("<dump><Assembly Name='TestAsm' Compatible='True'/></dump>");
			Dump.ApplyPatch(dump,patch);
			Assert.That(dump.Element("Assembly").Attribute("Compatible").Value=="true");
		}

		[Test]
		public void ApplyCompletePatchTest()
		{
			XElement patch = new XElement(dump);

			foreach (XElement element in patch.Descendants().Where(d=>d.Name.LocalName!="Parameter"))
			{
				element.SetAttributeValue("Compatible","true");
			}

			Dump.ApplyPatch(dump,patch);
			Assert.That(patch.Descendants().Where(d => d.Name.LocalName != "Parameter").All(d=>d.GetValue("Compatible")=="true"));
		}
	}
}
