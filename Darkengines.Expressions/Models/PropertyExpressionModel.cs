using System;
using System.Collections.Generic;
using System.Text;

namespace Darkengines.Expressions.Models {
	public class PropertyExpressionModel: ExpressionModel {
		public ExpressionModel Value { get; set; }
		public string PropertyName { get; set; }
	}
}
