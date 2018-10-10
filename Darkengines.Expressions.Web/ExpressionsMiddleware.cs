using Darkengines.Expressions.Factories;
using Darkengines.Expressions.ModelConverters;
using Darkengines.Expressions.Web.Entities;
using Esprima;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using NJsonSchema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Darkengines.Expressions.Web {
	public class ExpressionsMiddleware {
		private readonly RequestDelegate _next;

		public ExpressionsMiddleware(RequestDelegate next) {
			_next = next;
		}

		public async Task InvokeAsync(HttpContext context, BloggingContext bloggingContext, IEnumerable<IModelConverter> modelConverters, IEnumerable<IExpressionFactory> expressionFactories) {
			string query = null;
			using (var reader = new StreamReader(context.Request.Body)) {
				query = await reader.ReadToEndAsync();
			}
			// Parsing Ecmascript
			var parser = new JavaScriptParser(query);
			var jsExpression = parser.ParseExpression();

			var modelConversionContext = new ModelConverterContext() { ModelConverters = modelConverters };
			var rootConverter = modelConverters.FindModelConverterFor(jsExpression, modelConversionContext);
			var model = rootConverter.Convert(jsExpression, modelConversionContext);

			// Building the expression

			// The context olds the factories
			var expressionFactoryContext = new ExpressionFactoryContext() {
				ExpressionFactories = expressionFactories
			};

			var dbSetProperties = bloggingContext.GetType().GetProperties().Where(property => property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>)).ToArray();
			var types = dbSetProperties.Select(dbSetProperty => dbSetProperty.PropertyType.GetGenericArguments()[0]).ToArray();

			var schemasTasks = types.ToDictionary(type => type.Name, async type => await JsonSchema4.FromTypeAsync(type));
			var schemas = schemasTasks.ToDictionary(kp => kp.Key, kp => kp.Value.Result);

			var expressionFactoryScope = new ExpressionFactoryScope(null, null) {
				Variables = new Dictionary<string, Expression>() {
					{ nameof(bloggingContext.Users), Expression.Constant(bloggingContext.Users) },
					{ nameof(bloggingContext.Blogs), Expression.Constant(bloggingContext.Blogs) },
					{ nameof(bloggingContext.Posts), Expression.Constant(bloggingContext.Posts) },
					{ nameof(bloggingContext.Comments), Expression.Constant(bloggingContext.Comments) },
					{ "Schemas", Expression.Constant(schemas) },
				}
			};
			var rootExpressionFactory = expressionFactories.FindExpressionFactoryFor(model, expressionFactoryContext, expressionFactoryScope);
			var expression = rootExpressionFactory.BuildExpression(model, expressionFactoryContext, expressionFactoryScope);
			// Executing the resulting expression
			var function = Expression.Lambda<Func<object>>(Expression.Convert(expression, typeof(object))).Compile();
			var result = function();
			context.Response.ContentType = "application/json";
			context.Response.StatusCode = 200;
			var writer = new StreamWriter(context.Response.Body, Encoding.UTF8);
			var serializer = new JsonSerializer() {
				Formatting = Formatting.Indented,
				ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
				PreserveReferencesHandling = PreserveReferencesHandling.All
			};
			serializer.Serialize(writer, result);
			await writer.FlushAsync();
			//await _next(context);
		}
	}
}
