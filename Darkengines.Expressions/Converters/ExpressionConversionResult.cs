using System;
using System.Collections.Generic;
using System.Text;

namespace Darkengines.Expressions.Converters {
	public class ExpressionConversionResult {
		public bool ShouldApplyFilter { get; set; } = true;
		public bool ShouldApplyProjection { get; set; } = true;

	}
}
