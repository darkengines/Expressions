using Darkengines.Expressions.Rules;
using System.Collections.Generic;
using System.Reflection;

namespace Darkengines.Expressions.Converters {
	public class ExpressionConverterContext {
		public ExpressionConverterResolver ExpressionConverterResolver { get; set; }
		public IEnumerable<MethodInfo> ExtensionMethods { get; set; }
		public IEnumerable<MethodInfo> StaticMethods { get; set; }
		public RuleMapRegistry RuleMapRegistry { get; set; }
		//Shit, should be a permission resolver
		public bool IsAdmin { get; set; }
		public IEnumerable<ICustomMethodCallExpressionConverter> CustomMethodCallExpressionConverters { get; set; }
		public IEnumerable<IRuleMap> RuleMaps { get; set; }
		public object securityContext { get; set; }
		public string Source { get; set; }
	}
}
