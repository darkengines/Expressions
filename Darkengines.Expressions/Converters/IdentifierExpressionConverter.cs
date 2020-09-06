using Esprima.Ast;
using System;
using System.Linq;
using System.Linq.Expressions;
using LinqExpressions = System.Linq.Expressions;

namespace Darkengines.Expressions.Converters {
	public class IdentifierExpressionConverter : ExpressionConverter<Esprima.Ast.Identifier> {
		public override Esprima.Ast.Nodes NodeType => Esprima.Ast.Nodes.Identifier;
		public override (LinqExpressions.Expression, ExpressionConversionResult) Convert(Esprima.Ast.Identifier identifier, ExpressionConverterContext context, ExpressionConverterScope scope, bool allowTerminal, params Type[] genericArguments) {
			var value = scope.FindIdentifier(identifier.Name);
			return (value, DefaultResult);
		}
	}
}
