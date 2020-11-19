using Darkengines.Expressions.Converters;
using Darkengines.Expressions.Entities;
using Darkengines.Expressions.Rules;
using Darkengines.Expressions.Sample;
using System.Linq.Expressions;
using DarkEngines.Expressions;
using Esprima;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

		public async Task<Task> InvokeAsync(HttpContext httpContext,
			ExpressionConverterResolver expressionConverterResolver,
			IEnumerable<IExtensionMethodsProvider> extensionMethodsProviders,
			IEnumerable<IStaticMethodsProvider> staticMethodsProviders,
			IQueryProviderProvider queryProviderProvider,
			IEnumerable<IIdentifier> Identifiers,
			IEnumerable<IIdentifierProvider> identifierProviders,
			JsonSerializer jsonSerializer,
			IEnumerable<IRuleMapsProvider> ruleMapsProviders,
			IEnumerable<IRuleMap> ruleMaps,
			IEnumerable<ICustomMethodCallExpressionConverter> customMethodCallExpressionConverters,
			IIdentityProvider identityProvider,
			System.Text.Json.JsonSerializerOptions jsonSerializerOptions) {
			Esprima.Ast.Expression jsExpression = null;
			string payload = null;
			using (var reader = new StreamReader(httpContext.Request.Body)) {
				payload = await reader.ReadToEndAsync();
				var parser = new JavaScriptParser(payload, new ParserOptions() {
					Range = true
				});
				jsExpression = parser.ParseExpression();
			}

			// The context holds the converters
			// Building the expression

			var currentUser = identityProvider.GetIdentity().User;

			// The context olds the factories
			ruleMaps = ruleMaps.Union(ruleMapsProviders.SelectMany(rmp => rmp.RuleMaps));
			var expressionConvertersContext = new ExpressionConverterContext() {
				ExpressionConverterResolver = expressionConverterResolver,
				ExtensionMethods = extensionMethodsProviders.SelectMany(emp => emp.Methods),
				StaticMethods = staticMethodsProviders.SelectMany(emp => emp.Methods),
				RuleMapRegistry = new RuleMapRegistry(ruleMaps, Enumerable.Empty<IRuleMapsProvider>()),
				RuleMaps = ruleMaps,
				CustomMethodCallExpressionConverters = customMethodCallExpressionConverters,
				securityContext = new Context() { User = currentUser },
				Source = payload
			};

			var identifiers = new Dictionary<string, System.Linq.Expressions.Expression>();
			foreach (var identifierProvider in identifierProviders) {
				foreach (var identifier in identifierProvider.Identifiers) identifiers[identifier.Key] = identifier.Value;
			}
			foreach (var identifier in Identifiers) identifiers[identifier.Name] = identifier.Expression;
			// The scope olds the target type, the generic parameter map and varibales set.
			// Note that the target type is null since we cannot predict it at this stage.
			// Note that the generic parameter map is null since we cannot predict it at this stage
			var expressionConvertersScope = new ExpressionConverterScope(null, null) {
				Variables = identifiers
			};
			var expression = expressionConverterResolver.Convert(jsExpression, expressionConvertersContext, expressionConvertersScope, false);
			if (expression.Type != typeof(void) && expression.Type != typeof(Task)) {
				Func<Task<object>> asyncFunction = null;
				if (expression.Type.IsGenericType && expression.Type.GetGenericTypeDefinition() == typeof(Task<>)) {
					var f = System.Linq.Expressions.Expression.Lambda<Func<Task>>(expression).Compile();
					asyncFunction = async () => {
						var task = f();
						await task;
						return (object)((dynamic)task).Result;
					};
				} else {
					var function = System.Linq.Expressions.Expression.Lambda<Func<object>>(System.Linq.Expressions.Expression.Convert(expression, typeof(object))).Compile();
					asyncFunction = new Func<Task<object>>(() => Task.FromResult(function()));
				}
				var result = await asyncFunction();
				httpContext.Response.Headers.Add("Content-Type", "application/json");
				httpContext.Response.Headers.Add("charset", "utf8");
				var queryable = result as IQueryable<object>;
				if (queryable != null) {
					result = queryable.ToArray();
				}
				using (var streamWriter = new StreamWriter(httpContext.Response.Body)) {
					using (var jsonWriter = new JsonTextWriter(streamWriter)) {
						jsonSerializer.Serialize(jsonWriter, result);
					}
				}
			} else {
				Func<Task> asyncAction = null;
				if (expression.Type == typeof(Task)) {
					asyncAction = System.Linq.Expressions.Expression.Lambda<Func<Task>>(expression).Compile();
				} else {
					var a = System.Linq.Expressions.Expression.Lambda<Action>(expression).Compile();
					asyncAction = new Func<Task>(() => {
						a();
						return Task.CompletedTask;
					});
				}
				await asyncAction();
			}
			return Task.CompletedTask;
		}
	}
}
