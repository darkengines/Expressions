using Darkengines.Expressions.Models;
using Esprima.Ast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Darkengines.Expressions.ModelConverters.Javascript {
	public class ArrayExpressionJavascriptConverter : JavascriptModelConverter<ArrayExpression> {
		public override ExpressionModel Convert(ArrayExpression node, ModelConverterContext context) {
			return new ArrayExpressionModel() {
				Items = node.Elements.Select(element => {
					var converter = context.ModelConverters.FindModelConverterFor(element, context);
					return converter.Convert(element, context);
				})
			};
		}
	}
}
