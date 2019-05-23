using Darkengines.Expressions.Security;
using Darkengines.Expressions.Tests.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Darkengines.Expressions.Tests.Security {
	public class CommentSecurityRuleProvider : SecurityRuleProvider<Comment> {
		public CommentSecurityRuleProvider(IIdentityProvider identityProvider, BloggingContext bloggingContext) : base(identityProvider, bloggingContext) {
		}

		public override IQueryable<Comment> GetAccessibleQueryable(Permission permission) {
			return BloggingContext.Comments.Where(comment => comment.Owner == IdentityProvider.GetCurrentUser() || comment.Post.Owner == IdentityProvider.GetCurrentUser());
		}

		public override Permission GetOperationPermission() {
			return Permission.Read | Permission.Write;
		}
		public override IQueryable GetPermissions() {
			var currentUser = IdentityProvider.GetCurrentUser();
			return BloggingContext.Comments.Select(comment => new { Id = comment.Id, Permission = comment.Owner == currentUser || comment.Post.Owner == currentUser ? Permission.Write | Permission.Read | Permission.Delete : Permission.Read });
		}
	}
}
