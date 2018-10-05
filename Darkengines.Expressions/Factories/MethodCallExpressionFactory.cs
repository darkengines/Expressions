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
			var parameters = MethodInfo.GetParameters().ToArray();
			if (expressionModel.Callee is MemberExpressionModel) {
				var isExtension = MethodInfo.IsDefined(typeof(ExtensionAttribute), true);
				var methodName = ((MemberExpressionModel)expressionModel.Callee).PropertyName;

				if (!MethodInfo.IsGenericMethodDefinition) {
					var objectExpressionModel = ((MemberExpressionModel)expressionModel.Callee).Object;
					var objectExpressionFactory = context.ExpressionFactories.FindExpressionFactoryFor(objectExpressionModel, context, scope);
					var objectExpression = objectExpressionFactory.BuildExpression(objectExpressionModel, context, scope);
					var argumentsExpressions = expressionModel.Arguments.Select(argumentModel => {
						var argumentFactory = context.ExpressionFactories.FindExpressionFactoryFor(argumentModel, context, scope);
						var argumentExpression = argumentFactory.BuildExpression(argumentModel, context, scope);
						return argumentExpression;
					}).ToArray();
					return Expression.Call(isExtension ? null : objectExpression, MethodInfo, isExtension ? new[] { objectExpression }.Concat(argumentsExpressions) : argumentsExpressions);
				} else {
					var genericMap = MethodInfo.GetGenericArguments().ToDictionary(arg => arg, arg => (Type)null);
					var argumentModels = expressionModel.Arguments;
					var objectExpressionModel = ((MemberExpressionModel)expressionModel.Callee).Object;

					var objectExpression = (Expression)null;
					if (isExtension) {
						argumentModels = new[] { objectExpressionModel }.Concat(argumentModels).ToArray();
					} else {
						var objectExpressionFactory = context.ExpressionFactories.FindExpressionFactoryFor(objectExpressionModel, context, scope);
						objectExpression = objectExpressionFactory.BuildExpression(objectExpressionModel, context, scope);
					}

					var zippedParameters = argumentModels.Zip(parameters, (argumentModel, parameter) => new { ArgumentModel = argumentModel, Parameter = parameter });

					var argumentsExpressions = zippedParameters.Select(tuple => {
						var argumentScope = new ExpressionFactoryScope(scope, tuple.Parameter.ParameterType) { GenericTypeResolutionMap = genericMap };
						var argumentFactory = context.ExpressionFactories.FindExpressionFactoryFor(tuple.ArgumentModel, context, argumentScope);
						var argumentExpression = argumentFactory.BuildExpression(tuple.ArgumentModel, context, argumentScope);
						genericMap = InferGenericArguments(genericMap, tuple.Parameter.ParameterType, argumentExpression.Type);
						return argumentExpression;
					}).ToArray();

					var methoInfo = MethodInfo.MakeGenericMethod(genericMap.Values.ToArray());

					return Expression.Call(objectExpression, methoInfo, argumentsExpressions);
				}
			}
			throw new NotImplementedException();
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
					var arguments = argument.GetGenericArguments();
					map = InferGenericParameters(map, parameters, arguments);
				}
			}
			return map;
		}
	}
}
