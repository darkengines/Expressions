using Darkengines.Expressions.Factories;
using Darkengines.Expressions.ModelConverters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;

namespace Darkengines.Expressions {
	public static class ServiceCollectionExtensions {
		public static IServiceCollection AddExpressions(this IServiceCollection serviceCollection) {
			return serviceCollection
			.AddExpressionFactories()
			.AddModelConverters()
			.AddSingleton<JavascriptExpressionConverter>();
		}
	}
}
