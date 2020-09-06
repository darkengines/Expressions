using Darkengines.Expressions.Rules;
using Darkengines.Expressions.Security;
using DarkEngines.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Darkengines.Expressions.Converters {
	public class MethodCallExpressionConverter : ExpressionConverter<Esprima.Ast.CallExpression> {
		public override Esprima.Ast.Nodes NodeType => Esprima.Ast.Nodes.CallExpression;
		public override (Expression Expression, ExpressionConversionResult ExpressionConversionResult) Convert(Esprima.Ast.CallExpression methodCallExpression, ExpressionConverterContext context, ExpressionConverterScope scope, bool allowTerminal, params Type[] genericArguments) {
			var memberExpression = methodCallExpression.Callee as Esprima.Ast.MemberExpression;
			var arguments = methodCallExpression.Arguments.Cast<Esprima.Ast.INode>().ToArray();
			var methodIdentifier = (Esprima.Ast.Identifier)memberExpression.Property;
			MethodInfo methodInfo = null;
			Expression instanceExpression = null;
			Expression instanceExpressionDump = null;
			Expression[] argumentsExpressions = null;
			ExpressionConversionResult expressionConversionResult = null;
			if (memberExpression == null) {
				//method is static
				(methodInfo, argumentsExpressions, expressionConversionResult) = FindMethodInfo(context.StaticMethods.FindMethodInfos(methodInfo.Name, arguments.Count()).ToArray(), arguments, context, scope);
			} else {
				//method is not static
				instanceExpression = instanceExpressionDump = context.ExpressionConverterResolver.Convert(memberExpression.Object, context, scope, true);
				var instanceType = instanceExpression.Type;
				var methodInfosCandidates = instanceType.GetMethods().FindMethodInfos(methodIdentifier.Name, arguments.Length);
				(methodInfo, argumentsExpressions, expressionConversionResult) = FindMethodInfo(methodInfosCandidates, arguments, context, scope, instanceExpression);

				if (methodInfo == null) {
					//try extension methods
					methodInfosCandidates = context.ExtensionMethods.FindMethodInfos(methodIdentifier.Name, arguments.Length + 1);
					(methodInfo, argumentsExpressions, expressionConversionResult) = FindMethodInfo(methodInfosCandidates, arguments, context, scope, instanceExpression, true);
					if (methodInfo != null) {
						argumentsExpressions = new Expression[] { instanceExpression }.Concat(argumentsExpressions).ToArray();
						instanceExpression = null;
					}
				}
			}
			if (methodInfo == null) {
				//Fallback on custom converters
				var customConverter = context.CustomMethodCallExpressionConverters.FirstOrDefault(converter => converter.CanHandle(methodCallExpression, context, scope, allowTerminal, genericArguments));
				if (customConverter != null) return customConverter.Convert(methodCallExpression, context, scope, allowTerminal, genericArguments);
				throw new NotImplementedException($"Method {methodIdentifier.Name} on type {instanceExpressionDump.Type} not found.");
			}
			var callExpressiom = Expression.Call(instanceExpression, methodInfo, argumentsExpressions);
			return (callExpressiom, expressionConversionResult);
		}

		protected IEnumerable<int> GetGenericArgumentPath(TypeNode typeNode) {
			if (typeNode.Parent != null) {
				foreach (var index in GetGenericArgumentPath(typeNode.Parent)) yield return index;
			}
			yield return typeNode.Index;
		}
		protected (MethodInfo MethodInfo, Expression[] Arguments, ExpressionConversionResult ExpressionConversionResult) FindMethodInfo(MethodInfo[] methodInfosCandidates, Esprima.Ast.INode[] arguments, ExpressionConverterContext context, ExpressionConverterScope scope, Expression instanceExpression = null, bool isExtension = false) {
			MethodInfo methodInfo = null;
			Expression[] argumentsExpressions = null;
			ExpressionConversionResult expressionConversionResult = new ExpressionConversionResult();
			Permission? permission = Permission.None;
			var methodInfoCandidateIndex = 0;
			while (methodInfo == null && methodInfoCandidateIndex < methodInfosCandidates.Length) {
				var methodInfoCandidate = methodInfosCandidates[methodInfoCandidateIndex];
				var methodParameters = methodInfoCandidate.GetParameters();
				var argumentParameterMap = methodParameters.Skip(isExtension ? 1 : 0).Zip(arguments, (parameterInfo, argument) => (parameterInfo, argument: (Esprima.Ast.INode)argument)).ToArray();
				var parameterArgumentTupleIndex = 0;
				var isValid = true;
				var methodInfoCandidateArgumentsExpressions = new List<Expression>();
				if (isExtension) {
					var instanceParameter = methodParameters.First();
					isValid = instanceParameter.ParameterType.IsGenericType ? instanceParameter.ParameterType.GenericAssignableFrom(instanceExpression.Type, scope.GenericTypeResolutionMap) : instanceParameter.ParameterType.IsAssignableFrom(instanceExpression.Type);
				}
				while (isValid && parameterArgumentTupleIndex < argumentParameterMap.Length) {
					var tuple = argumentParameterMap[parameterArgumentTupleIndex];
					var converter = context.ExpressionConverterResolver.FindExpressionConverter(tuple.argument);
					if (converter.IsGenericType) {
						var argumentGenericType = converter.GetGenericType(tuple.argument, tuple.parameterInfo.ParameterType);
						if ((isValid = AreGenericDefinitionEqual(tuple.parameterInfo.ParameterType, argumentGenericType))) {
							var nodes = converter.GetRequiredGenericArgumentIndices(tuple.argument, tuple.parameterInfo.ParameterType);
							var parameterType = tuple.parameterInfo.ParameterType;
							var inputGenericArguments = new List<(IEnumerable<int> path, Type Type)>();
							var outputGenericArguments = new List<(IEnumerable<int> path, Type Type)>();
							foreach (var node in nodes) {
								var stack = new Stack<(TypeNode, Type)>();
								stack.Push((node, parameterType));
								while (stack.Any()) {
									var currentNode = stack.Pop();
									var nodeGenericArguments = currentNode.Item2.GetGenericArguments();
									if (currentNode.Item1.Children.Any()) {
										foreach (var child in currentNode.Item1.Children) {
											stack.Push((child, nodeGenericArguments[currentNode.Item1.Index]));
										}
									} else {
										var resolvedType = nodeGenericArguments[currentNode.Item1.Index];
										if (currentNode.Item1.Direction == TypeNodeDirection.Input) {
											if (resolvedType.IsGenericParameter) {
												resolvedType = scope.GenericTypeResolutionMap[resolvedType];
											}
											inputGenericArguments.Add((GetGenericArgumentPath(currentNode.Item1), resolvedType));
										} else {
											outputGenericArguments.Add((GetGenericArgumentPath(currentNode.Item1), resolvedType));
										}
									}
								}
							}
							var argumentScope = new ExpressionConverterScope(scope, parameterType);
							var argumentExpression = converter.Convert(tuple.argument, context, argumentScope, false, inputGenericArguments.OrderBy(iga => iga.path.Last()).Select(t => t.Type).ToArray());
							scope.GenericTypeResolutionMap[outputGenericArguments[0].Type] = outputGenericArguments[0].path.Aggregate(argumentExpression.Type, (type, index) => type.GenericTypeArguments[index]);
							methodInfoCandidateArgumentsExpressions.Add(argumentExpression);
							isValid = tuple.parameterInfo.ParameterType.GenericAssignableFrom(argumentExpression.Type, scope.GenericTypeResolutionMap);
						}
					} else {
						var parameterScope = new ExpressionConverterScope(scope, null);
						if (!tuple.parameterInfo.ParameterType.IsGenericType) {
							parameterScope.TargetType = tuple.parameterInfo.ParameterType;
						}
						var argumentExpression = converter.Convert(tuple.argument, context, parameterScope);
						if (!tuple.parameterInfo.ParameterType.ContainsGenericParameters
							&& argumentExpression.Type != tuple.parameterInfo.ParameterType && tuple.parameterInfo.ParameterType.IsAssignableFrom(argumentExpression.Type)) argumentExpression = Expression.Convert(argumentExpression, tuple.parameterInfo.ParameterType);
						methodInfoCandidateArgumentsExpressions.Add(argumentExpression);
						var argumentType = argumentExpression.Type;
						isValid = tuple.parameterInfo.ParameterType.GenericAssignableFrom(argumentExpression.Type, scope.GenericTypeResolutionMap);
					}

					parameterArgumentTupleIndex++;
				}
				if (isValid) {
					var genericArguments = methodInfoCandidate.IsGenericMethod ? methodInfoCandidate.GetGenericArguments().Select(ga => scope.GenericTypeResolutionMap[ga]).ToArray() : new Type[0];
					if (instanceExpression != null) {
						var ruleMap = context.RuleMapRegistry.GetRuleMap(instanceExpression.Type, context.securityContext);
						if (ruleMap != null) {
							permission = ruleMap.ResolveMethodPermission(methodInfoCandidate, context.securityContext, genericArguments);
							expressionConversionResult.ShouldApplyProjection = ruleMap.ShouldProjectForMethod(methodInfoCandidate, context.securityContext, genericArguments);
							expressionConversionResult.ShouldApplyFilter = ruleMap.ShouldFilterForMethod(methodInfoCandidate, context.securityContext, genericArguments);
						}
					}

					methodInfo = methodInfoCandidate.IsGenericMethod ? methodInfoCandidate.MakeGenericMethod(genericArguments) : methodInfoCandidate;
					argumentsExpressions = methodInfoCandidateArgumentsExpressions.ToArray();

					if (!context.IsAdmin && (permission == null || !permission.Value.HasFlag(Permission.Read))) {
						throw new UnauthorizedAccessException($"You do not have access to method {methodInfo.Name} on type {instanceExpression.Type}.");
					}
				}
				methodInfoCandidateIndex++;
			}
			return (methodInfo, argumentsExpressions, expressionConversionResult);
		}

		protected IEnumerable<GenericArgumentPath> GetGenericArgumentsInfos(GenericArgumentPath genericArgumentPath) {
			if (genericArgumentPath.Type.IsGenericParameter && genericArgumentPath.Type.IsGenericMethodParameter) {
				yield return genericArgumentPath;
			} else {
				if (genericArgumentPath.Type.ContainsGenericParameters) {
					int i = 0;
					foreach (var genericArgument in genericArgumentPath.Type.GetGenericArguments()) {
						var path = new List<int>(genericArgumentPath.Path);
						path.Add(i);
						var child = new GenericArgumentPath() {
							Type = genericArgument,
							Path = path
						};
						var childInfos = GetGenericArgumentsInfos(child).ToArray();
						foreach (var childInfo in childInfos) yield return childInfo;
						i++;
					}
				}
			}
		}
		public class GenericArgumentPath {
			public Type Type { get; set; }
			public List<int> Path { get; set; }
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

		public bool AreGenericDefinitionEqual(Type left, Type right) {
			if (left.IsGenericParameter || right.IsGenericParameter) return true;
			if (left.GetGenericTypeDefinition() == right.GetGenericTypeDefinition()) {
				var leftGenericArguments = left.GetGenericArguments();
				var rightGenericArguments = right.GetGenericArguments();
				return leftGenericArguments.Zip(rightGenericArguments, (leftArg, rightArg) => AreGenericDefinitionEqual(leftArg, rightArg)).All(b => b);
			}
			return false;
		}
	}
}
