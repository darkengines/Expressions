namespace Darkengines.Expressions {
	public static class StringExtensions {
		public static string ToPascalCase(this string @string) {
			return $"{char.ToUpper(@string[0])}{@string.Substring(1)}";
		}
		public static string ToCamelCase(this string @string) {
			return $"{char.ToLower(@string[0])}{@string.Substring(1)}";
		}
	}
}
