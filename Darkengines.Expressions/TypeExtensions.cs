using Darkengines.Expressions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DarkEngines.Expressions {
	public static class TypeExtensions {
		public static Type GetEnumerableUnderlyingType(this Type type) {
			if (type.IsArray) {
				return type.GetElementType();
			} else if (type.IsInterface && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)) {
				return type.GetGenericArguments()[0];
			} else {
				return (type.IsInterface ? new[] { type } : type.GetInterfaces()).Where(@interface =>
					@interface.IsGenericType
					&& typeof(IEnumerable).IsAssignableFrom(@interface.GetGenericTypeDefinition())
				).FirstOrDefault()?.GetGenericArguments()[0];
			}
		}
		public static Type ResolveGenericType(this Type type, Dictionary<Type, Type> resolved) {
			if (type.IsGenericType && type.ContainsGenericParameters) {
				var genericArguments = type.GetGenericArguments();
				var genericDefinition = type.GetGenericTypeDefinition();
				var resolvedGenericArguments = genericArguments.Select(arg => arg.ResolveGenericType(resolved)).ToArray();
				return genericDefinition.MakeGenericType(resolvedGenericArguments);
			} else {
				return type.IsGenericParameter ? resolved[type] : type;
			}
		}
		public static bool GenericAssignableFrom(this Type to, Type from, Dictionary<Type, Type> genericArgumentMapping = null) {
			if (to.ContainsGenericParameters) {
				if (!from.ContainsGenericParameters) {
					if (to.IsGenericParameter) {
						var constraints = to.GetGenericParameterConstraints();
						var isAssignable = constraints.All(constraint => constraint.IsAssignableFrom(from));
						if (genericArgumentMapping != null && isAssignable) {
							genericArgumentMapping[to] = from;
						}
						return isAssignable;
					}
					var toGenericTypeDefinition = to.GetGenericTypeDefinition();
					var fromGenericTypeDefinition = from.IsGenericType ? from.GetGenericTypeDefinition() : from;
					if (toGenericTypeDefinition.BaseType == typeof(LambdaExpression)) toGenericTypeDefinition = toGenericTypeDefinition.BaseType;
					if (fromGenericTypeDefinition.BaseType == typeof(LambdaExpression)) toGenericTypeDefinition = fromGenericTypeDefinition.BaseType;
					if (toGenericTypeDefinition.IsAssignableFrom(fromGenericTypeDefinition) || fromGenericTypeDefinition.GetInterfaces().Any(@interface => @interface.IsGenericType && toGenericTypeDefinition.IsAssignableFrom(@interface.GetGenericTypeDefinition()))) {
						var fromGenericArguments = from.HasElementType ? new[] { from.GetElementType() } : from.GenericTypeArguments;
						var toGenericArguments = to.GenericTypeArguments;
						return fromGenericArguments.Zip(toGenericArguments, (fromGenericArgument, toGenericArgument) => (fromGenericArgument, toGenericArgument)).All(tuple => tuple.toGenericArgument.GenericAssignableFrom(tuple.fromGenericArgument, genericArgumentMapping));
					}
				}
			} else {
				return to.IsAssignableFrom(from) || from.GetInterfaces().Any(@interface => to.IsAssignableFrom(@interface));
			}
			return false;
		}
		public static MethodInfo FindMethodInfo(this IEnumerable<MethodInfo> methodInfos, string name, Type[] argumentTypes) {
			var methodInfoGenericArgumentsTuple = methodInfos.Where(mi => {
				var parameters = mi.GetParameters();
				var maxArgumentCount = parameters.Length;
				var minArgumentCount = parameters.Where(p => !p.IsOptional).Count();
				return mi.Name == name
				&& argumentTypes.Length >= minArgumentCount
				&& argumentTypes.Length <= maxArgumentCount;
			}).Select(mi => {
				var parameters = mi.GetParameters();
				var genericArgumentMapping = new Dictionary<Type, Type>();
				var assignable = parameters.Zip(argumentTypes, (parameter, argumentType) => parameter.ParameterType.GenericAssignableFrom(argumentType, genericArgumentMapping)).All(isAssignable => isAssignable);
				return (assignable, genericArgumentMapping, methodInfo: mi);
			}).FirstOrDefault(tuple => tuple.assignable);
			return methodInfoGenericArgumentsTuple.assignable ? methodInfoGenericArgumentsTuple.methodInfo.MakeGenericMethod(methodInfoGenericArgumentsTuple.genericArgumentMapping.Values.ToArray()) : null;
		}
		public static MethodInfo[] FindMethodInfos(this IEnumerable<MethodInfo> methodInfos, string name, int argumentCount) {
			return methodInfos.Where(mi => {
				var parameters = mi.GetParameters();
				var maxArgumentCount = parameters.Length;
				var minArgumentCount = parameters.Where(p => !p.IsOptional).Count();
				return mi.Name == name.ToPascalCase()
				&& argumentCount >= minArgumentCount
				&& argumentCount <= maxArgumentCount;
			}).ToArray();
		}
	}
}
