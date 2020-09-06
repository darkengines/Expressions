using System.Collections.Generic;
using System.Linq.Expressions;

namespace Darkengines.Expressions {
	public interface IIdentifierProvider {
		Dictionary<string, Expression> Identifiers { get; }
	}
}
