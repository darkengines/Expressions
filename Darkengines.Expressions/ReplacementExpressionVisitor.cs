using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Darkengines.Expressions {
	public class ReplacementExpressionVisitor : ExpressionVisitor {
		protected IDictionary<Expression, Expression> Map { get; }
		public ReplacementExpressionVisitor(IDictionary<Expression, Expression> map) {
			Map = map;
		}
		public override Expression Visit(Expression node) {
			Expression expression = null;
			var found = Map.TryGetValue(node, out expression);
			if (found) return expression;
			return base.Visit(node);
		}
	}
}
