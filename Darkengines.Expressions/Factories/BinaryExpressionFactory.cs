using Darkengines.Expressions.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Darkengines.Expressions.Factories {
	public class BinaryExpressionFactory : ExpressionFactory<BinaryExpressionModel> {
		public override Expression BuildExpression(BinaryExpressionModel expressionModel, ExpressionFactoryContext context, ExpressionFactoryScope scope) {
			var leftExpressionFactory = context.ExpressionFactories.FindExpressionFactoryFor(expressionModel.Left, context, scope);
			var rightExpressionFactory = context.ExpressionFactories.FindExpressionFactoryFor(expressionModel.Right, context, scope);

			var left = leftExpressionFactory.BuildExpression(expressionModel.Left, context, scope);
			var right = rightExpressionFactory.BuildExpression(expressionModel.Right, context, scope);

			if (left.Type != right.Type) {
				if (expressionModel.Left is ConstantExpressionModel && !(expressionModel.Right is ConstantExpressionModel)) {
					left = Expression.Convert(left, right.Type);
				} else {
					right = Expression.Convert(right, left.Type);
				}
			}

			return Expression.MakeBinary(expressionModel.ExpressionType, left, right);
		}
	}
}
