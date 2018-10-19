using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Darkengines.Expressions.Web.Entities {
	public class Comment {
		[DisplayName(nameof(Id))]
		public int Id { get; set; }
		[DisplayName(nameof(OwnerId))]
		public int OwnerId { get; set; }
		[DisplayName(nameof(Owner))]
		public virtual User Owner { get; set; }
		[DisplayName(nameof(PostId))]
		public int PostId { get; set; }
		[DisplayName(nameof(Post))]
		public virtual Post Post { get; set; }
		[DisplayName(nameof(Content))]
		public string Content { get; set; }
	}
}
