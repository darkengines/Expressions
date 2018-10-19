using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace Darkengines.Expressions.Web.Entities {
	public class Blog {
		public Blog() {
			Posts = new Collection<Post>();
		}
		[DisplayName(nameof(Id))]
		public int Id { get; set; }
		[DisplayName(nameof(OwnerId))]
		public int OwnerId { get; set; }
		[DisplayName(nameof(Owner))]
		public virtual User Owner { get; set; }
		[DisplayName(nameof(Posts))]
		public virtual ICollection<Post> Posts { get; }
	}
}
