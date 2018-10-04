using Darkengines.Expressions.Factories;
using Darkengines.Expressions.ModelConverters;
using Esprima;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Darkengines.Expressions.Tests {
	[TestClass]
	public class UnitTest1 {
		[TestMethod]
		public void TestMethod1() {
			var serviceCollection = new ServiceCollection();
			serviceCollection.AddExpressionFactories()
			.AddModelConverters();

			var serviceProvider = serviceCollection.BuildServiceProvider();

			var code = "{a: 1}";
			var parser = new JavaScriptParser(code);
			var jsExpression = parser.ParseExpression();

			var modelConverters = serviceProvider.GetServices<IModelConverter>();
			var context = new ModelConverterContext() { ModelConverters = modelConverters };
			var rootConverter = modelConverters.FindModelConverterFor(jsExpression, context);
			var model = rootConverter.Convert(jsExpression, context);

			var expressionFactories = serviceProvider.GetServices<IExpressionFactory>();
			var expressionFactoryContext = new ExpressionFactoryContext() {
				Scope = new System.Collections.Generic.Dictionary<string, Expression>() {
					{ "Numbers", Expression.Constant(Enumerable.Range(0, 100).ToArray()) }
				},
				ExpressionFactories = expressionFactories
			};
			var expressionFactoryScope = new ExpressionFactoryScope(null, null);
			var rootExpressionFactory = expressionFactories.FindExpressionFactoryFor(model, expressionFactoryContext, expressionFactoryScope);

			var expression = rootExpressionFactory.BuildExpression(model, expressionFactoryContext, expressionFactoryScope);

			var function = Expression.Lambda<Func<object>>(Expression.Convert(expression, typeof(object))).Compile();
			var result = function();
		}
	}
}
