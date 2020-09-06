using System.Collections.Generic;

namespace Darkengines.Expressions.Rules {
	public interface IRuleMapsProvider {
		IEnumerable<IRuleMap> RuleMaps { get; }
	}
}
