using Darkengines.Expressions.Security;
using Darkengines.Expressions.Tests.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Darkengines.Expressions.Tests.Security {
	public class PostSecurityRuleProvider : SecurityRuleProvider<Post> {
		public PostSecurityRuleProvider(IIdentityProvider identityProvider, BloggingContext bloggingContext) : base(identityProvider, bloggingContext) {
		}

		public override IQueryable<Post> GetAccessibleQueryable(Permission permission) {
			return BloggingContext.Posts.Where(post => post.Owner == IdentityProvider.GetCurrentUser());
		}

		public override Permission GetOperationPermission() {
			return Permission.Read | Permission.Write;
		}
		public override IQueryable GetPermissions() {
			var currentUser = IdentityProvider.GetCurrentUser();
			return BloggingContext.Posts.Select(post => new { Id = post.Id, Permission = post.Owner == currentUser || post.Blog.Owner == currentUser ? Permission.Write | Permission.Read | Permission.Delete : Permission.Read });
		}
	}
}
