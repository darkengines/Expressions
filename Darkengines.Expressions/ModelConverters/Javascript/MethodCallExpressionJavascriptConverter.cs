using Darkengines.Expressions.Models;
using Esprima.Ast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Darkengines.Expressions.ModelConverters.Javascript {
	public class MethodCallExpressionJavascriptConverter : JavascriptModelConverter<CallExpression> {
		public override ExpressionModel Convert(CallExpression node, ModelConverterContext context) {
			var calleeConverter = context.ModelConverters.FindModelConverterFor(node.Callee, context);
			var calleeModel = calleeConverter.Convert(node.Callee, context);
			var arguments = node.Arguments.Select(argument => {
				var converter = context.ModelConverters.FindModelConverterFor(argument, context);
				var model = converter.Convert(argument, context);
				return model;
			}).ToArray();
			return new MethodCallExpressionModel() {
				Arguments = arguments,
				Callee = calleeModel
			};
		}
	}
}
