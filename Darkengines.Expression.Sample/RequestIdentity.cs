using Darkengines.Expressions.Sample.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darkengines.Expression.Sample {
	public class RequestIdentity {
		public RequestIdentity(User user) {
			User = user;
		}
		public User User { get; }
		public override string ToString() {
			return User.ToString();
		}
	}
}
