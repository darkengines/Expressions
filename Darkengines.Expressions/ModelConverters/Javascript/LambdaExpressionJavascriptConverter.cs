using Darkengines.Expressions.Models;
using Esprima.Ast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Darkengines.Expressions.ModelConverters.Javascript {
	public class LambdaExpressionJavascriptConverter : JavascriptModelConverter<ArrowFunctionExpression> {
		public override ExpressionModel Convert(ArrowFunctionExpression node, ModelConverterContext context) {
			var parameters = node.Params.Select(parameter => {
				return new ParameterExpressionModel() { Name = parameter.As<Identifier>().Name };
			}).ToArray();
			var bodyConverter = context.ModelConverters.FindModelConverterFor(node.Body, context);
			var body = bodyConverter.Convert(node.Body, context);
			return new LambdaExpressionModel() {
				Parameters = parameters,
				Body = body
			};
		}
	}
}
