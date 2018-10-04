using Darkengines.Expressions.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Darkengines.Expressions.Factories {
	public class ConstantExpressionFactory : ExpressionFactory<ConstantExpressionModel> {
		public override Expression BuildExpression(ConstantExpressionModel expressionModel, ExpressionFactoryContext context, ExpressionFactoryScope scope) {
			return Expression.Constant(expressionModel.Value);
		}
	}
}
