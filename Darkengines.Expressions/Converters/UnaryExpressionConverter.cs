using Esprima.Ast;
using System;
using System.Collections.Generic;

namespace Darkengines.Expressions.Converters {
	public class UnaryExpressionConverter : ExpressionConverter<UnaryExpression> {
		public override Esprima.Ast.Nodes NodeType => Esprima.Ast.Nodes.UnaryExpression;
		public override (System.Linq.Expressions.Expression, ExpressionConversionResult) Convert(UnaryExpression node, ExpressionConverterContext expressionConverterContext, ExpressionConverterScope expressionConverterScope, bool allowTerminal, params Type[] genericArguments) {
			var operand = expressionConverterContext.ExpressionConverterResolver.Convert(node.Argument, expressionConverterContext, expressionConverterScope, allowTerminal);
			return (System.Linq.Expressions.Expression.MakeUnary(UnaryOperatorMap[node.Operator], operand, expressionConverterScope.TargetType), DefaultResult);
		}
		protected static Dictionary<UnaryOperator, System.Linq.Expressions.ExpressionType> UnaryOperatorMap = new Dictionary<UnaryOperator, System.Linq.Expressions.ExpressionType>() {
			{ UnaryOperator.BitwiseNot, System.Linq.Expressions.ExpressionType.Not},
			{ UnaryOperator.LogicalNot, System.Linq.Expressions.ExpressionType.Not},
			{ UnaryOperator.Minus, System.Linq.Expressions.ExpressionType.Negate},
			{ UnaryOperator.Plus, System.Linq.Expressions.ExpressionType.UnaryPlus},
		};
	}
}
