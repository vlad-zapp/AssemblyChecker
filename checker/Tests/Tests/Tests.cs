using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using NUnit.Framework;

namespace Tests
{
	[TestFixture]
	public class Tests
	{
		//We may put them as compiled assemblies to tests\bin\debug.
		//But we need to test resolving of forwarded types. So let them stay in default places
		const string asm1 = @"..\..\..\TestAssembly1\bin\Debug\TestAssembly1.dll";
		const string asm2 = @"..\..\..\TestAssembly2\bin\Debug\TestAssembly2.dll";

		[Test]
		public void MethodSignatureTest()
		{
			XElement result = AsmChecker.Dump.MakeDumps(new []{asm1});
			XElement assemblyDump = result.Elements().SingleOrDefault(a => a.Name.LocalName == "Assembly");
			Assert.That(assemblyDump.Attribute("Name").Value=="TestAssembly1");
		}
	}
}
