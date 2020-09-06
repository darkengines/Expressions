using Microsoft.Extensions.DependencyInjection;

namespace Darkengines.Expressions.Converters {
	public static class ExpressionConvertersExtensions {
		public static IServiceCollection AddExpressionConverters(this IServiceCollection serviceCollection) {
			return serviceCollection.AddSingleton<IExpressionConverter, BinaryExpressionConverter>()
			.AddSingleton<IExpressionConverter, ConstantExpressionConverter>()
			.AddSingleton<IExpressionConverter, IdentifierExpressionConverter>()
			.AddSingleton<IExpressionConverter, MemberExpressionConverter>()
			.AddSingleton<IExpressionConverter, NewExpressionConverter>()
			.AddSingleton<IExpressionConverter, ArrayExpressionConverter>()
			.AddSingleton<IExpressionConverter, MethodCallExpressionConverter>()
			.AddSingleton<IExpressionConverter, UnaryExpressionConverter>()
			.AddSingleton<IExpressionConverter, LambdaExpressionConverter>()
			.AddSingleton<IExpressionConverter, ConditionalExpressionConverter>()
			.AddSingleton<ExpressionConverterResolver>();
		}
	}
}
