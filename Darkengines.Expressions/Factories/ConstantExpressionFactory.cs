using Darkengines.Expressions.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Darkengines.Expressions.Factories {
	public class ConstantExpressionFactory : ExpressionFactory<ConstantExpressionModel> {
		public override Expression BuildExpression(ConstantExpressionModel expressionModel, ExpressionFactoryContext context, ExpressionFactoryScope scope) {
			var expression = (Expression)Expression.Constant(expressionModel.Value);
			if (scope.TargetType != null && !scope.TargetType.IsGenericParameter && !scope.TargetType.IsGenericType && !scope.TargetType.IsAssignableFrom(expression.Type)) expression = Expression.Convert(expression, scope.TargetType);
			return expression;
		}
	}
}
