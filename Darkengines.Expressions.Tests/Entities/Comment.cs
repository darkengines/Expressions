using Darkengines.Expressions.Tests.Rules;
using System;
using System.Collections.Generic;
using System.Text;

namespace Darkengines.Expressions.Tests.Entities {
	public class Comment {
		public int Id { get; set; }
		public int OwnerId { get; set; }
		public virtual User Owner { get; set; }
		public int PostId { get; set; }
		public virtual Post Post { get; set; }
		public string Content { get; set; }
	}
    public class CommentRuleMap : RuleMap<Comment> {
        public CommentRuleMap() {
            var commentRule = new CommentRule();
            Self(commentRule.GetPermission);
        }
    }
}
