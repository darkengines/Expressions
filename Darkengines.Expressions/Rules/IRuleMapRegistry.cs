using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Darkengines.Expressions.Rules {
	public class IRuleMapRegistry {
		protected IEnumerable<IRuleMap> RuleMaps { get; }
		protected IEnumerable<IRuleMapsProvider> RuleMapProviders { get; }

	}
	public class RuleMapRegistry {
		protected IEnumerable<IRuleMap> RuleMaps { get; }
		protected IDictionary<(Type, object), IRuleMap> Registry { get; }
		public RuleMapRegistry(IEnumerable<IRuleMap> ruleMaps, IEnumerable<IRuleMapsProvider> ruleMapProviders) {
			RuleMaps = ruleMaps.Union(ruleMapProviders.SelectMany(rmp => rmp.RuleMaps)).ToArray();
			Registry = new Dictionary<(Type, object), IRuleMap>();
		}
		public IRuleMap GetRuleMap(Type type, object context) {
			IRuleMap ruleMap;
			if (!Registry.TryGetValue((type, context), out ruleMap)) {
				ruleMap = RuleMaps.Where(rm => rm.CanHandle(type, context)).BuildRuleMap();
				Registry[(type, context)] = ruleMap;
			}
			return ruleMap;
		}
	}
}
