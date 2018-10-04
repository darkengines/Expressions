using Darkengines.Expressions.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Darkengines.Expressions.Factories {
	public abstract class ExpressionFactory<TExpressionModel> : IExpressionFactory where TExpressionModel : ExpressionModel {
		public abstract Expression BuildExpression(TExpressionModel expressionModel, ExpressionFactoryContext context, ExpressionFactoryScope scope);
		public virtual bool CanHandle(ExpressionModel expressionModel, ExpressionFactoryContext context, ExpressionFactoryScope scope) {
			return expressionModel is TExpressionModel;
		}
		Expression IExpressionFactory.BuildExpression(ExpressionModel expressionModel, ExpressionFactoryContext context, ExpressionFactoryScope scope) {
			return BuildExpression((TExpressionModel)expressionModel, context, scope);
		}
	}
}
