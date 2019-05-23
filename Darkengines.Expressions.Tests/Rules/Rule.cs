using Darkengines.Expressions.Security;
using Darkengines.Expressions.Tests.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Darkengines.Expressions.Tests {
	public abstract class Rule<T> : IRule {
		public string Name => GetType().Name;
		public virtual bool CanHandle(Type type) {
			return type == typeof(T);
		}
        LambdaExpression IRule.GetPermission(User user)
        {
            return GetPermission(user);
        }

        public abstract Expression<Func<T, Permission>> GetPermission(User user);
	}
}
