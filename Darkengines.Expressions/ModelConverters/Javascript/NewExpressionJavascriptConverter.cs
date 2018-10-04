using Darkengines.Expressions.Models;
using Esprima.Ast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Darkengines.Expressions.ModelConverters.Javascript {
	public class NewExpressionJavascriptConverter : JavascriptModelConverter<ObjectExpression> {
		public override ExpressionModel Convert(ObjectExpression node, ModelConverterContext context) {
			var propertiesModels = node.Properties.Select(property => {
				var valueConverter = context.ModelConverters.FindModelConverterFor(property.Value, context);
				return new PropertyExpressionModel() {
					PropertyName = property.Key.As<Identifier>().Name,
					Value = valueConverter.Convert(property.Value, context)
				};
			}).ToArray();
			return new NewExpressionModel() {
				Properties = propertiesModels
			};
		}
	}
}
