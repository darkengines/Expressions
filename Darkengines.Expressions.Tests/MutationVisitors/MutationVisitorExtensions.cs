using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Darkengines.Expressions.Tests.MutationVisitors {
	public static class MutationVisitorExtensions {
		public static IServiceCollection AddMutationVisitors(this IServiceCollection serviceCollection) {
			return serviceCollection.AddScoped<MutationVisitor>();
		}
	}
}
