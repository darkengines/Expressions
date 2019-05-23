using Darkengines.Expressions.Tests.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Darkengines.Expressions.Tests.Rules {
	public static class RuleExtensions {
		public static IServiceCollection AddRules(this IServiceCollection serviceCollection) {
			return serviceCollection.AddScoped<RuleProvider>()
			.AddScoped<IRule, BlogRule>()
			.AddScoped<IRule, PostRule>()
			.AddScoped<IRule, CommentRule>()
			.AddSingleton<IRuleMap, UserRuleMap>()
			.AddSingleton<IRuleMap, BlogRuleMap>()
			.AddSingleton<IRuleMap, PostRuleMap>()
			.AddSingleton<IRuleMap, CommentRuleMap>();
		}
	}
}
