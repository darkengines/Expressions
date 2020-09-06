using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Darkengines.Expressions.Converters {
	public class ExpressionConverterScope {
		public ExpressionConverterScope(ExpressionConverterScope parent, Type targetType) {
			Parent = parent;
			TargetType = targetType;
			GenericTypeResolutionMap = parent != null ? new Dictionary<Type, Type>(parent.GenericTypeResolutionMap) : new Dictionary<Type, Type>();
			Variables = new Dictionary<string, Expression>();

		}
		public ExpressionConverterScope Parent { get; }
		public Type TargetType { get; set; }
		public Dictionary<Type, Type> GenericTypeResolutionMap { get; set; }
		public IDictionary<string, Expression> Variables { get; set; }
		public Expression FindIdentifier(string identifier) {
			Expression value = null;
			if (Variables.TryGetValue(identifier, out value)) {
				return value;
			} else if (Parent != null) {
				return Parent.FindIdentifier(identifier);
			} else {
				throw new UnresolvedIdentifierException(identifier);
			}
		}
	}
}
