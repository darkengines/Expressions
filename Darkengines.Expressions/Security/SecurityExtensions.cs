using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Darkengines.Expressions.Security {
	public static class SecurityExtensions {
		public static IServiceCollection AddPermissionTypeBuilder(this IServiceCollection serviceCollection) {
			return serviceCollection.AddSingleton<PermissionEntityTypeBuilder>();
		}
	}
}
