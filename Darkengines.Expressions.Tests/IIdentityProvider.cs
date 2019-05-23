using Darkengines.Expressions.Tests.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Darkengines.Expressions.Tests {
	public interface IIdentityProvider {
		User GetCurrentUser();
	}
}
