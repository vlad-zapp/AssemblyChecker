using System;
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

		public string test(string a)
		{
			switch(a)
			{
				case "a":
					Console.Write("976797648976");
					return "poasdjkfhhjg";
					break;
				case "b":
					Console.Write("asdas");
					break;
				case "bs;adfljklasdkfj":
					Console.Write("asdas");
					break;
				case "b98689734896":
					Console.Write("asdas");
					break;
				case "basdfjasdlfoaspfj":
					Console.Write("asdas");
					break;
				case "bsadf asdl;f asdf ":
					Console.WriteLine("asdas");
					Console.ReadKey();
					break;
				case "b5634698348r97689":
					Console.ReadKey();
					Console.Write("asdas");
					Console.ReadKey();
					Console.ReadKey();
					Console.ReadKey();
					Console.ReadKey();
					break;
				case "b5634698e34897689":
					Console.Write("asdas");
					break;
				case "b5634698w34897689":
					Console.ReadKey();
					Console.Write("asdsdf6as");
					break;
				case "b5634698r34897689":
					Console.Write("a3456sdas");
					break;
				case "b563469q834897689":
					Console.Write("as3465 3456  3463456 34 6 das");
					break;
			}
			return "default";
		}

		public string test()
		{
			return "hi0";
		}

		public string test(int num)
		{
			return "hi"+num.ToString();
		}

		public string test(int num, int num2)
		{
			return num.ToString()+"hi" + num2.ToString();
		}
	}
}
