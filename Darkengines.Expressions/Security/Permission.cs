using System;

namespace Darkengines.Expressions.Security {
	[Flags]
	public enum Permission {
		None = 0,
		Read = 1,
		Write = 2,
		Delete = 4,
		Create = 8,
		All = Read | Write | Delete |Create,
		Admin = All | 16
	}
}
