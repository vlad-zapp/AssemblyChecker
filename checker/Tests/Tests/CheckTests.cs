using System.Linq;
using System.Xml.Linq;
using AsmChecker;
using NUnit.Framework;

namespace Tests
{
	class CheckTests
	{
		private bool IsCompatible(XElement source)
		{
			return source.Descendants().Where(d => d.GetValue("Compatible") == "false").Count() == 0;
		}

		[Test]
		public void CheckSameAssemblies()
		{
			XElement dump = Dump.MakeDumps(new[] { "testasm.dll" });
			Check.CheckAssemblies(dump.Elements(),dump.Elements());
			Assert.That(dump.Descendants().Where(d=>d.GetValue("Compatible")=="false").Count()==0);
		}

		[Test]
		public void CheckAssemblies()
		{
			XElement first = XElement.Parse("<dump><Assembly Name='test'></Assembly></dump>");
			XElement second = XElement.Parse("<dump><Assembly Name='test'></Assembly></dump>");
			Check.CheckAssemblies(first.Elements(), second.Elements());
			Assert.That(IsCompatible(first));
			first = XElement.Parse("<dump><Assembly Name='test2'></Assembly></dump>");
			Check.CheckAssemblies(first.Elements(), second.Elements());
			Assert.That(!IsCompatible(first));
			first = XElement.Parse("<dump><NotAnAssembly Name='test'></NotAnAssembly></dump>");
			Check.CheckAssemblies(first.Elements(), second.Elements());
			Assert.That(!IsCompatible(first));
			first = XElement.Parse("<dump><Assembly Name='test'><Class Name='testClass'></Class></Assembly></dump>");
			second = XElement.Parse("<dump><Assembly Name='test'><Class Name='testClass'></Class></Assembly></dump>");
			Check.CheckAssemblies(first.Elements(), second.Elements());
			Assert.That(IsCompatible(first));
			first = XElement.Parse("<dump><Assembly Name='test'><Class Name='testClass2'></Class></Assembly></dump>");
			Check.CheckAssemblies(first.Elements(), second.Elements());
			Assert.That(!IsCompatible(first));
		}

		[Test]
		public void CheckTypes()
		{
			XElement first = XElement.Parse("<Assembly Name='test'><Class Name='testClass'></Class></Assembly>");
			XElement second = XElement.Parse("<Assembly Name='test'><Class Name='testClass'></Class></Assembly>");
			Check.CheckTypes(first.Elements(), second.Elements());
			Assert.That(IsCompatible(first));
			first = XElement.Parse("<Assembly Name='test'><Class></Class></Assembly>");
			Check.CheckTypes(first.Elements(), second.Elements());
			Assert.That(!IsCompatible(first));

			first = XElement.Parse("<Assembly Name='test'><Class Name='testClass' Abstract='true'></Class></Assembly>");
			second = XElement.Parse("<Assembly Name='test'><Class Name='testClass' Abstract='true'></Class></Assembly>");
			Check.CheckTypes(first.Elements(), second.Elements());
			Assert.That(IsCompatible(first));
			first = XElement.Parse("<Assembly Name='test'><Class Name='testClass'></Class></Assembly>");
			Check.CheckTypes(first.Elements(), second.Elements());
			Assert.That(!IsCompatible(first));

			first = XElement.Parse("<Assembly Name='test'><Class Name='testClass' Static='true'></Class></Assembly>");
			second = XElement.Parse("<Assembly Name='test'><Class Name='testClass' Static='true'></Class></Assembly>");
			Check.CheckTypes(first.Elements(), second.Elements());
			Assert.That(IsCompatible(first));
			first = XElement.Parse("<Assembly Name='test'><Class Name='testClass'></Class></Assembly>");
			Check.CheckTypes(first.Elements(), second.Elements());
			Assert.That(!IsCompatible(first));

			first = XElement.Parse("<Assembly Name='test'><Class Name='testClass' Path='System.Test'></Class></Assembly>");
			second = XElement.Parse("<Assembly Name='test'><Class Name='testClass' Path='System.Test'></Class></Assembly>");
			Check.CheckTypes(first.Elements(), second.Elements());
			Assert.That(IsCompatible(first));
			first = XElement.Parse("<Assembly Name='test'><Class Name='testClass'></Class></Assembly>");
			Check.CheckTypes(first.Elements(), second.Elements());
			Assert.That(!IsCompatible(first));
		}

		#region AreCompatible checks

		private void CheckAttributesCompatibility(XElement source, string attributeName, string value1, string value2)
		{
			XElement second = new XElement(source);
			source.SetAttributeValue(attributeName,value1);
			second.SetAttributeValue(attributeName, value1);
			Assert.True(Check.AreCompatible(source,second));
			Assert.True(Check.AreCompatible(second,source));
			second.SetAttributeValue(attributeName, value2);
			Assert.False(Check.AreCompatible(source, second));
			Assert.False(Check.AreCompatible(second, source));
			source.SetAttributeValue(attributeName, value2);
			second.SetAttributeValue(attributeName, value2);
			Assert.True(Check.AreCompatible(source, second));
			Assert.True(Check.AreCompatible(second, source));
			second.SetAttributeValue(attributeName, value1);
			Assert.False(Check.AreCompatible(source, second));
			Assert.False(Check.AreCompatible(second, source));
		}

		[Test]
		public void CheckElementType()
		{
			Assert.True(Check.AreCompatible(XElement.Parse("<Method />"), XElement.Parse("<Method />")));
			Assert.False(Check.AreCompatible(XElement.Parse("<Method />"), XElement.Parse("<Interface />")));
		}

		[Test]
		public void CheckElementPath()
		{ 
			CheckAttributesCompatibility(XElement.Parse("<Interface />"), "Path", "System.Test", "System.Test2");
		}

		[Test]
		public void CheckElementReturnType()
		{
			CheckAttributesCompatibility(XElement.Parse("<Method />"), "ReturnType", "System.Int32", "System.Void");
		}

		[Test]
		public void CheckElementAbstract()
		{
			CheckAttributesCompatibility(XElement.Parse("<Class />"), "Abstract", "true", null);
		}
		[Test]
		public void CheckElementVirtual()
		{
			CheckAttributesCompatibility(XElement.Parse("<Class />"), "Virtual", "true", null);
		}
		[Test]
		public void CheckElementOverride()
		{
			CheckAttributesCompatibility(XElement.Parse("<Class />"), "Override", "true", null);
		}
		[Test]
		public void CheckElementValue()
		{
			CheckAttributesCompatibility(XElement.Parse("<Class />"), "Value", "1", "0");
		}

		[Test]
		public void CheckElementAccessors()
		{
			XElement Property1 = XElement.Parse("<Property><Accessor></Accessor></Property>");
			XElement Property2 = XElement.Parse("<Property><Accessor></Accessor></Property>");
			Assert.True(Check.AreCompatible(Property1,Property2));
			Property2 = XElement.Parse("<Property><Accessor Name='get_Property' ReturnType='system.Int32'></Accessor></Property>");
			Assert.False(Check.AreCompatible(Property1, Property2));
		}

		#endregion
	}
}
