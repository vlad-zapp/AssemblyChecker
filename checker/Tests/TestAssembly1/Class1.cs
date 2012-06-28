using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestAssembly1
{
	public class Class1
	{
		public override bool Equals(object obj)
		{
			return false;
		}

		public int GetHashCode()
		{
			return base.GetHashCode();
		}

		public int testMethod1(int a, string b)
		{
			return 0;
		}

		private int hiddenMethod1(int a, string b, List<Delegate> c)
		{
			return 0;
		}

		protected int hiddenMethod2(int a, string b, List<Delegate> c)
		{
			return 0;
		}

		internal int hiddenMethod3(int a, string b, List<Delegate> c)
		{
			return 0;
		}

		public double testField;
		private double testField2;
		protected double testField3;
		internal double testField4;

		public Single testProperty1 { get; private set; }
		public Single testProperty2 { private get; set; }
		public Single testProperty3 { get; set; }
		private double testProperty4 { get; set; }
		protected double testProperty5 { get; set; }
		internal double testProperty6 { get; set; }

		public struct nestedType1 { }
		public class nestedType2 { }
		private class nestedType3 { }
		protected class nestedType4 { }
		internal class nestedType5 { }

		public Func<B,C> genericMegaMethod<A, B, C>(List<IEnumerable<Action<Dictionary<A, B>>>> f)
		{
			return null;
		}
	}


	public enum TestEnum
	{
		foo = 0,
		bar = 1,
		baz = 2
	}
}
