using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Darkengines.Expressions.Mutation {
	public abstract class MemberMutationInfo {
		public bool IsModified { get; set; }
		public bool IsTouched { get; set; }
		protected MutationContext MutationContext { get; }
		public abstract MemberBinding BuildMemberBindingExpression(Expression parameter, ISet<EntityMutationInfo> cache);
		public EntityMutationInfo EntityMutationInfo { get; }
		public MemberMutationInfo(MutationContext mutationContext, EntityMutationInfo entityMutationInfo) {
			MutationContext = mutationContext;
			EntityMutationInfo = entityMutationInfo;
		}
	}
}
