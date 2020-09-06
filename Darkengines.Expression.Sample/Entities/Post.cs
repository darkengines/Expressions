using Darkengines.Expressions.Rules;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Darkengines.Expressions.Sample.Entities {
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
    public class PostRuleMap : RuleMap<Context, Post> {
        public PostRuleMap(): base() {
		}
    }
}
