using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestAssembly1
{
	public class Class1
	{
		public int method1(int a, string b, List<Delegate> c)
		{
			return 0;
		}

		private int method2(int a, string b, List<Delegate> c)
		{
			return 0;
		}

		protected int method3(int a, string b, List<Delegate> c)
		{
			return 0;
		}

		internal int method4(int a, string b, List<Delegate> c)
		{
			return 0;
		}

		public void genericMegaMethod<A,B,C>(int d, string e, List<IEnumerable<Action<Dictionary<A,B>>>> f)
		{

		}
	}
}
