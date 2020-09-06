using DarkEngines.Expressions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace Darkengines.Expressions.Converters {
	public class NewExpressionConverter : ExpressionConverter<Esprima.Ast.ObjectExpression> {
		public override Esprima.Ast.Nodes NodeType => Esprima.Ast.Nodes.ObjectExpression;
		protected AnonymousTypeBuilder AnonymousTypeBuilder { get; }
		protected IEqualityComparer<HashSet<Tuple<Type, string>>> SetComparer { get; }
		protected Dictionary<HashSet<Tuple<Type, string>>, Type> Cache { get; }
		protected JsonSerializer JsonSerializer { get; }

		public NewExpressionConverter(AnonymousTypeBuilder anonymousTypeBuilder, JsonSerializer jsonSerializer) : base() {
			AnonymousTypeBuilder = anonymousTypeBuilder;
			var comparer = HashSet<Tuple<Type, string>>.CreateSetComparer();
			Cache = new Dictionary<HashSet<Tuple<Type, string>>, Type>(comparer);
			JsonSerializer = jsonSerializer;
		}
		public override (Expression, ExpressionConversionResult) Convert(Esprima.Ast.ObjectExpression objectExpression, ExpressionConverterContext context, ExpressionConverterScope scope, bool allowTerminal, params Type[] genericArguments) {
			if (scope.TargetType != null) {
				var code = context.Source.Substring(objectExpression.Range.Start, objectExpression.Range.End - objectExpression.Range.Start + 1);
				using (var reader = new StringReader(code)) {
					var @object = JsonSerializer.Deserialize(reader, scope.TargetType);
					return (Expression.Constant(@object), DefaultResult);
				}
			}
			var propertyValueExpressions = objectExpression.Properties.Select(property => new {
				Property = property,
				ValueExpression = context.ExpressionConverterResolver.Convert(property.Value, context, scope, false)
			}).ToArray();
			var tuples = propertyValueExpressions.Select(propertyValueExpression => new Tuple<Type, string>(propertyValueExpression.ValueExpression.Type, ((Esprima.Ast.Identifier)propertyValueExpression.Property.Key).Name)).ToArray();
			var set = new HashSet<Tuple<Type, string>>(tuples);
			Type anonymousType = null;
			if (!Cache.TryGetValue(set, out anonymousType)) {
				anonymousType = AnonymousTypeBuilder.BuildAnonymousType(set);
				Cache[set] = anonymousType;
			}
			var newExpression = Expression.New(anonymousType.GetConstructor(new Type[0]));
			var initializationExpression = Expression.MemberInit(
				newExpression,
				propertyValueExpressions.Select(pve => Expression.Bind(anonymousType.GetProperty(((Esprima.Ast.Identifier)pve.Property.Key).Name), pve.ValueExpression))
			);
			return (initializationExpression, DefaultResult);
		}
	}
}
