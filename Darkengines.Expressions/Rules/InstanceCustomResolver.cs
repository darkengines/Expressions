using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Darkengines.Expressions.Rules {
	public class InstanceCustomResolver<TContext, T, TResoled> {
		public Expression<Func<TContext, T, object>> Resolver { get; set; }
		public Func<Expression, Expression, Expression> Reducer { get; set; }
	}
}
