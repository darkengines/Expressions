using Esprima.Ast;
using System;
using System.Collections.Generic;

namespace Darkengines.Expressions.Converters {
	public class BinaryExpressionConverter : ExpressionConverter<BinaryExpression> {
		public override Esprima.Ast.Nodes NodeType => Esprima.Ast.Nodes.BinaryExpression;
		public override (System.Linq.Expressions.Expression, ExpressionConversionResult) Convert(BinaryExpression node, ExpressionConverterContext expressionConverterContext, ExpressionConverterScope expressionConverterScope, bool allowTerminal, params Type[] genericArguments) {
			var leftOperand = expressionConverterContext.ExpressionConverterResolver.Convert(node.Left, expressionConverterContext, new ExpressionConverterScope(expressionConverterScope, null), allowTerminal);
			var rightOperand = expressionConverterContext.ExpressionConverterResolver.Convert(node.Right, expressionConverterContext, new ExpressionConverterScope(expressionConverterScope, null), allowTerminal);
			if (rightOperand.Type != leftOperand.Type) rightOperand = System.Linq.Expressions.Expression.Convert(rightOperand, leftOperand.Type);
			return (System.Linq.Expressions.Expression.MakeBinary(BinaryOperatorMap[node.Operator], leftOperand, rightOperand), DefaultResult);
		}
		protected static Dictionary<BinaryOperator, System.Linq.Expressions.ExpressionType> BinaryOperatorMap = new Dictionary<BinaryOperator, System.Linq.Expressions.ExpressionType>() {
			{ BinaryOperator.BitwiseAnd, System.Linq.Expressions.ExpressionType.And  },
			{ BinaryOperator.BitwiseOr, System.Linq.Expressions.ExpressionType.Or  },
			{ BinaryOperator.BitwiseXOr, System.Linq.Expressions.ExpressionType.ExclusiveOr  },
			{ BinaryOperator.Divide, System.Linq.Expressions.ExpressionType.Divide },
			{ BinaryOperator.Equal, System.Linq.Expressions.ExpressionType.Equal  },
			{ BinaryOperator.Greater, System.Linq.Expressions.ExpressionType.GreaterThan  },
			{ BinaryOperator.GreaterOrEqual, System.Linq.Expressions.ExpressionType.GreaterThanOrEqual  },
			{ BinaryOperator.InstanceOf, System.Linq.Expressions.ExpressionType.TypeIs  },
			{ BinaryOperator.LeftShift, System.Linq.Expressions.ExpressionType.LeftShift  },
			{ BinaryOperator.Less, System.Linq.Expressions.ExpressionType.LessThan  },
			{ BinaryOperator.LessOrEqual, System.Linq.Expressions.ExpressionType.LessThanOrEqual  },
			{ BinaryOperator.LogicalAnd, System.Linq.Expressions.ExpressionType.AndAlso  },
			{ BinaryOperator.LogicalOr, System.Linq.Expressions.ExpressionType.OrElse  },
			{ BinaryOperator.Minus, System.Linq.Expressions.ExpressionType.Subtract  },
			{ BinaryOperator.Modulo, System.Linq.Expressions.ExpressionType.Modulo  },
			{ BinaryOperator.NotEqual, System.Linq.Expressions.ExpressionType.NotEqual  },
			{ BinaryOperator.Plus, System.Linq.Expressions.ExpressionType.Add  },
			{ BinaryOperator.RightShift, System.Linq.Expressions.ExpressionType.RightShift  },
			{ BinaryOperator.StricltyNotEqual, System.Linq.Expressions.ExpressionType.NotEqual  },
			{ BinaryOperator.StrictlyEqual, System.Linq.Expressions.ExpressionType.Equal  },
			{ BinaryOperator.Times, System.Linq.Expressions.ExpressionType.Multiply  },
			{ BinaryOperator.UnsignedRightShift, System.Linq.Expressions.ExpressionType.RightShift  },
		};
	}
}
