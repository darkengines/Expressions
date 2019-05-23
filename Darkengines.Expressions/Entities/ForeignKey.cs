using System;
using System.Collections.Generic;
using System.Text;

namespace Darkengines.Expressions.Entities {
	public class ForeignKey {
		public string[] Properties { get; set; }
		public string Dependent { get; set; }
		public string Principal { get; set; }
		public string PrincipalToDependent { get; set; }
	}
}
