using System;
using System.Collections.Generic;
using System.Text;
using Darkengines.Expressions.Tests.Entities;

namespace Darkengines.Expressions.Tests {
	public class TestIdentityProvider : IIdentityProvider {
		protected User CurrentUser { get; }

		public TestIdentityProvider() {
			CurrentUser = new User() {
				Id = 4
			};
		}

		public User GetCurrentUser() {
			return CurrentUser;
		}
	}
}
