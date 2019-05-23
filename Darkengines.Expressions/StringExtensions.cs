using System;
using System.Collections.Generic;
using System.Text;

namespace Darkengines.Expressions {
	public static class StringExtensions {
		public static string ToPascalCase(this string @string) {
			return $"{char.ToUpper(@string[0])}{@string.Substring(1)}";
		}
	}
}
