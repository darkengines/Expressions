using Darkengines.Expressions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Darkengines.Expressions.ModelConverters {
	public static class IEnumerableExtensions {
		public static IModelConverter FindModelConverterFor(this IEnumerable<IModelConverter> modelConverters, object @object, ModelConverterContext context) {
			return modelConverters.First(modelConverter => modelConverter.CanHandle(@object, context));
		}
	}
}
