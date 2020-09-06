using Esprima.Ast;
using Newtonsoft.Json;
using System;
using System.IO;

namespace Darkengines.Expressions.Converters {
	public class ConstantExpressionConverter : ExpressionConverter<Literal> {
		protected JsonSerializer JsonSerializer { get; }
		public ConstantExpressionConverter(JsonSerializer jsonSerializer) : base() {
			JsonSerializer = jsonSerializer;
		}

		public override Esprima.Ast.Nodes NodeType => Esprima.Ast.Nodes.Literal;
		public override (System.Linq.Expressions.Expression, ExpressionConversionResult) Convert(Literal literal, ExpressionConverterContext context, ExpressionConverterScope scope, bool allowTerminal, params Type[] genericArguments) {
			if (literal.Value is string && !(scope.TargetType == typeof(string))) {
				using (var reader = new StringReader($"'{(string)literal.Value}'")) {
					var @object = JsonSerializer.Deserialize(reader, scope.TargetType);
					return (System.Linq.Expressions.Expression.Constant(@object), DefaultResult);
				}
			}
			var expression = (System.Linq.Expressions.Expression)System.Linq.Expressions.Expression.Constant(literal.Value);
			if (scope.TargetType != null && !scope.TargetType.IsGenericParameter && !scope.TargetType.IsGenericType && !scope.TargetType.IsAssignableFrom(expression.Type) && literal.Value != null) expression = System.Linq.Expressions.Expression.Convert(expression, scope.TargetType);
			return (expression, DefaultResult);
		}
	}
}
