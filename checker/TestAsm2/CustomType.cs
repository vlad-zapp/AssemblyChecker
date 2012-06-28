using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TestAsm3;

namespace TestAsm2
{
	public class CustomType
	{
		public int id = 0;
		public CustomReturnType CustomReturn;
		public CustomReturnType CustomReturnAsProperty { get; set; }
	}
}
