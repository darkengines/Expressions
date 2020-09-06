using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Darkengines.Expressions {
	public static class ExpressionHelper {
		public static MethodInfo ExtractMethodInfo<TDeclaringType, TMethod>(Expression<Func<TDeclaringType, TMethod>> methodAccessExpression) {
			return (MethodInfo)((ConstantExpression)((MethodCallExpression)((UnaryExpression)methodAccessExpression.Body).Operand).Object).Value;
		}
		public static MethodInfo ExtractGenericDefinitionMethodInfo<TDeclaringType, TMethod>(Expression<Func<TDeclaringType, TMethod>> methodAccessExpression) {
			return ((MethodInfo)((ConstantExpression)((MethodCallExpression)((UnaryExpression)methodAccessExpression.Body).Operand).Object).Value).GetGenericMethodDefinition();
		}
		public static PropertyInfo ExtractPropertyInfo<TDeclaringType>(Expression<Func<TDeclaringType, object>> propertyAccessExpression) {
			var unaryExpression = propertyAccessExpression.Body as UnaryExpression;
			if (unaryExpression != null) {
				return (PropertyInfo)((MemberExpression)unaryExpression.Operand).Member;
			}
			return (PropertyInfo)((MemberExpression)propertyAccessExpression.Body).Member;
		}
		public static TExpression Replace<TExpression>(this TExpression expression, Expression oldExpression, Expression newExpression) where TExpression : Expression {
			return expression.Replace(new Dictionary<Expression, Expression> { { oldExpression, newExpression } });
		}
		public static TExpression Replace<TExpression>(this TExpression expression, IDictionary<Expression, Expression> replacements) where TExpression : Expression {
			var replacementExpressionVisitor = new ReplacementExpressionVisitor(replacements);
			return (TExpression)replacementExpressionVisitor.Visit(expression);
		}
		public static Expression Join(this IEnumerable<Expression> expressions, Func<Expression, Expression, Expression> reducer) {
			return expressions.Skip(1).Any() ? expressions.Skip(1).Aggregate(expressions.First(), reducer) : expressions.First();
		}
		public static TSource Reduce<TSource>(this TSource source, Func<TSource, TSource> reducer, Func<TSource, bool> predicate) {
			while (predicate(source)) {
				source = reducer(source);
			}
			return source;
		}
	}
}
