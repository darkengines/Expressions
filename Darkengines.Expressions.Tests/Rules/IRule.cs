using Darkengines.Expressions.Tests.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Darkengines.Expressions.Tests {
	public interface IRule {
		string Name { get; }
		bool CanHandle(Type type);
		LambdaExpression GetPermission(User user);
	}
}
