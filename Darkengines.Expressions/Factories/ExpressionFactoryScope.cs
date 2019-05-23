﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Darkengines.Expressions.Factories {
	public class ExpressionFactoryScope {
		public ExpressionFactoryScope(ExpressionFactoryScope parent, Type targetType) {
			Parent = parent;
			TargetType = targetType;
			GenericTypeResolutionMap = new Dictionary<Type, Type>();
			Variables = new Dictionary<string, Expression>();

		}
		public ExpressionFactoryScope Parent { get; }
		public Type TargetType { get; }
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
