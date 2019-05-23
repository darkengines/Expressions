using Darkengines.Expressions.Security;
using Darkengines.Expressions.Tests.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Darkengines.Expressions.Tests.Security {
	public class BlogSecurityRuleProvider : SecurityRuleProvider<Blog> {
		public BlogSecurityRuleProvider(IIdentityProvider identityProvider, BloggingContext bloggingContext) : base(identityProvider, bloggingContext) {
		}

		public override IQueryable<Blog> GetAccessibleQueryable(Permission permission) {
			return BloggingContext.Blogs.Where(blog => blog.Owner == IdentityProvider.GetCurrentUser());
		}

		public override Permission GetOperationPermission() {
			return Permission.Read | Permission.Write;
		}

		public override IQueryable GetPermissions() {
			var currentUser = IdentityProvider.GetCurrentUser();
			return BloggingContext.Blogs.Select(blog => new { Id = blog.Id, Permission = blog.Owner == currentUser ? Permission.Write | Permission.Read | Permission.Delete : Permission.Read });
		}
	}
}
