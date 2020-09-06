using DarkEngines.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Darkengines.Expressions.Mutation {
	public class CollectionMutationInfo : NavigationMutationInfo {
		public ISet<EntityMutationInfo> TargetEntityMutationInfos { get; }
		public CollectionMutationInfo(MutationContext mutationContext, INavigation navigation, EntityMutationInfo entityMutationInfo) : base(mutationContext, navigation, entityMutationInfo) {
			TargetEntityMutationInfos = new HashSet<EntityMutationInfo>();
		}

		public override MemberBinding BuildMemberBindingExpression(Expression parameter, ISet<EntityMutationInfo> cache) {
			var values = TargetEntityMutationInfos.Where(emi => !cache.Contains(emi)).Select(entityMutationInfo => {
				var result = entityMutationInfo.BuildPermissionEntityExpression(MutationContext.SecurityContext, cache, null);
				return result;
			}).Where(value => value != null).ToArray();
			if (values.Any()) {
				var permissionCollectionPropertyInfo = MutationContext.PermissionEntityTypeBuilder.PropertyMap[Navigation];
				var bindingExpression = Expression.Bind(
					permissionCollectionPropertyInfo,
					Expression.NewArrayInit(
						permissionCollectionPropertyInfo.PropertyType.GetEnumerableUnderlyingType(),
						values
					)
				);
				return bindingExpression;
			}
			return null;
		}
	}
}
