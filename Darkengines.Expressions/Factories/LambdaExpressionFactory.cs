using Darkengines.Expressions.Models;
using DarkEngines.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Darkengines.Expressions.Factories {
	public class LambdaExpressionFactory : ExpressionFactory<LambdaExpressionModel> {
		public override Expression BuildExpression(LambdaExpressionModel expressionModel, ExpressionFactoryContext context, ExpressionFactoryScope scope) {

			var targetType = scope.TargetType;
			if (targetType.IsGenericType && typeof(Expression<>) == targetType.GetGenericTypeDefinition()) {
				targetType = targetType.GetGenericArguments()[0];
			}

			var genericArguments = targetType.GetGenericArguments().ToArray();
			var parameterGenericArguments = genericArguments.Take(genericArguments.Length - 1);
			var genericReturnType = genericArguments.Last();

			var argumentGenericTypeMap = expressionModel.Parameters.Zip(parameterGenericArguments, (parameterModel, genericType) => new { ParameterModel = parameterModel, GenericType = genericType }).ToArray();
			var parameterExpressions = argumentGenericTypeMap.Select(tuple => Expression.Parameter(tuple.GenericType.ResolveGenericType(scope.GenericTypeResolutionMap), tuple.ParameterModel.Name)).ToArray();

			var lambdaScope = new ExpressionFactoryScope(scope, null) {
				Variables = parameterExpressions.ToDictionary(parameterExpression => parameterExpression.Name, parameterExpression => (Expression)parameterExpression)
			};

			var bodyFactory = context.ExpressionFactories.FindExpressionFactoryFor(expressionModel.Body, context, lambdaScope);
			var bodyExpression = bodyFactory.BuildExpression(expressionModel.Body, context, lambdaScope);
			var returnType = bodyExpression.Type;

			var resolvedReturnType = genericReturnType.ResolveGenericType(scope.GenericTypeResolutionMap);
			if (resolvedReturnType != null && resolvedReturnType != returnType) {
				bodyExpression = Expression.Convert(bodyExpression, resolvedReturnType);
			}

			if (scope.TargetType.IsGenericType && typeof(Expression<>) == scope.TargetType.GetGenericTypeDefinition()) {
				return Expression.Constant(Expression.Lambda(bodyExpression, parameterExpressions));
			} else {
				return Expression.Lambda(bodyExpression, parameterExpressions);
			}
		}
	}
}
