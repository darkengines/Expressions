using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;

namespace Darkengines.Expressions.Factories {
	public static class ServiceCollectionExtensions {
		public static IServiceCollection AddExpressionFactories(this IServiceCollection serviceCollection) {
			return serviceCollection.AddSingleton<IExpressionFactory, BinaryExpressionFactory>()
			.AddSingleton<IExpressionFactory, ConstantExpressionFactory>()
			.AddSingleton<IExpressionFactory, IdentifierExpressionFactory>()
			.AddSingleton<IExpressionFactory, UnaryExpressionFactory>()
			.AddSingleton<IExpressionFactory, MemberExpressionFactory>()
			.AddSingleton<IExpressionFactory, NewExpressionFactory>()
			.AddSingleton<IExpressionFactory, ArrayExpressionFactory>()
			.AddSingleton<IExpressionFactory, LambdaExpressionFactory>();
		}

		public static IServiceCollection AddLinqMethodCallExpressionFactories(this IServiceCollection serviceCollection) {
			var queryableLinqMethodInfos = typeof(Queryable).GetMethods().Where(methodInfo => methodInfo.IsDefined(typeof(ExtensionAttribute), true));
			var enumerableLinqMethodInfos = typeof(Enumerable).GetMethods().Where(methodInfo => methodInfo.IsDefined(typeof(ExtensionAttribute), true));
			foreach (var linqMethodInfo in queryableLinqMethodInfos) {
				serviceCollection.AddSingleton<IExpressionFactory>(serviceProvider => new MethodCallExpressionFactory(linqMethodInfo));
			}
			foreach (var linqMethodInfo in enumerableLinqMethodInfos) {
				serviceCollection.AddSingleton<IExpressionFactory>(serviceProvider => new MethodCallExpressionFactory(linqMethodInfo));
			}
			return serviceCollection;
		}
	}
}
