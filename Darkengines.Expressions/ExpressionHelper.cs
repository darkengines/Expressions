using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Darkengines.Expressions {
	public static class ExpressionHelper {
		public static MethodInfo ExtractMethodInfo<TDeclaringType, TMethod>(Expression<Func<TDeclaringType, TMethod>> methodAccessExpression) {
			return (MethodInfo)((ConstantExpression)((MethodCallExpression)((UnaryExpression)methodAccessExpression.Body).Operand).Object).Value;
		}
	}
}
