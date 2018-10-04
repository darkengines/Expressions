using Darkengines.Expressions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Darkengines.Expressions.Factories {
	public static class IEnumerableExtensions {
		public static IExpressionFactory FindExpressionFactoryFor(this IEnumerable<IExpressionFactory> expressionFactories, ExpressionModel expressionModel, ExpressionFactoryContext context, ExpressionFactoryScope scope) {
			return expressionFactories.First(expressionFactory => expressionFactory.CanHandle(expressionModel, context, scope));
		}
	}
}
