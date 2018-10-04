using System;
using System.Collections.Generic;
using System.Text;

namespace Darkengines.Expressions.Factories {
	public class ExpressionFactoryScope {
		public ExpressionFactoryScope(ExpressionFactoryScope parent, Type targetType) {
			Parent = parent;
			TargetType = targetType;
			GenericTypeResolutionMap = new Dictionary<Type, Type>();
		}
		public ExpressionFactoryScope Parent { get; }
		public Type TargetType { get; }
		public Dictionary<Type, Type> GenericTypeResolutionMap { get; }
	}
}
