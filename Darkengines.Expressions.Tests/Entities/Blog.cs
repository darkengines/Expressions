using Darkengines.Expressions.Security;
using Darkengines.Expressions.Tests.Rules;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Darkengines.Expressions.Tests.Entities {
	public class Blog {
		public Blog() {
			Posts = new Collection<Post>();
		}
		public int Id { get; set; }
		public int OwnerId { get; set; }
		public virtual User Owner { get; set; }
		public virtual ICollection<Post> Posts { get; }
	}
    public class BlogRuleMap : RuleMap<Blog> {
        public BlogRuleMap() {
            var blogRule = new BlogRule();
            Self(blogRule.GetPermission);
			Property(blog => blog.Posts, currentUser => blog => blog.OwnerId == currentUser.Id ? Permission.Read | Permission.Write : Permission.None);
		}
    }
}
