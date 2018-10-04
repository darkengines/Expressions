using Darkengines.Expressions.ModelConverters.Javascript;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Darkengines.Expressions.ModelConverters {
	public static class ServiceCollectionExtensions {
		public static IServiceCollection AddModelConverters(this IServiceCollection serviceCollection) {
			return serviceCollection
			.AddSingleton<IModelConverter, BinaryExpressionJavascriptConverter>()
			.AddSingleton<IModelConverter, ConstantExpressionJavascriptConverter>()
			.AddSingleton<IModelConverter, IdentifierExpressionConverter>()
			.AddSingleton<IModelConverter, LambdaExpressionJavascriptConverter>()
			.AddSingleton<IModelConverter, UnaryExpressionJavascriptConverter>()
			.AddSingleton<IModelConverter, MemberExpressionJavascriptConverter>()
			.AddSingleton<IModelConverter, MethodCallExpressionJavascriptConverter>()
			.AddSingleton<IModelConverter, NewExpressionJavascriptConverter>();
		}
	}
}
