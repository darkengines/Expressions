using Darkengines.Expressions.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Darkengines.Expressions.Factories {
	public class CustomExpressionFactory : ExpressionFactory<ExpressionModel> {
		protected Func<ExpressionModel, ExpressionFactoryContext, ExpressionFactoryScope, Expression> FactoryFunction { get; }
		public CustomExpressionFactory(Func<ExpressionModel, ExpressionFactoryContext, ExpressionFactoryScope, Expression> factoryFunction) {
			FactoryFunction = factoryFunction;
		}
		public override Expression BuildExpression(ExpressionModel expressionModel, ExpressionFactoryContext context, ExpressionFactoryScope scope) {
			return FactoryFunction(expressionModel, context, scope);
		}
	}
}
