using Darkengines.Expressions.Security;
using Darkengines.Expressions.Tests.Entities;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Darkengines.Expressions.Tests.Rules {
	public class RuleMap<T>: IRuleMap where T : class {
		public ISet<Func<User, LambdaExpression>> SelfRules { get; protected set; } = new HashSet<Func<User, LambdaExpression>>();
		public Dictionary<PropertyInfo, ISet<Func<User, LambdaExpression>>> PropertiesRules { get; protected set; } = new Dictionary<PropertyInfo, ISet<Func<User, LambdaExpression>>>();

		public Type Type => typeof(T);

		ISet<Func<User, LambdaExpression>> IRuleMap.Self => SelfRules;

		public IDictionary<PropertyInfo, ISet<Func<User, LambdaExpression>>> Properties => PropertiesRules;

		protected RuleMap<T> Self(Func<User, Expression<Func<T, Permission>>> rule) {
			SelfRules.Add(rule);
			return this;
		}
		protected RuleMap<T> Property(Expression<Func<T, object>> propertyAccessExpression, Func<User, Expression<Func<T, Permission>>> rule) {
			ISet<Func<User, LambdaExpression>> set = null;
			var propertyInfo = ExpressionHelper.ExtractPropertyInfo(propertyAccessExpression);
			var setExists = PropertiesRules.TryGetValue(propertyInfo, out set);
			if (!setExists) {
				set = new HashSet<Func<User, LambdaExpression>>();
				PropertiesRules[propertyInfo] = set;
			}
			set.Add(rule);
			return this;
		}
	}
}
