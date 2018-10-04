using System;
using System.Collections.Generic;
using System.Text;

namespace Darkengines.Expressions.Models {
	public class NewExpressionModel: ExpressionModel {
		public IEnumerable<PropertyExpressionModel> Properties { get; set; }
	}
}
