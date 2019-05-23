using Darkengines.Expressions.Factories;
using Darkengines.Expressions.ModelConverters;
using Esprima;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Darkengines.Expressions {
	public class JavascriptExpressionConverter {
		protected IEnumerable<IExpressionFactory> ExpressionFactories { get; }
		protected IEnumerable<IModelConverter> ModelConverters { get; }
		protected IEnumerable<IIdentifierProvider> IdentifierProviders { get; }
		protected IDictionary<string, Expression> Identifiers { get; }

		protected IDictionary<string, Expression> MergeIdentifierProviders(IEnumerable<IIdentifierProvider> identifierProviders) {
			var mergedIdentifiers = identifierProviders.Aggregate(new Dictionary<string, Expression>(), (identifiers, identifierProvider) => {
				foreach (var pair in identifierProvider.Identifiers) {
					if (identifiers.ContainsKey(pair.Key)) {
						throw new Exception($"Identifier with key {pair.Key} is already in use.");
					} else {
						identifiers.Add(pair.Key, pair.Value);
					}
				}
				return identifiers;
			});
			return mergedIdentifiers;
		}

		public JavascriptExpressionConverter(IEnumerable<IModelConverter> modelConverters, IEnumerable<IExpressionFactory> expressionFactories, IEnumerable<IIdentifierProvider> identifierProviders) {
			ExpressionFactories = expressionFactories;
			ModelConverters = modelConverters;
			IdentifierProviders = identifierProviders;
			Identifiers = MergeIdentifierProviders(IdentifierProviders);
		}

		public object Process(string stringExpression) {
			var javaScriptParser = new JavaScriptParser(stringExpression);
			var jsExpression = javaScriptParser.ParseExpression();
			var modelConversionContext = new ModelConverterContext() { ModelConverters = ModelConverters };
			var expressionFactoryContext = new ExpressionFactoryContext() { ExpressionFactories = ExpressionFactories };
			var rootConverter = ModelConverters.FindModelConverterFor(jsExpression, modelConversionContext);
			var model = rootConverter.Convert(jsExpression, modelConversionContext);
			var expressionFactoryScope = new ExpressionFactoryScope(null, null) {
				Variables = Identifiers
			};
			var rootExpressionFactory = ExpressionFactories.FindExpressionFactoryFor(model, expressionFactoryContext, expressionFactoryScope);
			var expression = rootExpressionFactory.BuildExpression(model, expressionFactoryContext, expressionFactoryScope);
			var function = Expression.Lambda<Func<object>>(Expression.Convert(expression, typeof(object))).Compile();
			var result = function();
			return result;
		}

	}
}
