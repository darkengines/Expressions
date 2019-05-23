using System;
using System.Linq.Expressions;
using Darkengines.Expressions.Security;
using Darkengines.Expressions.Tests.Entities;

namespace Darkengines.Expressions.Tests {
	public class BlogRule : Rule<Blog> {
		public override Expression<Func<Blog, Permission>> GetPermission(User user) {
			return b => b.OwnerId == user.Id ?
			Permission.Delete | Permission.Write | Permission.Read
			: Permission.Read;
		}
	}
}
