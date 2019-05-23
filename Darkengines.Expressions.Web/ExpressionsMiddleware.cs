using Darkengines.Expressions.Entities;
using Darkengines.Expressions.Factories;
using Darkengines.Expressions.ModelConverters;
using Darkengines.Expressions.Web.Entities;
using DarkEngines.Expressions;
using Esprima;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using NJsonSchema;
using NJsonSchema.CodeGeneration.TypeScript;
using NJsonSchema.Generation;
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

		protected EntityInfo BuildEntityInfo(IEntityType entityType, HashSet<IEntityType> cache = null) {
			if (cache == null) cache = new HashSet<IEntityType>();
			cache.Add(entityType);
			var navigations = entityType.GetNavigations().Select(navigation => {
				return new Navigation() {
					PropertyName = navigation.Name,
					InversePropertyName = navigation.FindInverse()?.Name
				};
			}).ToArray();
			var key = entityType.FindPrimaryKey().Properties.Select(property => property.Name).ToArray();
			var foreignKeys = entityType.GetForeignKeys().Select(fk => {
				return new ForeignKey {
					PrincipalToDependent = fk.PrincipalToDependent?.Name,
					Properties = fk.Properties.Select(p => p.Name).ToArray(),
					Dependent = fk.DeclaringEntityType.ClrType.Name,
					Principal = fk.PrincipalEntityType.ClrType.Name,
				};
			}).ToArray();
			return new EntityInfo {
				Name = entityType.ClrType.Name,
				ForeignKeys = foreignKeys,
				Key = key,
				Navigations = navigations.ToDictionary(n => n.PropertyName, n => n)
			};
		}

		public async Task InvokeAsync(HttpContext context, BloggingContext bloggingContext, IEnumerable<IModelConverter> modelConverters, IEnumerable<IExpressionFactory> expressionFactories, AnonymousTypeBuilder anonymousTypeBuilder) {
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

			// The context holds the factories
			var expressionFactoryContext = new ExpressionFactoryContext() {
				ExpressionFactories = expressionFactories
			};

			var dbSetProperties = bloggingContext.GetType().GetProperties().Where(property => property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>)).ToArray();
			var types = dbSetProperties.Select(dbSetProperty => dbSetProperty.PropertyType.GetGenericArguments()[0]).ToArray();

			var rootTypeShape = new HashSet<Tuple<Type, string>>( types.Select(t => new Tuple<Type, string>(t, t.Name)).ToArray());
			var rootType = anonymousTypeBuilder.BuildAnonymousType(rootTypeShape);

			var generator = new JSchemaGenerator() {
				SchemaIdGenerationHandling = SchemaIdGenerationHandling.TypeName,
				SchemaReferenceHandling = SchemaReferenceHandling.All
			};
			var rootTypeSchema = generator.Generate(rootType);
			
			var schema = await JsonSchema4.FromTypeAsync(rootType);
			
			var schemas = types.ToDictionary(type => type.Name, type => generator.Generate(type, true));
			var contractResolver = JsonSchema4.CreateJsonSerializerContractResolver(SchemaType.JsonSchema);
			JsonSchemaReferenceUtilities.UpdateSchemaReferencePaths(schema, false, contractResolver);

			var entityInfos = types.Join(bloggingContext.Model.GetEntityTypes(), clrType => clrType, et => et.ClrType, (clrType, entityType) => new { ClrType = clrType, EntityType = entityType })
			.Select(map => {
				return BuildEntityInfo(map.EntityType);
			}).ToArray();

			var tsGenerator = new TypeScriptGenerator(schema);
			var ts = tsGenerator.GenerateFile();

			var expressionFactoryScope = new ExpressionFactoryScope(null, null) {
				Variables = new Dictionary<string, Expression>() {
					{ nameof(bloggingContext.Users), Expression.Constant(bloggingContext.Users) },
					{ nameof(bloggingContext.Blogs), Expression.Constant(bloggingContext.Blogs) },
					{ nameof(bloggingContext.Posts), Expression.Constant(bloggingContext.Posts) },
					{ nameof(bloggingContext.Comments), Expression.Constant(bloggingContext.Comments) },
					{ "Schemas", Expression.Constant(schemas) },
					{ "RootSchema", Expression.Constant(schema) },
					{ "JsonRootSchema", Expression.Constant(schema.ToJson()) },
					{ "EntityInfos", Expression.Constant(entityInfos.ToDictionary(ei => ei.Name, ei => ei)) }
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
			if (expression.Type == typeof(JsonSchema4)) {
				await writer.WriteAsync(((JsonSchema4)result).ToJson());
			} else {
				var serializer = new JsonSerializer() {
					Formatting = Formatting.Indented,
					ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
					PreserveReferencesHandling = PreserveReferencesHandling.All
				};
				serializer.Serialize(writer, result);
			}
			await writer.FlushAsync();
			//await _next(context);
		}
	}
}
