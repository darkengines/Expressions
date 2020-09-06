using DarkEngines.Expressions;
using Esprima.Ast;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using Expression = System.Linq.Expressions.Expression;

namespace Darkengines.Expressions.Converters {
	public class ArrayExpressionConverter : ExpressionConverter<ArrayExpression> {
		public ArrayExpressionConverter(JsonSerializer jsonSerializer) : base() {
			JsonSerializer = jsonSerializer;
		}
		public override Esprima.Ast.Nodes NodeType => Esprima.Ast.Nodes.ArrayExpression;
		protected JsonSerializer JsonSerializer { get; }
		public override (Expression, ExpressionConversionResult) Convert(ArrayExpression node, ExpressionConverterContext expressionConverterContext, ExpressionConverterScope expressionConverterScope, bool allowTerminal, params Type[] genericArguments) {
			if (expressionConverterScope.TargetType == typeof(JArray)) {
				var code = expressionConverterContext.Source.Substring(node.Range.Start, node.Range.End - node.Range.Start + 1);
				using (var reader = new StringReader(code)) {
					var @object = JsonSerializer.Deserialize(reader, expressionConverterScope.TargetType);
					return (Expression.Constant(@object), DefaultResult);
				}
			}
			var targetType = expressionConverterScope.TargetType != null ? expressionConverterScope.TargetType.GetEnumerableUnderlyingType() : null;
			var itemScope = new ExpressionConverterScope(expressionConverterScope, targetType);
			var elementsExpressions = node.Elements.Select((element, index) => {
				var itemExpression = expressionConverterContext.ExpressionConverterResolver.Convert(element, expressionConverterContext, itemScope, allowTerminal);
				if (index == 0 && targetType == null) targetType = itemExpression.Type;
				if (itemExpression.Type != targetType && targetType != null) itemExpression = Expression.Convert(itemExpression, targetType);
				return itemExpression;
			}).ToArray();
			return (Expression.NewArrayInit(targetType, elementsExpressions), DefaultResult);
		}
	}
}
