﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

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
	}
}
