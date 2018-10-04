using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Darkengines.Expressions.Models {
	public class UnaryExpressionModel: ExpressionModel {
		public ExpressionModel Operand { get; set; }
		public ExpressionType ExpressionType { get; set; }
	}
}
