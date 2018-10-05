using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
			.AddSingleton<IExpressionFactory, LambdaExpressionFactory>()
			.AddSingleton<IExpressionFactory>((serviceProvider) => new MethodCallExpressionFactory(ExpressionHelper.ExtractMethodInfo<IQueryable<object>, Func<IEnumerable<object>, Expression<Func<object, object>>, Expression<Func<object, object>>, Expression<Func<object, IEnumerable<object>, object>>, IQueryable<object>>>(x => x.GroupJoin).GetGenericMethodDefinition()));
		}
	}
}
