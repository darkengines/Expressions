using Darkengines.Expressions.Models;
using Esprima.Ast;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Darkengines.Expressions.ModelConverters.Javascript {
	public class BinaryExpressionJavascriptConverter : JavascriptModelConverter<Esprima.Ast.BinaryExpression> {
		protected static Dictionary<BinaryOperator, ExpressionType> BinaryOperatorMap = new Dictionary<BinaryOperator, ExpressionType>() {
			{ BinaryOperator.BitwiseAnd, ExpressionType.And  },
			{ BinaryOperator.BitwiseOr, ExpressionType.Or  },
			{ BinaryOperator.BitwiseXOr, ExpressionType.ExclusiveOr  },
			{ BinaryOperator.Divide, ExpressionType.Divide },
			{ BinaryOperator.Equal, ExpressionType.Equal  },
			{ BinaryOperator.Greater, ExpressionType.GreaterThan  },
			{ BinaryOperator.GreaterOrEqual, ExpressionType.GreaterThanOrEqual  },
			{ BinaryOperator.InstanceOf, ExpressionType.TypeIs  },
			{ BinaryOperator.LeftShift, ExpressionType.LeftShift  },
			{ BinaryOperator.Less, ExpressionType.LessThan  },
			{ BinaryOperator.LessOrEqual, ExpressionType.LessThanOrEqual  },
			{ BinaryOperator.LogicalAnd, ExpressionType.AndAlso  },
			{ BinaryOperator.LogicalOr, ExpressionType.OrElse  },
			{ BinaryOperator.Minus, ExpressionType.Subtract  },
			{ BinaryOperator.Modulo, ExpressionType.Modulo  },
			{ BinaryOperator.NotEqual, ExpressionType.NotEqual  },
			{ BinaryOperator.Plus, ExpressionType.Add  },
			{ BinaryOperator.RightShift, ExpressionType.RightShift  },
			{ BinaryOperator.StricltyNotEqual, ExpressionType.NotEqual  },
			{ BinaryOperator.StrictlyEqual, ExpressionType.Equal  },
			{ BinaryOperator.Times, ExpressionType.Multiply  },
			{ BinaryOperator.UnsignedRightShift, ExpressionType.RightShift  },
		};

		public override ExpressionModel Convert(Esprima.Ast.BinaryExpression node, ModelConverterContext context) {
			var leftConverter = context.ModelConverters.FindModelConverterFor(node.Left, context);
			var rightConverter = context.ModelConverters.FindModelConverterFor(node.Right, context);
			var left = leftConverter.Convert(node.Left, context);
			var right = rightConverter.Convert(node.Right, context);
			return new BinaryExpressionModel() {
				ExpressionType = BinaryOperatorMap[node.Operator],
				Left = left,
				Right = right
			};
		}
	}
}
