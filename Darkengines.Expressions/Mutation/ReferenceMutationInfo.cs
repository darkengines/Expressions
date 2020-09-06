using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Darkengines.Expressions.Mutation {
	public class ReferenceMutationInfo : NavigationMutationInfo {
		public EntityMutationInfo TargetEntityMutationInfo { get; set; }
		public ReferenceMutationInfo(MutationContext mutationContext, INavigation navigation, EntityMutationInfo entityMutationInfo) : base(mutationContext, navigation, entityMutationInfo) {
		}

		public override MemberBinding BuildMemberBindingExpression(Expression parameter, ISet<EntityMutationInfo> cache) {
			var propertyInfo = Navigation.PropertyInfo;
			var memberExpression = parameter == null ? null : Expression.MakeMemberAccess(parameter, propertyInfo);
			if (cache.Contains(TargetEntityMutationInfo)) return null;
			var value = TargetEntityMutationInfo.BuildPermissionEntityExpression(MutationContext.SecurityContext, cache, memberExpression);
			return value == null ? null : Expression.Bind(EntityMutationInfo.PermissionType.GetProperty(PermissionNavigationPropertyInfo.Name), value);
		}
	}
}
