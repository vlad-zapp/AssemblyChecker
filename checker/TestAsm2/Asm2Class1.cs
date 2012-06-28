using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TestAsm3;

namespace TestAsm2
{
	public class Asm2Class1:TestAsm3.Asm3Class1
	{
		public void Method1()
		{

		}

		public virtual void Method2()
		{

		}

		public void Method3()
		{

		}

		public virtual void Method4()
		{

		}

		public Asm2Class1 ReturnIteself()
		{
			return this;
		}

		public CustomReturnType MethodWithReturnTypeFromAnotherAsm(CustomType CustomTypeVar)
		{
			return null;
		}
	}
}
