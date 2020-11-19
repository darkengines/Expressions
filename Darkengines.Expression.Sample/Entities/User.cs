using Darkengines.Expressions.Rules;
using Darkengines.Expressions.Security;
using DarkEngines.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Text;

namespace Darkengines.Expressions.Sample.Entities {
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
	public class UserRuleMap : RuleMap<Context, User> {
		public UserRuleMap(AnonymousTypeBuilder anonymousTypeBuilder, IModel model) : base(anonymousTypeBuilder, model) {
		}
	}
}
