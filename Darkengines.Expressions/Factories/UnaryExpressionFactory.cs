using Darkengines.Expressions.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Darkengines.Expressions.Factories {
	public class UnaryExpressionFactory : ExpressionFactory<UnaryExpressionModel> {
		public override Expression BuildExpression(UnaryExpressionModel expressionModel, ExpressionFactoryContext context, ExpressionFactoryScope scope) {
			var operandFactory = context.ExpressionFactories.FindExpressionFactoryFor(expressionModel.Operand, context, scope);
			var operandExpression = operandFactory.BuildExpression(expressionModel.Operand, context, scope);
			return Expression.MakeUnary(expressionModel.ExpressionType, operandExpression, scope.TargetType);
		}
	}
}
