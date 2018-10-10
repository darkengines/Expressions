using System;
using System.Collections.Generic;
using System.Text;

namespace Darkengines.Expressions.Models {
	public class ArrayExpressionModel: ExpressionModel {
		public IEnumerable<ExpressionModel> Items { get; set; }
	}
}
