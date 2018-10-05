using Darkengines.Expressions.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Darkengines.Expressions.Factories {
	public class IdentifierExpressionFactory : ExpressionFactory<IdentifierExpressionModel> {
		public override Expression BuildExpression(IdentifierExpressionModel expressionModel, ExpressionFactoryContext context, ExpressionFactoryScope scope) {
			return scope.FindIdentifier(expressionModel.Name);
		}
	}
}
