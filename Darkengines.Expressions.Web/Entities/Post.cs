using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Darkengines.Expressions.Web.Entities {
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
}
