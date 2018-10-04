using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Darkengines.Expressions.Generator {
	public class Program {
		static void Main(string[] args) {
			//var baseExpressionType = typeof(Expression);
			//var staticMethods = baseExpressionType.GetMethods(BindingFlags.Static | BindingFlags.Public).ToArray();
			//var staticMethodGroups = staticMethods.GroupBy(staticMethod => staticMethod.Name);
			//foreach (var staticMethodGroup in staticMethodGroups) {
			//	var choices = staticMethodGroup.ToArray();
			//	var lines = choices.Select((staticMethod, index) => $"[{index}] {staticMethod.ToString()}");

			//	int? chosenIndex = -1;
			//	do {
			//		Console.Out.WriteLine();
			//		Console.Out.WriteLine(staticMethodGroup.Key);
			//		foreach (var line in lines) Console.Out.WriteLine(line);
			//		var input = Console.In.ReadLine();
			//		if (string.IsNullOrWhiteSpace(input)) {
			//			chosenIndex = null;
			//		} else {
			//			int index = 0;
			//			if (int.TryParse(input, out index)) {
			//				chosenIndex = index;
			//			}
			//		}
			//	} while (chosenIndex < 0 || chosenIndex >= choices.Length);

			//	if (chosenIndex != null) {
			//		var chosenMethod = choices[(int)chosenIndex];
			//		WriteClass(chosenMethod);
			//	}
			//}
		}
		public static void WriteClass(MethodInfo methodInfo) {
			var className = $"{methodInfo.Name}Expression";
			var parameters = methodInfo.GetParameters();
			var fixedParameters = parameters.Select(parameter => new {
				Parameter = parameter,
				TypeName = typeof(Expression).IsAssignableFrom(parameter.ParameterType) ? (parameter.ParameterType == typeof(Expression) ? "Expression" : $"{parameter.ParameterType.Name}") : parameter.ParameterType.Name,
				CamelCasedName = parameter.Name.ToCamelCase(),
				PascalCasedName = parameter.Name.ToPascalCase(),
			}).ToArray();
			using (var stream = new FileStream($@"C:\Users\root\source\repos\Darkengines.Expressions\Darkengines.Expressions\{className}.cs", FileMode.Create, FileAccess.Write)) {
				using (var writer = new StreamWriter(stream)) {
					writer.WriteLine("using System;");
					writer.WriteLine("using System.Linq;");
					writer.WriteLine("using System.Linq.Expressions;");
					writer.WriteLine("using System.Reflection;");
					writer.WriteLine("namespace Darkengines.Expressions.Modelss {");
					writer.WriteLine($"	public class {className}: Expression {{");
					writer.WriteLine($"		public {className}({string.Join(", ", fixedParameters.Select(fixedParameter => $"{fixedParameter.TypeName} {fixedParameter.CamelCasedName}"))}) {{");
					foreach (var fixedParameter in fixedParameters) {
						writer.WriteLine($"			{fixedParameter.PascalCasedName} = {fixedParameter.CamelCasedName};");
					}
					writer.WriteLine("		}");
					foreach (var fixedParameter in fixedParameters) {
						writer.WriteLine($"		public {fixedParameter.TypeName} {fixedParameter.PascalCasedName} {{ get; set; }}");
					}
					writer.WriteLine("	}");
					writer.Write("}");
				}
			}
		}
	}
}
