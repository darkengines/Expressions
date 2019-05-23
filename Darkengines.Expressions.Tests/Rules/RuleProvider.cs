using Darkengines.Expressions.Tests.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Darkengines.Expressions.Tests.Rules {
    public class RuleProvider {
        protected IEnumerable<IRuleMap> Maps { get; }
        public RuleProvider(IEnumerable<IRuleMap> maps) {
            Maps = maps;
        }
        public ISet<Func<User, LambdaExpression>> GetRulesFor<T>() {
            return GetRulesFor(typeof(T));
        }
        public IRuleMap GetRuleMapFor<T>() {
            return GetRuleMapFor(typeof(T));
        }
        public IRuleMap GetRuleMapFor(Type type) {
            return Maps.FirstOrDefault(rm => rm.Type == type);
        }
        public ISet<Func<User, LambdaExpression>> GetRulesFor(Type type) {
            return Maps.FirstOrDefault(map => map.Type == type)?.Self ?? new HashSet<Func<User, LambdaExpression>>();
        }
        public ISet<Func<User, LambdaExpression>> GetRulesFor<T>(Expression<Func<T, object>> propertyAccessExpression) {
            var propertyInfo = ExpressionHelper.ExtractPropertyInfo(propertyAccessExpression);
            return GetRulesFor(propertyInfo);
        }
        public ISet<Func<User, LambdaExpression>> GetRulesFor(PropertyInfo propertyInfo) {
            var dictionary = Maps.FirstOrDefault(map => map.Type == propertyInfo.DeclaringType);
            if (dictionary != null) {
                ISet<Func<User, LambdaExpression>> rules = null;
                var hasRules = dictionary.Properties.TryGetValue(propertyInfo, out rules);
                if (hasRules) return rules;
            }
            return new HashSet<Func<User, LambdaExpression>>();
        }
    }
}
