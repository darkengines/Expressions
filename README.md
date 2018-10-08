# Expressions
Provide a way to execute expressions on the server side from the client side.

```C#
// Create a service collection and add the model converters and the expression factories.
// AddLinqMethodCallExpressionFactories is optional, it just adds IQueryable and IEnumerable generic extension methods.
var serviceCollection = new ServiceCollection();
serviceCollection.AddExpressionFactories()
.AddLinqMethodCallExpressionFactories()
.AddModelConverters();

var serviceProvider = serviceCollection.BuildServiceProvider();

// This line of code will be executed on the server side.
var code = "Integers.Skip(10).Take(20).GroupJoin(Decimals, n => n, d => d, (n, ds) => ds).SelectMany(x => x).Select(x => x*2).Select(x => ({a: x})).Aggregate(0, (sum, x) => sum + x.a)";

// Parsing Ecmascript
var parser = new JavaScriptParser(code);
var jsExpression = parser.ParseExpression();

// Converting parsed Ecmascript expression to expression model
var modelConverters = serviceProvider.GetServices<IModelConverter>();

// The context olds the converters
var context = new ModelConverterContext() { ModelConverters = modelConverters };
var rootConverter = modelConverters.FindModelConverterFor(jsExpression, context);
var model = rootConverter.Convert(jsExpression, context);

// Building the expression
var expressionFactories = serviceProvider.GetServices<IExpressionFactory>();

// The context olds the factories
var expressionFactoryContext = new ExpressionFactoryContext() {
	ExpressionFactories = expressionFactories
};

// The scope olds the target type, the generic parameter map and varibales set.
// Note that the target type is null since we cannot predict it at this stage.
// Note that the generic parameter map is null since we cannot predict it at this stage
var expressionFactoryScope = new ExpressionFactoryScope(null, null) {
	Variables = new Dictionary<string, Expression>() {
		{ "Integers", Expression.Constant(Enumerable.Range(0, 100).AsEnumerable()) },
		{ "Decimals", Expression.Constant(Enumerable.Range(0, 100).Select(n => (decimal)n).AsQueryable()) }
	}
};
var rootExpressionFactory = expressionFactories.FindExpressionFactoryFor(model, expressionFactoryContext, expressionFactoryScope);
var expression = rootExpressionFactory.BuildExpression(model, expressionFactoryContext, expressionFactoryScope);

// Executing the resulting expression
var function = Expression.Lambda<Func<object>>(Expression.Convert(expression, typeof(object))).Compile();
var result = function();
```
