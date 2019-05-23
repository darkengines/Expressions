using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Darkengines.Expressions {
	public interface IIdentifierProvider {
		Dictionary<string, Expression> Identifiers { get; }
	}
}
