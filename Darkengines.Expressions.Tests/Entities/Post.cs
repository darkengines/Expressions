using Darkengines.Expressions.Security;
using Darkengines.Expressions.Tests.Rules;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Darkengines.Expressions.Tests.Entities {
    public class Post {
        public Post() {
            Comments = new Collection<Comment>();
        }
        public int Id { get; set; }
        public int OwnerId { get; set; }
        public virtual User Owner { get; set; }
        public int BlogId { get; set; }
        public virtual Blog Blog { get; set; }
        public virtual ICollection<Comment> Comments { get; }
        public string Content { get; set; }
    }
    public class PostRuleMap : RuleMap<Post> {
        public PostRuleMap() {
            var postRule = new PostRule();
            Self(postRule.GetPermission);
			Property(post => post.Content, currentUser => post => post.OwnerId == currentUser.Id || post.Blog.OwnerId == currentUser.Id ? Permission.Read | Permission.Write : Permission.None);
		}
    }
}
