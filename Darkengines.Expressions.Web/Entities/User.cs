using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Darkengines.Expressions.Web.Entities {
	public class User {
		public User() {
			Blogs = new Collection<Blog>();
			Posts = new Collection<Post>();
			Comments = new Collection<Comment>();
		}
		[DisplayName(nameof(Id))]
		[Display(Name = nameof(Id))]
		[Required]
		public int Id { get; set; }
		[DisplayName(nameof(DisplayName))]
		[Display(Name = nameof(DisplayName))]
		[Required]
		public string DisplayName { get; set; }
		[DisplayName(nameof(Blogs))]
		[Display(Name = nameof(Blogs))]
		[Required]
		public virtual ICollection<Blog> Blogs { get; }
		[DisplayName(nameof(Posts))]
		[Display(Name = nameof(Posts))]
		[Required]
		public virtual ICollection<Post> Posts { get; }
		[DisplayName(nameof(Comments))]
		[Display(Name = nameof(Comments))]
		[Required]
		public virtual ICollection<Comment> Comments { get; }
		[DisplayName(nameof(HashedPassword))]
		[Display(Name = nameof(HashedPassword))]
		[Required]
		public string HashedPassword { get; set; }
	}
}
