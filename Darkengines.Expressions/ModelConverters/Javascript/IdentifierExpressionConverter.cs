using Darkengines.Expressions.Models;
using Esprima.Ast;
using System;
using System.Collections.Generic;
using System.Text;

namespace Darkengines.Expressions.ModelConverters.Javascript {
	public class IdentifierExpressionConverter : JavascriptModelConverter<Identifier> {
		public override ExpressionModel Convert(Identifier node, ModelConverterContext context) {
			return new IdentifierExpressionModel() {
				Name = node.Name
			};
		}
	}
}
