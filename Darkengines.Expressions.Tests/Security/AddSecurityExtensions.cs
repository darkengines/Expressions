using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Darkengines.Expressions.Tests.Security {
	public static class AddSecurityExtensions {
		public static IServiceCollection AddSecurity(this IServiceCollection serviceCollection) {
			serviceCollection.AddSingleton<IIdentityProvider, TestIdentityProvider>()
			.AddSingleton<ISecurityRuleProvider, UserSecurityRuleProvider>()
			.AddSingleton<ISecurityRuleProvider, BlogSecurityRuleProvider>()
			.AddSingleton<ISecurityRuleProvider, PostSecurityRuleProvider>()
			.AddSingleton<ISecurityRuleProvider, CommentSecurityRuleProvider>();

			return serviceCollection;
		}
		public static ModelBuilder AddSecurity(this ModelBuilder modelBuilder) {
			modelBuilder.Entity<EntityAccess>().HasKey(ea => ea.NodeId);
			return modelBuilder;
		}
	}
}
