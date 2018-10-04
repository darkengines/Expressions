using Darkengines.Expressions.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Darkengines.Expressions.Factories {
	public interface IExpressionFactory {
		Expression BuildExpression(ExpressionModel expressionModel, ExpressionFactoryContext context, ExpressionFactoryScope scope);
		bool CanHandle(ExpressionModel expressionModel, ExpressionFactoryContext context, ExpressionFactoryScope scope);
	}
}
