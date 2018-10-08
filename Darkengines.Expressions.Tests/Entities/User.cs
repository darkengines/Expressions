using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Darkengines.Expressions.Tests.Entities {
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
}
