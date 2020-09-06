using Esprima.Ast;
using System.Collections.Generic;
using System.Linq;

namespace Darkengines.Expressions.Converters {
	public class ExpressionConverterResolver {
		protected IEnumerable<IExpressionConverter> ExpressionConverters { get; }
		protected IDictionary<Nodes, IEnumerable<IExpressionConverter>> ExpressionConverterTypeMap { get; }
		public ExpressionConverterResolver(IEnumerable<IExpressionConverter> expressionConverters) {
			ExpressionConverters = expressionConverters;
			ExpressionConverterTypeMap = ExpressionConverters.GroupBy(ec => ec.NodeType).ToDictionary(g => g.Key, e => e.AsEnumerable());
			ExpressionConverterTypeMap[Nodes.LogicalExpression] = ExpressionConverterTypeMap[Nodes.BinaryExpression];
		}
		public IExpressionConverter FindExpressionConverter(INode node) {
			return ExpressionConverterTypeMap[node.Type].FirstOrDefault(ec => ec.CanHandle(node));
		}
		public System.Linq.Expressions.Expression Convert(INode node, ExpressionConverterContext expressionConverterContext, ExpressionConverterScope expressionConverterScope, bool allowTerminal) {
			return FindExpressionConverter(node).Convert(node, expressionConverterContext, expressionConverterScope, allowTerminal);
		}
	}
}
