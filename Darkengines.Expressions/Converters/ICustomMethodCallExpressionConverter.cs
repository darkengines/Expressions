using System;
using System.Linq.Expressions;

namespace Darkengines.Expressions.Converters {
	public interface ICustomMethodCallExpressionConverter {
		bool CanHandle(Esprima.Ast.CallExpression methodCallExpression, ExpressionConverterContext context, ExpressionConverterScope scope, bool allowTerminal, params Type[] genericArguments);
		(Expression, ExpressionConversionResult) Convert(Esprima.Ast.CallExpression methodCallExpression, ExpressionConverterContext context, ExpressionConverterScope scope, bool allowTerminal, params Type[] genericArguments);
	}
}
