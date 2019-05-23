using System;
using System.Linq.Expressions;
using Darkengines.Expressions.Security;
using Darkengines.Expressions.Tests.Entities;

namespace Darkengines.Expressions.Tests {
	public class PostRule : Rule<Post> {
		public override Expression<Func<Post, Permission>> GetPermission(User user) {
			return p => p.Owner.Id == user.Id ?
			Permission.Delete | Permission.Write | Permission.Read
			: Permission.Read;
		}
	}
}
