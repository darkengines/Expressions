using DarkEngines.Expressions;
using Esprima.Ast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Darkengines.Expressions.Converters {
	public class LambdaExpressionConverter : ExpressionConverter<ArrowFunctionExpression> {
		public override Esprima.Ast.Nodes NodeType => Esprima.Ast.Nodes.ArrowFunctionExpression;
		public override bool IsGenericType => true;
		public override Type GetGenericType(ArrowFunctionExpression arrowFunctionExpression, Type genericTypeDefinition) {
			var genericType = Type.GetType($"System.Func`{arrowFunctionExpression.Params.Count + 1}");
			if (typeof(System.Linq.Expressions.Expression).IsAssignableFrom(genericTypeDefinition)) {
				genericType = typeof(Expression<>).MakeGenericType(genericType);
			}
			return genericType;
		}
		public override TypeNode[] GetRequiredGenericArgumentIndices(ArrowFunctionExpression arrowFunctionExpression, Type genericTypeDefinition) {
			var nodes = new List<TypeNode>();
			var arguments = arrowFunctionExpression.Params.Cast<Identifier>();
			var argumentsCount = arguments.Count();
			var typeNodes = Enumerable.Range(0, argumentsCount).Select(argumentIndex => new TypeNode(argumentIndex, TypeNodeDirection.Input, null)).ToList();
			typeNodes.Add(new TypeNode(argumentsCount, TypeNodeDirection.Output, null));
			if (typeof(System.Linq.Expressions.Expression).IsAssignableFrom(genericTypeDefinition)) {
				var typeNode = new TypeNode(0, TypeNodeDirection.Input | TypeNodeDirection.Output, null);
				foreach (var child in typeNodes) {
					typeNode.Children.Add(child);
					child.Parent = typeNode;
				}
				typeNodes = new List<TypeNode> { typeNode };
			}
			return typeNodes.ToArray();
		}
		public override (System.Linq.Expressions.Expression, ExpressionConversionResult) Convert(ArrowFunctionExpression arrowFunctionExpression, ExpressionConverterContext context, ExpressionConverterScope scope, bool allowTerminal, params Type[] genericArguments) {

			var argumentGenericTypeMap = arrowFunctionExpression.Params.Zip(genericArguments, (param, genericType) => new { Param = param, GenericType = genericType }).ToArray();
			var parameterExpressions = argumentGenericTypeMap.Select(tuple => System.Linq.Expressions.Expression.Parameter(tuple.GenericType.ResolveGenericType(scope.GenericTypeResolutionMap), tuple.Param.As<Identifier>().Name)).ToArray();

			var lambdaScope = new ExpressionConverterScope(scope, null) {
				Variables = parameterExpressions.ToDictionary(parameterExpression => parameterExpression.Name, parameterExpression => (System.Linq.Expressions.Expression)parameterExpression)
			};

			var bodyExpression = context.ExpressionConverterResolver.Convert(arrowFunctionExpression.Body, context, lambdaScope, false);
			var returnType = bodyExpression.Type;

			if (scope.TargetType.IsGenericType && typeof(Expression<>) == scope.TargetType.GetGenericTypeDefinition()) {
				return (System.Linq.Expressions.Expression.Constant(System.Linq.Expressions.Expression.Lambda(bodyExpression, parameterExpressions)), DefaultResult);
			} else {
				scope.TargetType.GetGenericArguments().Last().GenericAssignableFrom(returnType, scope.GenericTypeResolutionMap);
				var returnTargetType = scope.TargetType.GetGenericArguments().Last().ResolveGenericType(scope.GenericTypeResolutionMap);
				//if (returnTargetType.IsAssignableFrom(bodyExpression.Type) && bodyExpression.Type != returnTargetType)
				//{
				//	bodyExpression = System.Linq.Expressions.Expression.Convert(bodyExpression, returnTargetType);
				//}
				var lambdaMethodInfo = ExpressionHelper.ExtractGenericDefinitionMethodInfo<System.Linq.Expressions.Expression, Func<System.Linq.Expressions.Expression, ParameterExpression[], Expression<Func<int>>>>(_ => System.Linq.Expressions.Expression.Lambda<Func<int>>);
				var genericType = Type.GetType($"System.Func`{arrowFunctionExpression.Params.Count + 1}");
				var funcType = genericType.MakeGenericType(parameterExpressions.Select(p => p.Type).Concat(new[] { returnTargetType }).ToArray());
				var expression = lambdaMethodInfo.MakeGenericMethod(funcType).Invoke(null, new object[] { bodyExpression, parameterExpressions });
				return ((System.Linq.Expressions.Expression)expression, DefaultResult);
				//lambdaMethodInfo.MakeGenericMethod()
				//return System.Linq.Expressions.Expression.Lambda(bodyExpression, parameterExpressions);
			}
		}

		public Dictionary<Type, Type> InferGenericParameters(Dictionary<Type, Type> map, Type[] parameters, Type[] arguments) {
			var tuples = parameters.Zip(arguments, (parameter, argument) => new { Parameter = parameter, Argument = argument }).ToArray();
			foreach (var tuple in tuples) {
				var result = InferGenericArguments(map, tuple.Parameter, tuple.Argument);
			}
			return map;
		}

		public override void ResolveGenericArguments(Dictionary<Type, Type> genericArgumentMapping) {
			base.ResolveGenericArguments(genericArgumentMapping);
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
