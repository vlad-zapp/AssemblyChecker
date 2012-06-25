using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
			XElement first = XElement.Parse("<Method Name='test'></Assembly>");
			XElement second = XElement.Parse("<Method Name='test'></Assembly>");
			
		}

		#region AreCompatible checks

		[Test]
		public void CheckType()
		{
			
		}

		#endregion
	}
}
