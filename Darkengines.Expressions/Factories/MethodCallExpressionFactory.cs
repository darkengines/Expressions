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
		protected bool IsExtension { get; }
		protected ParameterInfo[] Parameters { get; }
		protected Type[] GenericArguments { get; }

		public static MethodCallExpressionFactory CreateMethodCallExpressionFactory<TDeclaringType, TMethod>(Expression<Func<TDeclaringType, TMethod>> methodAccessExpression) {
			var methodInfo = ExpressionHelper.ExtractMethodInfo(methodAccessExpression);
			return new MethodCallExpressionFactory(methodInfo);
		}
		public MethodCallExpressionFactory(MethodInfo methodInfo) {
			MethodInfo = methodInfo;
			IsExtension = MethodInfo.IsDefined(typeof(ExtensionAttribute), true);
			Parameters = MethodInfo.GetParameters().ToArray();
			GenericArguments = MethodInfo.GetGenericArguments();
		}
		public override Expression BuildExpression(MethodCallExpressionModel expressionModel, ExpressionFactoryContext context, ExpressionFactoryScope scope) {
			if (expressionModel.Callee is MemberExpressionModel) {
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
					return Expression.Call(IsExtension ? null : objectExpression, MethodInfo, IsExtension ? new[] { objectExpression }.Concat(argumentsExpressions) : argumentsExpressions);
				} else {
					var genericMap = GenericArguments.ToDictionary(arg => arg, arg => (Type)null);
					var argumentModels = expressionModel.Arguments;
					var objectExpressionModel = ((MemberExpressionModel)expressionModel.Callee).Object;

					var objectExpression = (Expression)null;
					if (IsExtension) {
						argumentModels = new[] { objectExpressionModel }.Concat(argumentModels).ToArray();
					} else {
						var objectExpressionFactory = context.ExpressionFactories.FindExpressionFactoryFor(objectExpressionModel, context, scope);
						objectExpression = objectExpressionFactory.BuildExpression(objectExpressionModel, context, scope);
					}

					var zippedParameters = argumentModels.Zip(Parameters, (argumentModel, parameter) => new { ArgumentModel = argumentModel, Parameter = parameter });

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

		public override bool CanHandle(ExpressionModel expressionModel, ExpressionFactoryContext context, ExpressionFactoryScope scope) {
			var canHandle = base.CanHandle(expressionModel, context, scope);
			if (canHandle) {
				var methodCallExpressionModel = (MethodCallExpressionModel)expressionModel;
				string methodName = null;
				ExpressionModel objectExpressionModel = null;
				ExpressionModel[] parametersExpressionModels = null;
				var memberCallee = methodCallExpressionModel.Callee as MemberExpressionModel;
				if (memberCallee != null) {
					methodName = memberCallee.PropertyName;
					objectExpressionModel = memberCallee.Object;
				} else {
					var identifierCallee = expressionModel as IdentifierExpressionModel;
					if (identifierCallee != null) {
						methodName = identifierCallee.Name;
					}
				}
				canHandle &= methodName == MethodInfo.Name;
				if (canHandle) {
					var objectExpression = (Expression)null;

					if (MethodInfo.IsStatic && IsExtension) {
						parametersExpressionModels = new[] { objectExpressionModel }.Concat(methodCallExpressionModel.Arguments).ToArray();
					}

					var minimumParameterCount = Parameters.Where(parameter => !parameter.IsOptional).Count();
					var maximumParameterCount = Parameters.Length;
					canHandle &= minimumParameterCount <= parametersExpressionModels.Length && maximumParameterCount >= parametersExpressionModels.Length;

					if (canHandle) {
						//TODO: Check overloads without buildind the whole set of parameter expressions. This is hard.
						if (!MethodInfo.IsStatic) {
							var objectExpressionFactory = context.ExpressionFactories.FindExpressionFactoryFor(objectExpressionModel, context, scope);
							objectExpression = objectExpressionFactory.BuildExpression(objectExpressionModel, context, scope);
							canHandle &= MethodInfo.DeclaringType.IsAssignableFrom(objectExpression.Type);
						}
						if (canHandle) {
							var genericMap = GenericArguments.ToDictionary(arg => arg, arg => (Type)null);
							var zippedParameters = parametersExpressionModels.Zip(Parameters, (argumentModel, parameter) => new { ArgumentModel = argumentModel, Parameter = parameter }).ToArray();
							var index = 0;
							var argumentsExpressions = new List<Expression>();
							while(canHandle && index < zippedParameters.Length) {
								var tuple = zippedParameters[index];
								var argumentScope = new ExpressionFactoryScope(scope, tuple.Parameter.ParameterType) { GenericTypeResolutionMap = genericMap };
								var argumentFactory = context.ExpressionFactories.FindExpressionFactoryFor(tuple.ArgumentModel, context, argumentScope);
								canHandle &= argumentFactory != null;
								if (canHandle) {
									var argumentExpression = argumentFactory.BuildExpression(tuple.ArgumentModel, context, argumentScope);
									genericMap = InferGenericArguments(genericMap, tuple.Parameter.ParameterType, argumentExpression.Type);
									if (tuple.Parameter.ParameterType.IsGenericType) {
										var genericParameters = tuple.Parameter.ParameterType.GetGenericArguments();
										canHandle &= genericParameters.All(genericParameter => {
											return (!genericParameter.IsGenericType && !genericParameter.IsGenericParameter)
											|| (genericParameter.IsGenericParameter && genericMap[genericParameter] != null)
											|| (genericParameter.IsGenericType && genericParameter.GetGenericArguments().Where(arg => arg.IsGenericTypeParameter).All(arg => genericMap[arg] != null));
										});
									}
									argumentsExpressions.Add(argumentExpression);
								}
								index++;
							}

							if (canHandle) {
								var methoInfo = MethodInfo.MakeGenericMethod(genericMap.Values.ToArray());
								var parameters = methoInfo.GetParameters();
								canHandle &= parameters.Zip(argumentsExpressions, (parameter, argument) => new { Parameter = parameter, Argument = argument }).All(tuple => tuple.Parameter.ParameterType.IsAssignableFrom(tuple.Argument.Type));
							}
						}
					}
				}
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
					while(argument != null && (!argument.IsGenericType || !parameter.GetGenericTypeDefinition().MakeGenericType(argument.GetGenericArguments()).IsAssignableFrom(argument))) {
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
