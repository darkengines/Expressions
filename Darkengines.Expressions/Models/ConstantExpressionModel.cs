using System;
using System.Collections.Generic;
using System.Text;

namespace Darkengines.Expressions.Models {
	public class ConstantExpressionModel : ExpressionModel {
		public object Value { get; set; }
	}
}
