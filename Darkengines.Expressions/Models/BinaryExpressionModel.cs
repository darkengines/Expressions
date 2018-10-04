using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Darkengines.Expressions.Models {
	public class BinaryExpressionModel : ExpressionModel {
		public ExpressionModel Left { get; set; }
		public ExpressionModel Right { get; set; }
		public ExpressionType ExpressionType { get; set; }
	}
}
