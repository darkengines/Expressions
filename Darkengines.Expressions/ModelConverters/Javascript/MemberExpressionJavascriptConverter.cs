using Darkengines.Expressions.Models;
using Esprima.Ast;
using System;
using System.Collections.Generic;
using System.Text;

namespace Darkengines.Expressions.ModelConverters.Javascript {
	public class MemberExpressionJavascriptConverter : JavascriptModelConverter<MemberExpression> {
		public override ExpressionModel Convert(MemberExpression node, ModelConverterContext context) {
			var objectConverter = context.ModelConverters.FindModelConverterFor(node.Object, context);
			var objectModel = objectConverter.Convert(node.Object, context);
			var propertyName = node.Property.As<Identifier>().Name;
			return new MemberExpressionModel() {
				Object = objectModel,
				PropertyName = propertyName
			};
		}
	}
}
