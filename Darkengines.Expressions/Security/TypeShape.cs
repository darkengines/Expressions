using System;
using System.Collections.Generic;
using System.Text;

namespace Darkengines.Expressions.Security {
	public class TypeShape {
		public string Name { get; set; }
		public IEnumerable<Tuple<string, Type>> Properties { get; set; }
		public IEnumerable<Tuple<string, TypeShape>> Relations { get; set; }
	}
}
