using Darkengines.Expressions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Darkengines.Expressions.Factories {
	public class MethodCallExpressionFactory : ExpressionFactory<MethodCallExpressionModel> {
		public MethodInfo MethodInfo { get; }
		public static MethodCallExpressionFactory CreateMethodCallExpressionFactory<TDeclaringType, TMethod>(Expression<Func<TDeclaringType, TMethod>> methodAccessExpression) {
			var methodInfo = ExpressionHelper.ExtractMethodInfo(methodAccessExpression);
			return new MethodCallExpressionFactory(methodInfo);
		}
		public MethodCallExpressionFactory(MethodInfo methodInfo) {
			MethodInfo = methodInfo;
		}
		public override Expression BuildExpression(MethodCallExpressionModel expressionModel, ExpressionFactoryContext context, ExpressionFactoryScope scope) {
			if (expressionModel.Callee is MemberExpressionModel) {
				var isExtension = MethodInfo.IsDefined(typeof(ExtensionAttribute), true);
				var methodName = ((MemberExpressionModel)expressionModel.Callee).PropertyName;
				var objectExpressionModel = ((MemberExpressionModel)expressionModel.Callee).Object;
				var objectExpressimFactory = context.ExpressionFactories.FindExpressionFactoryFor(objectExpressionModel, context, scope);
				var objectExpression = objectExpressimFactory.BuildExpression(objectExpressionModel, context, scope);
				var argumentsExpressions = expressionModel.Arguments.Select(argumentModel => {
					var argumentFactory = context.ExpressionFactories.FindExpressionFactoryFor(argumentModel, context, scope);
					var argumentExpression = argumentFactory.BuildExpression(argumentModel, context, scope);
					return argumentExpression;
				}).ToArray();
				return Expression.Call(objectExpression, MethodInfo, argumentsExpressions);
			}
			throw new NotImplementedException();
		}
	}
}
