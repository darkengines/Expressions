using Darkengines.Expressions.Models;
using Esprima.Ast;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Darkengines.Expressions.ModelConverters.Javascript {
	public class UnaryExpressionJavascriptConverter : JavascriptModelConverter<Esprima.Ast.UnaryExpression> {
		protected static Dictionary<UnaryOperator, ExpressionType> UnaryOperatorMap = new Dictionary<UnaryOperator, ExpressionType>() {
			{ UnaryOperator.BitwiseNot, ExpressionType.Not },
			{ UnaryOperator.Decrement, ExpressionType.Decrement },
			{ UnaryOperator.Increment, ExpressionType.Increment },
			{ UnaryOperator.LogicalNot, ExpressionType.Not },
		};
		public override ExpressionModel Convert(Esprima.Ast.UnaryExpression node, ModelConverterContext context) {
			var operandConverter = context.ModelConverters.FindModelConverterFor(node.Argument, context);
			var operandModel = operandConverter.Convert(node.Argument, context);
			var expressionType = UnaryOperatorMap[node.Operator];
			return new UnaryExpressionModel() {
				ExpressionType = expressionType,
				Operand = operandModel
			};
		}
	}
}
