using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Darkengines.Expressions.Mutation {
	public abstract class NavigationMutationInfo : MemberMutationInfo {
		public INavigation Navigation { get; }
		public PropertyInfo PermissionNavigationPropertyInfo { get; }
		public EntityMutationInfo EntityMutationInfo { get; }
		public NavigationMutationInfo(MutationContext mutationContext, INavigation navigation, EntityMutationInfo entityMutationInfo) : base(mutationContext, entityMutationInfo) {
			Navigation = navigation;
			PermissionNavigationPropertyInfo = mutationContext.PermissionEntityTypeBuilder.PropertyMap[navigation];
			EntityMutationInfo = entityMutationInfo;
		}
	}
}
