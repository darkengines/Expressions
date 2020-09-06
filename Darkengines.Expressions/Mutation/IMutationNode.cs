using Darkengines.Expressions.Rules;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Darkengines.Expressions.Mutation {
	public interface IMutationNode {
		void CheckPermissions(object context);
		Task PrepareEntry(EntityEntry entry);
		Task<ActionResult> PrePersistAction(object context, object entity, EntityEntry entry);
		Task PostPersistAction(object context, object entity, EntityEntry entry);
		Expression BuildPermissionEntityExpression(object context, object instance);
	}
}
