using Darkengines.Expressions.Security;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Darkengines.Expressions.Rules {
	public class InstanceOptions<TContext, T> {
		public IDictionary<Type, IDictionary<object, ICollection<Expression>>> RelationMapExpressions { get; }
		public InstanceOptions() {
			RelationMapExpressions = new Dictionary<Type, IDictionary<object, ICollection<Expression>>>();
		}
		public InstanceOptions<TContext, T> AddRelation<TRelated>(object key, Expression<Func<TRelated, T, bool>> predicateExpression) {
			IDictionary<object, ICollection<Expression>> predicateExpressions = null;
			if (!RelationMapExpressions.TryGetValue(typeof(TRelated), out predicateExpressions)) {
				predicateExpressions = new Dictionary<object, ICollection<Expression>>();
				RelationMapExpressions[typeof(TRelated)] = predicateExpressions;
			}
			ICollection<Expression> expressions = null;
			if (!predicateExpressions.TryGetValue(key, out expressions)) {
				expressions = new Collection<Expression>();
				predicateExpressions[key] = expressions;
			}
			expressions.Add(predicateExpression);
			return this;
		}
	}
}
