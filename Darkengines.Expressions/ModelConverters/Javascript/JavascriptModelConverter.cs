using System;
using System.Collections.Generic;
using System.Text;
using Darkengines.Expressions.Models;
using Esprima.Ast;

namespace Darkengines.Expressions.ModelConverters.Javascript {
	public abstract class JavascriptModelConverter<TNode> : IModelConverter where TNode: INode {
		public virtual bool CanHandle(object @object, ModelConverterContext context) { return @object is TNode; }
		public abstract ExpressionModel Convert(TNode node, ModelConverterContext context);
		ExpressionModel IModelConverter.Convert(object @object, ModelConverterContext context) { return Convert((TNode)@object, context); }
	}
}
