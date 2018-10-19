using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace Darkengines.Expressions.Web.Entities {
	public class Post {
		public Post() {
			Comments = new Collection<Comment>();
		}
		[DisplayName(nameof(Id))]
		public int Id { get; set; }
		[DisplayName(nameof(OwnerId))]
		public int OwnerId { get; set; }
		[DisplayName(nameof(Owner))]
		public virtual User Owner { get; set; }
		[DisplayName(nameof(BlogId))]
		public int BlogId { get; set; }
		[DisplayName(nameof(Blog))]
		public virtual Blog Blog { get; set; }
		[DisplayName(nameof(Comments))]
		public virtual ICollection<Comment> Comments { get; }
		[DisplayName(nameof(Content))]
		public string Content { get; set; }
	}
}
