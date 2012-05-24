﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace testLib
{
	public class ClassGenericsChanged<T1,T2>
	{
		public string SayHello()
		{
			return "hello!";
		}

		public string SayHelloToGenerics<S1,S2>()
		{
			return "hello, generics: "+typeof(S1)+" "+typeof(S2);
		}

		public string SayHelloToOwnersGenerics<T1,T2>()
		{
			return "hello, owners generics: " + typeof(T1) + " " + typeof(T2);
		}
	}

	public class MethodGenericsChanged<T1, T2>
	{
		public string SayHello()
		{
			return "hello!";
		}

		public string SayHelloToGenerics<S1, S2>()
		{
			return "hello, generics: " + typeof(S1) + " " + typeof(S2);
		}

		public string SayHelloToOwnersGenerics<T1, T2>(bool test)
		{
			return "hello, owners generics: " + typeof(T1) + " " + typeof(T2);
		}
	}
}
