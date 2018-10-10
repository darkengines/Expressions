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
			var parameterGenericArguments = genericArguments.Take(genericArguments.Length - 1).ToArray();
			var genericReturnType = genericArguments.Last();

			var argumentGenericTypeMap = expressionModel.Parameters.Zip(parameterGenericArguments, (parameterModel, genericType) => new { ParameterModel = parameterModel, GenericType = genericType }).ToArray();
			var parameterExpressions = argumentGenericTypeMap.Select(tuple => Expression.Parameter(tuple.GenericType.ResolveGenericType(scope.GenericTypeResolutionMap), tuple.ParameterModel.Name)).ToArray();

			var lambdaScope = new ExpressionFactoryScope(scope, null) {
				Variables = parameterExpressions.ToDictionary(parameterExpression => parameterExpression.Name, parameterExpression => (Expression)parameterExpression)
			};

			var bodyFactory = context.ExpressionFactories.FindExpressionFactoryFor(expressionModel.Body, context, lambdaScope);
			var bodyExpression = bodyFactory.BuildExpression(expressionModel.Body, context, lambdaScope);
			var returnType = bodyExpression.Type;

			if (genericReturnType.IsGenericType) {
				scope.GenericTypeResolutionMap = InferGenericArguments(scope.GenericTypeResolutionMap, genericReturnType, returnType);
			}

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

		public override bool CanHandle(ExpressionModel expressionModel, ExpressionFactoryContext context, ExpressionFactoryScope scope) {
			var canHandle = base.CanHandle(expressionModel, context, scope);
			if (canHandle) {
				var lambdaExpressionModel = (LambdaExpressionModel)expressionModel;
				var targetType = scope.TargetType;
				if (targetType.IsGenericType && typeof(Expression<>) == targetType.GetGenericTypeDefinition()) {
					targetType = targetType.GetGenericArguments()[0];
				}

				var genericArguments = targetType.GetGenericArguments().ToArray();
				var parameterGenericArguments = genericArguments.Take(genericArguments.Length - 1).ToArray();
				var genericReturnType = genericArguments.Last();

				canHandle &= parameterGenericArguments.Length == lambdaExpressionModel.Parameters.Count();
			}
			return canHandle;
		}

		public Dictionary<Type, Type> InferGenericParameters(Dictionary<Type, Type> map, Type[] parameters, Type[] arguments) {
			var tuples = parameters.Zip(arguments, (parameter, argument) => new { Parameter = parameter, Argument = argument }).ToArray();
			foreach (var tuple in tuples) {
				var result = InferGenericArguments(map, tuple.Parameter, tuple.Argument);
			}
			return map;
		}

		public Dictionary<Type, Type> InferGenericArguments(Dictionary<Type, Type> map, Type parameter, Type argument) {
			if (map.ContainsKey(parameter)) {
				map[parameter] = argument;
			} else {
				if (parameter.IsGenericType) {
					var parameters = parameter.GetGenericArguments();
					Type matchingInterface = null;
					if ((matchingInterface = argument.GetInterfaces().FirstOrDefault(@interface => @interface.IsGenericType && parameter.GetGenericTypeDefinition() == @interface.GetGenericTypeDefinition())) != null) {
						argument = matchingInterface;
					}
					while (argument != null && (!argument.IsGenericType || !parameter.GetGenericTypeDefinition().MakeGenericType(argument.GetGenericArguments().Take(parameters.Length).ToArray()).IsAssignableFrom(argument))) {
						argument = argument.BaseType;
					}
					if (argument != null) {
						var arguments = argument.GetGenericArguments();
						map = InferGenericParameters(map, parameters, arguments);
					}
				}
			}
			return map;
		}
	}
}
