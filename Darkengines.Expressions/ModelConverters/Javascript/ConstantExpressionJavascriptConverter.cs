using Darkengines.Expressions.Models;
using Esprima.Ast;
using System;
using System.Collections.Generic;
using System.Text;

namespace Darkengines.Expressions.ModelConverters.Javascript {
	public class ConstantExpressionJavascriptConverter : JavascriptModelConverter<Literal> {
		public override ExpressionModel Convert(Literal node, ModelConverterContext context) {
			return new ConstantExpressionModel() {
				Value = node.Value
			};
		}
	}
}
