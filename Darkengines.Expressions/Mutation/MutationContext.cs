using Darkengines.Expressions.Rules;
using Darkengines.Expressions.Security;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Darkengines.Expressions.Mutation {
	public class MutationContext {
		public PermissionEntityTypeBuilder PermissionEntityTypeBuilder { get; }
		public IEnumerable<IRuleMap> RuleMaps { get; }
		public object SecurityContext { get; }
		public IQueryProviderProvider QueryProviderProvider { get; }
		public JsonSerializer JsonSerializer { get; }
		public MutationContext(PermissionEntityTypeBuilder permissionEntityTypeBuilder, IEnumerable<IRuleMap> ruleMaps, object securityContext, IQueryProviderProvider queryProviderProvider, JsonSerializer jsonSerializer) {
			PermissionEntityTypeBuilder = permissionEntityTypeBuilder;
			RuleMaps = ruleMaps;
			SecurityContext = securityContext;
			QueryProviderProvider = queryProviderProvider;
			JsonSerializer = jsonSerializer;
		}
	}
}
