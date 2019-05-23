using System;
using System.Collections.Generic;
using System.Text;

namespace Darkengines.Expressions.Security {
	[Flags]
	public enum Permission {
		None = 0,
		Read = 1,
		Write = 2,
		Delete= 4
	}
}
