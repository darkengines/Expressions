using System;
using System.Collections.Generic;
using System.Text;

namespace Darkengines.Expressions.Models {
	public class LambdaExpressionModel: ExpressionModel {
		public IEnumerable<ParameterExpressionModel> Parameters { get; set; }
		public ExpressionModel Body { get; set; }
	}
}
