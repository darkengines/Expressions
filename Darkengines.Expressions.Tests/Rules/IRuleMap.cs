using Darkengines.Expressions.Tests.Entities;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Darkengines.Expressions.Tests.Rules {
	public interface IRuleMap {
		Type Type { get; }
		ISet<Func<User, LambdaExpression>> Self { get; }
		IDictionary<PropertyInfo, ISet<Func<User, LambdaExpression>>> Properties { get; }
	}
}
