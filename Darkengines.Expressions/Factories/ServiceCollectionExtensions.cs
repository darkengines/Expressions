using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
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
			.AddSingleton<IExpressionFactory>((serviceProvider) => MethodCallExpressionFactory.CreateMethodCallExpressionFactory<IEnumerable<int>, Func<Type>>(x => x.GetType));
		}
	}
}
