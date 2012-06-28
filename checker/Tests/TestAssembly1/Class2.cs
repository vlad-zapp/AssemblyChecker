using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestAsm
{
	/// <summary>
	/// Just another dummy class in the assembly
	/// </summary>
	public class Class2:TestAsm2.Asm2Class1
	{
		public string SayHello()
		{
			return "hello!";
		}
	}
}
