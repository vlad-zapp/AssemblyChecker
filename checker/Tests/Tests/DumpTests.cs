using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using AsmChecker;

namespace Tests
{
	[TestFixture]
	public class DumpTests
	{
		private XElement dump, classDump;

		[SetUp]
		public void SetUp()
		{
			dump = Dump.MakeDumps(new[] { "testasm.dll" });
			classDump = dump.Element("Assembly").Elements("Class").Single(c=>c.Attribute("Name").Value=="Class1");
		}

		[Test]
		public void AssemblyDumpTest()
		{
			XElement assemblyDump = dump.Elements("Assembly").SingleOrDefault();

			Assert.That(assemblyDump, Is.Not.Null);
			Assert.That(assemblyDump.Attribute("Name").Value == "TestAsm");
		}

		public void ClassDumpTest()
		{
			Assert.That(classDump, Is.Not.Null);
			Assert.That(classDump.Attribute("Name").Value == "Class1");
		}

		[Test]
		public void MethodDumpTest()
		{
			XElement methodDump = classDump.Elements("Method").SingleOrDefault(m => m.GetValue("Name", false) == "testMethod1");

			Assert.That(methodDump, Is.Not.Null);
			Assert.That(methodDump.Attribute("Name").Value == "testMethod1");
			Assert.That(methodDump.Attribute("ReturnType").Value == typeof(int).ToString());
			Assert.That(methodDump.Elements("Parameter").Select(p => p.Attribute("Type").Value).SequenceEqual(new[] { typeof(int).ToString(), typeof(string).ToString() }));
			Assert.That(!classDump.Elements("Method").Any(m => m.Name.LocalName.StartsWith("hiddenMethod")));
		}

		[Test]
		public void EnumDumpTest()
		{
			XElement enumDump = dump.Element("Assembly").Elements("Enum").SingleOrDefault();
			Assert.That(enumDump, Is.Not.Null);
			Assert.That(enumDump.Elements("Field").Where(f => f.GetValue("Static") == "true").Select(fl => fl.Attribute("Value").Value).SequenceEqual(new[] { "0", "1", "2" }));
			Assert.That(enumDump.Elements("Field").Where(f => f.GetValue("Static") != "true").Single().Attribute("Name").Value == "value__");
		}

		[Test]
		public void FieldDumpTest()
		{
			XElement fieldDump = classDump.Elements("Field").SingleOrDefault();
			Assert.That(fieldDump, Is.Not.Null);
			Assert.That(fieldDump.Attribute("Name").Value == "testField");
			Assert.That(fieldDump.Attribute("Type").Value == typeof(double).ToString());
		}

		[Test]
		public void PropertyDumpTest()
		{
			List<XElement> propertyDump = classDump.Elements("Property").ToList();
			Assert.That(propertyDump.Count() == 3);

			XElement testProperty = propertyDump.Single(p => p.GetValue("Name",false) == "testProperty1");
			Assert.That(testProperty.Elements().SingleOrDefault(), Is.Not.Null);
			Assert.That(testProperty.Elements("Accessor").Single(a => a.Attribute("Name").Value == "get_" + testProperty.Attribute("Name").Value).Attribute("ReturnType").Value == testProperty.Attribute("Type").Value);
			
			testProperty = propertyDump.Single(p => p.GetValue("Name",false) == "testProperty2");
			Assert.That(testProperty.Elements().SingleOrDefault(), Is.Not.Null);
			Assert.That(testProperty.Elements("Accessor").Single(a => a.Attribute("Name").Value == "set_" + testProperty.Attribute("Name").Value).Elements("Parameter").Single().Attribute("Type").Value == testProperty.Attribute("Type").Value);
			
			testProperty = propertyDump.Single(p => p.GetValue("Name",false) == "testProperty3");
			Assert.That(testProperty.Elements().Count() == 2);
			Assert.That(testProperty.Elements("Accessor").Single(a => a.Attribute("Name").Value == "get_" + testProperty.Attribute("Name").Value).Attribute("ReturnType").Value == testProperty.Attribute("Type").Value);
			Assert.That(testProperty.Elements("Accessor").Single(a => a.Attribute("Name").Value == "set_" + testProperty.Attribute("Name").Value).Elements("Parameter").Single().Attribute("Type").Value == testProperty.Attribute("Type").Value);
		}

		[Test]
		public void NestedTypeDumpTest()
		{
			IEnumerable<XElement> NestedTypes = classDump.Elements().Where(e => e.GetValue("Name",false).StartsWith("nestedType"));
			Assert.That(NestedTypes.Select(t => t.Name.LocalName).SequenceEqual(new[] { "Struct", "Class" }));
			Assert.That(NestedTypes.Select(t => t.Attribute("Name").Value).SequenceEqual(new[] { "nestedType1", "nestedType2" }));
		}

		[Test]
		public void GenericsHandlingTest()
		{
			XElement megaMethod = classDump.Elements("Method").Single(m => m.GetValue("Name", false).StartsWith("genericMegaMethod<A,B,C>"));
			Assert.That(megaMethod.GetValue("ReturnType",false) == @"System.Func`2<B,C>");

			string theType =
				@"System.Collections.Generic.List`1<System.Collections.Generic.IEnumerable`1<System.Action`1<System.Collections.Generic.Dictionary`2<A,B>>>>";
			Assert.That(megaMethod.Elements("Parameter").Single().GetValue("Type",false)==theType);
		}
	}
}
