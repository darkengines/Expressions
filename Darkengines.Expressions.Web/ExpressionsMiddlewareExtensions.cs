using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Darkengines.Expressions.Web {
	public static class ExpressionsMiddlewareExtensions {
		public static IApplicationBuilder UseExpressions(this IApplicationBuilder builder) {
			return builder.UseMiddleware<ExpressionsMiddleware>();
		}
	}
}
