using Darkengines.Expressions.Models;
using DarkEngines.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Darkengines.Expressions.Factories {
	public class NewExpressionFactory : ExpressionFactory<NewExpressionModel> {
		protected AnonymousTypeBuilder AnonymousTypeBuilder { get; }
		protected IEqualityComparer<HashSet<Tuple<Type, string>>> SetComparer { get; }
		protected Dictionary<HashSet<Tuple<Type, string>>, Type> Cache { get; }
		public NewExpressionFactory() : base() {
			AnonymousTypeBuilder = new AnonymousTypeBuilder("DarkEngines", "DarkEngines.Expressions.ExpressionFactories");
			var comparer = HashSet<Tuple<Type, string>>.CreateSetComparer();
			Cache = new Dictionary<HashSet<Tuple<Type, string>>, Type>(comparer);
		}
		public override Expression BuildExpression(NewExpressionModel expressionModel, ExpressionFactoryContext context, ExpressionFactoryScope scope) {
			var propertyValueExpressions = expressionModel.Properties.Select(property => new {
				Property = property,
				ValueExpression = context.ExpressionFactories.FindExpressionFactoryFor(property.Value, context, scope).BuildExpression(property.Value, context, scope)
			}).ToArray();
			var tuples = propertyValueExpressions.Select(propertyValueExpression => new Tuple<Type, string>(propertyValueExpression.ValueExpression.Type, (propertyValueExpression.Property.PropertyName))).ToArray();
			var set = new HashSet<Tuple<Type, string>>(tuples);
			Type anonymousType = null;
			Cache.TryGetValue(set, out anonymousType);
			if (anonymousType == null) {
				anonymousType = AnonymousTypeBuilder.BuildAnonymousType(set);
				Cache[set] = anonymousType;
			}
			var newExpression = Expression.New(anonymousType.GetConstructor(new Type[0]));
			var initializationExpression = Expression.MemberInit(
				newExpression,
				propertyValueExpressions.Select(pve => Expression.Bind(anonymousType.GetProperty(pve.Property.PropertyName), pve.ValueExpression))
			);
			return initializationExpression;
		}
	}
}
