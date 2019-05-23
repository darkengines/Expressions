using Darkengines.Expressions.Security;
using System;
using System.Collections.Generic;
using System.Text;

namespace Darkengines.Expressions.Tests {
	public interface IPermission {
		Permission Permission { get; }
	}
}
