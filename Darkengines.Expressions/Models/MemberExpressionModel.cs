using System;
using System.Collections.Generic;
using System.Text;

namespace Darkengines.Expressions.Models {
	public class MemberExpressionModel: ExpressionModel {
		public ExpressionModel Object { get; set; }
		public string PropertyName { get; set; }
	}
}
