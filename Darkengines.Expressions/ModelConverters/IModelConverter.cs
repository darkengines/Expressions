using Darkengines.Expressions.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Darkengines.Expressions.ModelConverters {
	public interface IModelConverter {
		bool CanHandle(object @object, ModelConverterContext context);
		ExpressionModel Convert(object @object, ModelConverterContext context);
	}
}
