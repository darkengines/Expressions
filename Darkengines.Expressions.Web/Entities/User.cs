using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace Darkengines.Expressions.Web.Entities {
	public class User {
		public User() {
			Blogs = new Collection<Blog>();
			Posts = new Collection<Post>();
			Comments = new Collection<Comment>();
		}
		[DisplayName(nameof(Id))]
		public int Id { get; set; }
		[DisplayName(nameof(DisplayName))]
		public string DisplayName { get; set; }
		[DisplayName(nameof(Blogs))]
		public virtual ICollection<Blog> Blogs { get; }
		[DisplayName(nameof(Posts))]
		public virtual ICollection<Post> Posts { get; }
		[DisplayName(nameof(Comments))]
		public virtual ICollection<Comment> Comments { get; }
		[DisplayName(nameof(HashedPassword))]
		public string HashedPassword { get; set; }
	}
}
