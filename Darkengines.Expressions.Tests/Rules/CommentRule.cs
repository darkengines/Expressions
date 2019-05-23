using System;
using System.Linq.Expressions;
using Darkengines.Expressions.Security;
using Darkengines.Expressions.Tests.Entities;

namespace Darkengines.Expressions.Tests {
	public class CommentRule : Rule<Comment> {
		public override Expression<Func<Comment, Permission>> GetPermission(User user) {
			return c => c.Owner.Id == user.Id ?
			Permission.Delete | Permission.Write | Permission.Read
			: Permission.Read;
		}
	}
}
