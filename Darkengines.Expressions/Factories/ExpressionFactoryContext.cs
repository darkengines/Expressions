using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Darkengines.Expressions.Factories {
	public class ExpressionFactoryContext {
		public IEnumerable<IExpressionFactory> ExpressionFactories { get; set; }
	}
}
