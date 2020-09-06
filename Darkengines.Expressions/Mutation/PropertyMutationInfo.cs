using Darkengines.Expressions.Security;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Darkengines.Expressions.Mutation {
	public class PropertyMutationInfo: MemberMutationInfo {
		public IProperty Property { get; }
		public PropertyMutationInfo(MutationContext mutationContext, IProperty property, EntityMutationInfo entityMutationInfo) : base(mutationContext, entityMutationInfo) {
			Property = property;
		}
		public override MemberBinding BuildMemberBindingExpression(Expression parameter, ISet<EntityMutationInfo> cache) {
			var expression = EntityMutationInfo.RuleMap.ResolveInstancePropertyCustomResolverExpression(Property.PropertyInfo, Permission.Write, MutationContext.SecurityContext, parameter);
			return expression == null ? null : Expression.Bind(EntityMutationInfo.PermissionType.GetProperty(Property.Name), Expression.Convert(expression, typeof(bool?)));
		}
	}
}
