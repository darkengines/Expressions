using Darkengines.Expressions.Security;
using Darkengines.Expressions.Tests.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Darkengines.Expressions.Tests.Security {
	public class UserSecurityRuleProvider : SecurityRuleProvider<User> {
		public UserSecurityRuleProvider(IIdentityProvider identityProvider, BloggingContext bloggingContext) : base(identityProvider, bloggingContext) {
		}

		public override IQueryable<User> GetAccessibleQueryable(Permission permission) {
			return BloggingContext.Users.Where(user => user == IdentityProvider.GetCurrentUser());
		}

		public override Permission GetOperationPermission() {
			return Permission.Read | Permission.Write;
		}
		public override IQueryable GetPermissions() {
			var currentUser = IdentityProvider.GetCurrentUser();
			return BloggingContext.Users.Select(user => new { Id = user.Id, Permission = user == currentUser ? Permission.Write | Permission.Read | Permission.Delete : Permission.Read });
		}
	}
}
