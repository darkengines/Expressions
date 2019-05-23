using Darkengines.Expressions.Security;
using Darkengines.Expressions.Tests.Rules;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Text;

namespace Darkengines.Expressions.Tests.Entities {
	public class User {
		public User() {
			Blogs = new Collection<Blog>();
			Posts = new Collection<Post>();
			Comments = new Collection<Comment>();
		}
		public int Id { get; set; }
		public string DisplayName { get; set; }
		public virtual ICollection<Blog> Blogs { get; }
		public virtual ICollection<Post> Posts { get; }
		public virtual ICollection<Comment> Comments { get; }
		public string HashedPassword { get; set; }
	}
	public class UserRuleMap : RuleMap<User> {
		public UserRuleMap() {
			Self(currentUser => u => u.Id == currentUser.Id ? Permission.Delete | Permission.Write | Permission.Read : Permission.Read);
			Property(user => user.HashedPassword, currentUser => user => user.Id == currentUser.Id ? Permission.Read | Permission.Write : Permission.None);
			Property(user => user.Blogs, currentUser => user => user.Id == currentUser.Id ? Permission.Read | Permission.Write : Permission.Read);
		}
	}
}
