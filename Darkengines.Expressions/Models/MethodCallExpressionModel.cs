using System;
using System.Collections.Generic;
using System.Text;

namespace Darkengines.Expressions.Models {
	public class MethodCallExpressionModel : ExpressionModel {
		public ExpressionModel Callee { get; set; }
		public IEnumerable<ExpressionModel> Arguments { get; set; }
	}
}
