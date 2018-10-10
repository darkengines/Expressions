using Darkengines.Expressions.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Darkengines.Expressions.Web {
	public static class ExpressionFactoriesExtensions {
		public static IServiceCollection AddEntityFrameworkLinqExtensions(this IServiceCollection serviceCollection) {
			var methodInfos = typeof(EntityFrameworkQueryableExtensions).GetMethods().Where(methodInfo => methodInfo.IsDefined(typeof(ExtensionAttribute), true));
			var dbSetMethodInfos = typeof(DbSet<>).GetMethods();
			foreach (var linqMethodInfo in methodInfos) {
				serviceCollection.AddSingleton<IExpressionFactory>(serviceProvider => new MethodCallExpressionFactory(linqMethodInfo));
			}
			foreach (var dbSetMethodInfo in dbSetMethodInfos) {
				serviceCollection.AddSingleton<IExpressionFactory>(serviceProvider => new MethodCallExpressionFactory(dbSetMethodInfo));
			}
			return serviceCollection;
		}
	}
}
