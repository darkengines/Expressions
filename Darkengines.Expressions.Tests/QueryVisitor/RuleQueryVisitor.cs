using Darkengines.Expressions.Tests.Entities;
using Darkengines.Expressions.Tests.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Darkengines.Expressions.Tests.QueryVisitor {
	public class RuleQueryVisitor : ExpressionVisitor {
		protected RuleProvider RuleProvider { get; }
		protected User User { get; }

		public RuleQueryVisitor(RuleProvider ruleProvider, User user) {
			RuleProvider = ruleProvider;
			User = user;
		}

		protected override Expression VisitMember(MemberExpression node) {
			var property = node.Member as PropertyInfo;
			if (property != null) {
				var rules = RuleProvider.GetRulesFor(property);
				var surrogates = new Dictionary<Expression, Expression>();
				var newParameter = node.Expression;
				var replacementExpressionVisitor = new ReplacementExpressionVisitor(surrogates);
				if (rules.Any()) {
					var ruleExpressions = rules.Select(rule => {
						var expression = rule(User);
						var parameter = expression.Parameters[0];
						var body = expression.Body;
						surrogates[parameter] = newParameter;
						body = replacementExpressionVisitor.Visit(body);
						return body;
					}).ToArray();
					var predicateExpression = ruleExpressions.Skip(1).Aggregate(ruleExpressions.First(), (predicate, current) => Expression.And(predicate, current));
					var memberExpression = Expression.Condition(Expression.GreaterThan(Expression.Convert(predicateExpression, typeof(int)), Expression.Constant(0)), node, Expression.Constant(null, node.Type));
					return memberExpression;
				}
			}
			return base.VisitMember(node);
		}
	}
}
