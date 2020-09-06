using System;
using System.Linq.Expressions;

namespace Darkengines.Expressions.Converters {
	public class ConditionalExpressionConverter : ExpressionConverter<Esprima.Ast.ConditionalExpression> {
		public override Esprima.Ast.Nodes NodeType => Esprima.Ast.Nodes.ConditionalExpression;

		public override (Expression, ExpressionConversionResult) Convert(Esprima.Ast.ConditionalExpression node, ExpressionConverterContext expressionConverterContext, ExpressionConverterScope expressionConverterScope, bool allowTerminal, params Type[] genericArguments) {
			var condition = expressionConverterContext.ExpressionConverterResolver.Convert(node.Test, expressionConverterContext, expressionConverterScope, allowTerminal);
			var left = expressionConverterContext.ExpressionConverterResolver.Convert(node.Consequent, expressionConverterContext, expressionConverterScope, allowTerminal);
			var right = expressionConverterContext.ExpressionConverterResolver.Convert(node.Alternate, expressionConverterContext, expressionConverterScope, allowTerminal);
			return (Expression.Condition(condition, left, right), DefaultResult);
		}
	}
}
