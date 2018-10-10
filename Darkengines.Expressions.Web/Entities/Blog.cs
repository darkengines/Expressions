using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Darkengines.Expressions.Web.Entities {
	public class Blog {
		public Blog() {
			Posts = new Collection<Post>();
		}
		public int Id { get; set; }
		public int OwnerId { get; set; }
		public virtual User Owner { get; set; }
		public virtual ICollection<Post> Posts { get; }
	}
}
