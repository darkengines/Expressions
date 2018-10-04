using System;
using System.Collections.Generic;
using System.Text;

namespace Darkengines.Expressions.ModelConverters {
	public class ModelConverterContext {
		public IEnumerable<IModelConverter> ModelConverters { get; set; }
	}
}
