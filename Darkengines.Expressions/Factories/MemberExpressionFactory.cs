using Darkengines.Expressions.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Darkengines.Expressions.Factories {
	public class MemberExpressionFactory : ExpressionFactory<MemberExpressionModel> {
		public override Expression BuildExpression(MemberExpressionModel expressionModel, ExpressionFactoryContext context, ExpressionFactoryScope scope) {
			var objectFactory = context.ExpressionFactories.FindExpressionFactoryFor(expressionModel.Object, context, scope);
			var objectExpression = objectFactory.BuildExpression(expressionModel.Object, context, scope);
			var propertyInfo = objectExpression.Type.GetProperty(expressionModel.PropertyName);
			return Expression.MakeMemberAccess(objectExpression, propertyInfo);
		}
	}
}
