using Darkengines.Expressions.Models;
using DarkEngines.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Darkengines.Expressions.Factories {
	public class ArrayExpressionFactory : ExpressionFactory<ArrayExpressionModel> {
		public override Expression BuildExpression(ArrayExpressionModel expressionModel, ExpressionFactoryContext context, ExpressionFactoryScope scope) {
			var targetType = scope.TargetType != null ? scope.TargetType.GetEnumerableUnderlyingType() : null;
			var itemScope = new ExpressionFactoryScope(scope, targetType);

			var itemExpressions = expressionModel.Items.Select(itemExpressionModel => {
				var factory = context.ExpressionFactories.FindExpressionFactoryFor(itemExpressionModel, context, itemScope);
				var expression = factory.BuildExpression(itemExpressionModel, context, itemScope);
				if (expression.Type != targetType) expression = Expression.Convert(expression, targetType);
				return expression;
			}).ToArray();
			return Expression.NewArrayInit(targetType, itemExpressions);
		}
	}
}
