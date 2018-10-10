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
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Darkengines.Expressions.Tests {
	[TestClass]
	public class UnitTest1 {
		[TestMethod]
		public void TestMethod1() {

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
			var stopWatch = new Stopwatch();
			while (true) {
				stopWatch.Reset();
				stopWatch.Start();
				var rootExpressionFactory = expressionFactories.FindExpressionFactoryFor(model, expressionFactoryContext, expressionFactoryScope);
				stopWatch.Stop();
				var expression = rootExpressionFactory.BuildExpression(model, expressionFactoryContext, expressionFactoryScope);
				// Executing the resulting expression
				var function = Expression.Lambda<Func<object>>(Expression.Convert(expression, typeof(object))).Compile();
				var result = function();
			}
		}

		[TestMethod]
		public void TestTypescript() {
			var queryableLinqMethodInfos = typeof(Queryable).GetMethods().Where(methodInfo => methodInfo.IsDefined(typeof(ExtensionAttribute), true));
			var signatures = queryableLinqMethodInfos.Select(methodInfo => {
				var stringBuilder = new StringBuilder();
				var genericArguments = methodInfo.GetGenericArguments().Where(ga => ga.Name != "TSource").ToArray();
				var parameters = methodInfo.GetParameters().Skip(1);
				stringBuilder.Append($"public {methodInfo.Name}");
				if (genericArguments.Any()) {
					stringBuilder.Append($"<{string.Join(", ", genericArguments.Select(ga => ga.Name))}>");
				}
				stringBuilder.Append("(");
				var tsParameters = parameters.Select(parameter => {
					var typeName = parameter.ParameterType.Name;
					if (parameter.ParameterType.IsGenericType) {
						var args = parameter.ParameterType.GetGenericArguments();
						typeName = $"{new string(parameter.ParameterType.Name.TakeWhile(c => c != '`').ToArray())}<{string.Join(", ", args.Select(a => a.Name))}>";
					}
					if (parameter.ParameterType.Name.StartsWith("Expression")) {
						var funcType = parameter.ParameterType.GetGenericArguments()[0];
						var funcGenericArguments = funcType.GetGenericArguments();
						typeName = $"Expression<({string.Join(", ", funcGenericArguments.Take(funcGenericArguments.Count() - 1).Select(fga => { var name = fga.Name.Substring(1); return $"{ name[0] + name.Substring(1)}: {fga.Name}"; }))}) => {funcGenericArguments.Last().Name}>";
					}
					if (parameter.ParameterType.Name.StartsWith("Func")) {
						var funcType = parameter.ParameterType.GetGenericArguments()[0];
						var funcGenericArguments = funcType.GetGenericArguments();
						typeName = $"({string.Join(", ", funcGenericArguments.Take(funcGenericArguments.Count() - 1).Select(fga => { var name = fga.Name.Substring(1); return $"{ name[0] + name.Substring(1)}: {fga.Name}"; }))}) => {funcGenericArguments.Last().Name}";
					}
					return $"{parameter.Name}: {typeName}";
				});
				stringBuilder.Append(string.Join(", ", tsParameters));
				stringBuilder.Append(")");
				var returnTypeName = methodInfo.ReturnType.Name;
				if (methodInfo.ReturnType.IsGenericType) {
					var args = methodInfo.ReturnType.GetGenericArguments();
					returnTypeName = $"{new string(methodInfo.ReturnType.Name.TakeWhile(c => c != '`').ToArray())}<{string.Join(", ", args.Select(a => a.Name))}>";
				}
				stringBuilder.Append($": {returnTypeName}");
				stringBuilder.Append(" {}");
				return stringBuilder.ToString();
			}).ToArray();
			var all = string.Join("\n", signatures);
		}

		[TestMethod]
		public void TestMethod2() {
			var list1 = Enumerable.Range(0, 100).AsQueryable();
			var list2 = Enumerable.Range(0, 100).Select(n => (double)n).AsQueryable();
			var key1 = (Expression<Func<int, int>>)(n => n);
			var key2 = (Expression<Func<double, int>>)(n => (int)n);
			var selector = (Expression<Func<int, IEnumerable<double>, double[]>>)((outer, inners) => inners.ToArray());
			var arguments = new object[] {
				list1,
				list2,
				key1,
				key2,
				selector
			};
			var argumentTypes = new Type[] {
				list1.GetType(),
				list2.GetType(),
				key1.GetType(),
				key2.GetType(),
				selector.GetType(),
			};
			var methodInfo = ExpressionHelper.ExtractMethodInfo<IQueryable<object>, Func<IEnumerable<object>, Expression<Func<object, object>>, Expression<Func<object, object>>, Expression<Func<object, IEnumerable<object>, object>>, IQueryable<object>>>(x => x.GroupJoin).GetGenericMethodDefinition();
			var map = methodInfo.GetGenericArguments().ToDictionary(arg => arg, arg => (Type)null);
			var parameters = methodInfo.GetParameters().Select(p => p.ParameterType).ToArray();

			var result = InferGenericParameters(map, parameters, argumentTypes);

			var resolvedMethodInfo = methodInfo.MakeGenericMethod(result.Values.ToArray());
			var methodCallResult = resolvedMethodInfo.Invoke(null, arguments);
		}

		public Dictionary<Type, Type> InferGenericParameters(Dictionary<Type, Type> map, Type[] parameters, Type[] arguments) {
			var tuples = parameters.Zip(arguments, (parameter, argument) => new { Parameter = parameter, Argument = argument }).ToArray();
			foreach (var tuple in tuples) {
				var result = InferGenericArguments(map, tuple.Parameter, tuple.Argument);
			}
			return map;
		}

		public Dictionary<Type, Type> InferGenericArguments(Dictionary<Type, Type> map, Type parameter, Type argument) {
			if (map.ContainsKey(parameter)) {
				map[parameter] = argument;
			} else {
				if (parameter.IsGenericType) {
					var parameters = parameter.GetGenericArguments();
					var arguments = argument.GetGenericArguments();
					map = InferGenericParameters(map, parameters, arguments);
				}
			}
			return map;
		}
	}
}
