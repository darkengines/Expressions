using Darkengines.Expressions.Models;
using DarkEngines.Expressions;
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
		protected bool[] HasParamsAttribute { get; }
		protected Type[] GenericArguments { get; }

		public static MethodCallExpressionFactory CreateMethodCallExpressionFactory<TDeclaringType, TMethod>(Expression<Func<TDeclaringType, TMethod>> methodAccessExpression) {
			var methodInfo = ExpressionHelper.ExtractMethodInfo(methodAccessExpression);
			return new MethodCallExpressionFactory(methodInfo);
		}
		public MethodCallExpressionFactory(MethodInfo methodInfo) {
			MethodInfo = methodInfo;
			IsExtension = MethodInfo.IsDefined(typeof(ExtensionAttribute), true);
			Parameters = MethodInfo.GetParameters().ToArray();
			HasParamsAttribute = Parameters.Select(parameter => parameter.IsDefined(typeof(ParamArrayAttribute), true)).ToArray();
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
					var methodInfo = MethodInfo;
					var zipped = methodInfo.GetParameters().Zip(argumentsExpressions, (parameter, argument) => new {
						Parameter = parameter,
						Argument = argument
					}).ToArray();
					var arguments = new List<Expression>();
					var index = 0;
					while (index < zipped.Length) {
						var tuple = zipped[index];
						var isParams = HasParamsAttribute[index];
						if (isParams && !tuple.Argument.Type.IsArray) {
							var arrayType = tuple.Parameter.ParameterType.GetEnumerableUnderlyingType();
							var arrayExpression = Expression.NewArrayInit(
								arrayType,
								argumentsExpressions.Skip(index).Select(e => Expression.Convert(e, arrayType))
							);
							arguments.Add(arrayExpression);
						} else {
							arguments.Add(tuple.Argument);
						}
						index++;
					}
					if (!MethodInfo.IsStatic) {
						methodInfo = objectExpression.Type.GetMethod(MethodInfo.Name, GenericArguments.Length, arguments.Select(e => e.Type).ToArray());
					}
					return Expression.Call(IsExtension ? null : objectExpression, methodInfo, IsExtension ? new[] { objectExpression }.Concat(arguments) : arguments);
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
				ExpressionModel[] parametersExpressionModels = methodCallExpressionModel.Arguments.ToArray();
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
						parametersExpressionModels = new[] { objectExpressionModel }.Concat(parametersExpressionModels).ToArray();
					}

					var minimumParameterCount = Parameters.Where(parameter => !parameter.IsOptional).Count();
					var maximumParameterCount = Parameters.Length;
					canHandle &= minimumParameterCount <= parametersExpressionModels.Length && maximumParameterCount >= parametersExpressionModels.Length;

					if (canHandle) {
						Type objectType = null;
						//TODO: Check overloads without buildind the whole set of parameter expressions. This is hard.
						var genericMap = GenericArguments.ToDictionary(arg => arg, arg => (Type)null);
						if (!MethodInfo.IsStatic) {
							var objectExpressionFactory = context.ExpressionFactories.FindExpressionFactoryFor(objectExpressionModel, context, scope);
							objectExpression = objectExpressionFactory.BuildExpression(objectExpressionModel, context, scope);
							if (MethodInfo.DeclaringType.IsGenericType) {
								var objectGenericArguments = MethodInfo.DeclaringType.GetGenericArguments().ToArray();
								foreach (var objectGenericArgument in objectGenericArguments) genericMap[objectGenericArgument] = null;
								genericMap = InferGenericArguments(genericMap, MethodInfo.DeclaringType, objectExpression.Type);
								objectType = MethodInfo.DeclaringType.ResolveGenericType(genericMap);
								canHandle &= objectType.IsAssignableFrom(objectExpression.Type);
							} else {
								objectType = MethodInfo.DeclaringType;
								canHandle &= MethodInfo.DeclaringType.IsAssignableFrom(objectExpression.Type);
							}
						}
						if (canHandle) {
							var zippedParameters = parametersExpressionModels.Zip(Parameters, (argumentModel, parameter) => new { ArgumentModel = argumentModel, Parameter = parameter }).ToArray();
							var index = 0;
							var argumentsExpressions = new List<Expression>();
							while(canHandle && index < zippedParameters.Length) {
								var tuple = zippedParameters[index];
								var isParams = HasParamsAttribute[index];
								if (isParams) {
									if (!(tuple.ArgumentModel is ArrayExpressionModel)) {
										tuple = new {
											ArgumentModel = (ExpressionModel)new ArrayExpressionModel() {
												Items = parametersExpressionModels.Skip(index - 1)
											},
											tuple.Parameter
										};
									}
								}
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
								MethodInfo methodInfo = null;
								if (!MethodInfo.IsStatic) {
									methodInfo = objectType.GetMethod(MethodInfo.Name, GenericArguments.Length, argumentsExpressions.Select(e => e.Type).ToArray());
								} else {
									methodInfo = MethodInfo.MakeGenericMethod(genericMap.Values.ToArray());
								}
								var parameters = methodInfo.GetParameters();
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
					Type matchingInterface = null;
					if ((matchingInterface = argument.GetInterfaces().FirstOrDefault(@interface => @interface.IsGenericType && parameter.GetGenericTypeDefinition() == @interface.GetGenericTypeDefinition())) != null) {
						argument = matchingInterface;
					}
					while(argument != null && (!argument.IsGenericType || !parameter.GetGenericTypeDefinition().MakeGenericType(argument.GetGenericArguments().Take(parameters.Length).ToArray()).IsAssignableFrom(argument))) {
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
