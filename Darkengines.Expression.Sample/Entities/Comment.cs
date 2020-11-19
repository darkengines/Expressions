using Darkengines.Expressions.Rules;
using DarkEngines.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Text;

namespace Darkengines.Expressions.Sample.Entities {
	public class Comment {
		public int Id { get; set; }
		public int OwnerId { get; set; }
		public virtual User Owner { get; set; }
		public int PostId { get; set; }
		public virtual Post Post { get; set; }
		public string Content { get; set; }
	}
	public class CommentRuleMap : RuleMap<Context, Comment> {
		public CommentRuleMap(AnonymousTypeBuilder anonymousTypeBuilder, IModel model) : base(anonymousTypeBuilder, model) {
		}
	}
}
