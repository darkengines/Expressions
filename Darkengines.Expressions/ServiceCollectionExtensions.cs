using Darkengines.Expressions.Mutation;
using Darkengines.Expressions.Rules;
using Darkengines.Expressions.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Darkengines.Expressions {
	public static class ServiceCollectionExtensions {
		public static IServiceCollection AddExpressions(this IServiceCollection serviceCollection) {
			return serviceCollection
			.AddScoped<PermissionEntityTypeBuilder>()
			.AddSingleton<PermissionEntityTypeBuilderCache>();
		}
		public static IServiceCollection AddQueryableRuleMap<TContext>(this IServiceCollection serviceCollection) {
			return serviceCollection.AddScoped<IRuleMap, QueryableGenericRuleMap<TContext>>()
			.AddScoped<IRuleMap, OrderedQueryableGenericRuleMap<TContext>>()
			.AddScoped<IRuleMap, EnumerableGenericRuleMap<TContext>>()
			.AddScoped<IRuleMap, OrderedEnumerableGenericRuleMap<TContext>>()
			.AddScoped<IRuleMap, CollectionGenericRuleMap<TContext>>()
			.AddScoped<IRuleMap, IncludableQueryGenericRuleMap<TContext>>();
		}
	}
}
