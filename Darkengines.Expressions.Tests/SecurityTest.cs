using Darkengines.Expressions.Factories;
using Darkengines.Expressions.ModelConverters;
using Darkengines.Expressions.Security;
using Darkengines.Expressions.Tests.Entities;
using Darkengines.Expressions.Tests.MutationVisitors;
using Darkengines.Expressions.Tests.QueryVisitor;
using Darkengines.Expressions.Tests.Rules;
using Darkengines.Expressions.Tests.Security;
using DarkEngines.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace Darkengines.Expressions.Tests {
	[TestClass]
	public class SecurityTest {
		public static readonly LoggerFactory MyLoggerFactory = new LoggerFactory(new[] { new ConsoleLoggerProvider((_, __) => true, true) });
		[TestMethod]
		public void TestSecurity() {
			var configuration = new ConfigurationBuilder()
			.AddJsonFile("appsettings.json")
			.Build();
			var serviceCollection = new ServiceCollection();
			serviceCollection.AddExpressionFactories()
			.AddSecurity()
			.AddEntityFrameworkSqlServer()
			.AddRules()
			.AddPermissionTypeBuilder()
			.AddMutationVisitors()
			.AddSingleton(context => new AnonymousTypeBuilder("anonymous", "anonymous"))
			.AddDbContext<BloggingContext>((optionBuilder) =>
				optionBuilder.UseSqlServer(configuration.GetConnectionString("default"))
				.UseLoggerFactory(MyLoggerFactory)
			)
			.AddScoped<DbContext>(sp => sp.GetService<BloggingContext>())
			.AddLinqMethodCallExpressionFactories()
			.AddModelConverters();

			var serviceProvider = serviceCollection.BuildServiceProvider();
			var permissionTypeBuilder = serviceProvider.GetService<PermissionEntityTypeBuilder>();
			var dbContext = serviceProvider.GetService<BloggingContext>();
			var ruleProvider = serviceProvider.GetService<RuleProvider>();
			var securityRuleProvider = serviceProvider.GetServices<ISecurityRuleProvider>();
			var identityProvider = serviceProvider.GetService<IIdentityProvider>();
			var serializer = new JsonSerializer() {
				PreserveReferencesHandling = PreserveReferencesHandling.Objects,
				ContractResolver = new CamelCasePropertyNamesContractResolver(),
				ReferenceLoopHandling = ReferenceLoopHandling.Serialize
			};

			var mutationVisitor = serviceProvider.GetService<MutationVisitor>();
			var currentUser = identityProvider.GetCurrentUser();

			var data = $@"{{ 
				""id"": 4,
                ""hashedPassword"": ""mamadoupassword"",
				""blogs"": [{{ 
					""posts"": [
						{{""$isDeletion"": true, ""id"": 10, ""content"": ""coincoin""}}
					]
				}}]
			}}";

			JObject jObjectUser = null;

			using (var stringReader = new StringReader(data)) {
				using (var jsonReader = new JsonTextReader(stringReader)) {
					jObjectUser = serializer.Deserialize<JObject>(jsonReader);
				}
			}

			var permissionEntity = GetPermissions(typeof(User), jObjectUser, dbContext, permissionTypeBuilder, ruleProvider, currentUser, securityRuleProvider);
			var lambda = Expression.Lambda(permissionEntity.Expression).Compile();
			var result = lambda.DynamicInvoke();

			var user = jObjectUser.ToObject(typeof(User));
			var entry = dbContext.Attach(user);
			permissionEntity.Action(entry, result);

			dbContext.SaveChanges();
		}

		[TestMethod]
		public void TestSelectSecurity() {
			var configuration = new ConfigurationBuilder()
			.AddJsonFile("appsettings.json")
			.Build();
			var serviceCollection = new ServiceCollection();
			serviceCollection.AddExpressionFactories()
			.AddSecurity()
			.AddPermissionTypeBuilder()
			.AddEntityFrameworkSqlServer()
			.AddRules()
			.AddMutationVisitors()
			.AddSingleton(context => new AnonymousTypeBuilder("anonymous", "anonymous"))
			.AddDbContext<BloggingContext>((optionBuilder) =>
				optionBuilder.UseSqlServer(configuration.GetConnectionString("default"))
				.UseLoggerFactory(MyLoggerFactory)
			)
			.AddScoped<DbContext>(sp => sp.GetService<BloggingContext>())
			.AddLinqMethodCallExpressionFactories()
			.AddModelConverters();

			var serviceProvider = serviceCollection.BuildServiceProvider();
			var ruleProvider = serviceProvider.GetService<RuleProvider>();
			var identityProvider = serviceProvider.GetService<IIdentityProvider>();
			var permissionTypeBuilder = serviceProvider.GetService<PermissionEntityTypeBuilder>();
			var user = identityProvider.GetCurrentUser();

			var dbContext = serviceProvider.GetService<BloggingContext>();

			var mamadou = dbContext.Users.Select(u => new[] { new { Permission = dbContext.Posts.First(p => p.Id == 7).Owner == user }, new { Permission = dbContext.Posts.First(p => p.Id == 7).Owner == user } }).ToArray();

			var method = ExpressionHelper.ExtractMethodInfo<DbContext, Func<IQueryable<object>>>(context => context.Query<object>).GetGenericMethodDefinition().MakeGenericMethod(dbContext.Model.FindEntityType("UserPermission").ClrType);
			var callExpression = Expression.Call(Expression.Constant(dbContext), method);
			var lambda = Expression.Lambda<Func<IQueryable>>(callExpression).Compile();
			var test = lambda();

			var visitor = new RuleQueryVisitor(ruleProvider, user);
			var query = dbContext.Users.Select(u => new { u.Id, u.DisplayName, u.HashedPassword });
			var expression = visitor.Visit(query.Expression);
			var q2 = query.Provider.CreateQuery(expression);
			var result = query.ToArray();

		}

		protected virtual Expression GetKeyFilterExpression(JObject jObject, IEntityType entityType) {
			var parameter = Expression.Parameter(entityType.ClrType);
			var keyProperties = entityType.FindPrimaryKey().Properties;
			var keyPropertyValues = keyProperties.Join(jObject.Properties(), property => property.Name, jProperty => jProperty.Name.ToPascalCase(), (property, jProperty) => new {
				Property = property,
				JProperty = jProperty
			}).ToArray();

			var predicate = keyPropertyValues.Select(keyPropertyValue =>
				Expression.Equal(
					Expression.Property(parameter, keyPropertyValue.Property.PropertyInfo),
					Expression.Constant(keyPropertyValue.JProperty.Value.ToObject(keyPropertyValue.Property.ClrType))
				)
			).Join((keyPredicate, partialPredicate) => Expression.AndAlso(keyPredicate, partialPredicate));
			return Expression.Lambda(predicate, parameter);
		}

		protected Expression BuildFindEntityQuery(Type rootType, DbContext dbContext, JObject jObject) {
			var queryMethodInfo = ExpressionHelper.ExtractMethodInfo<DbContext, Func<IQueryable<object>>>(context => context.Query<object>).GetGenericMethodDefinition();
			var queryCallExpression = Expression.Call(Expression.Constant(dbContext), queryMethodInfo.MakeGenericMethod(rootType));
			var whereMethodInfo = ExpressionHelper.ExtractMethodInfo<IQueryable<object>, Func<Expression<Func<object, bool>>, IQueryable<object>>>(context => context.Where).GetGenericMethodDefinition();
			var keyPredicateLambdaExpression = GetKeyFilterExpression(jObject, dbContext.Model.GetEntityTypes().FirstOrDefault(et => et.ClrType == rootType));
			var whereCallExpression = Expression.Call(null, whereMethodInfo.MakeGenericMethod(rootType), Expression.Constant(dbContext.GetType().GetProperties().FirstOrDefault(p => p.PropertyType == typeof(DbSet<>).MakeGenericType(rootType)).GetValue(dbContext)), keyPredicateLambdaExpression);
			return whereCallExpression;
		}

		protected (Expression Expression, Action<EntityEntry, object> Action) BuildNewPermissionEntityExpression(Type rootType, DbContext dbContext, JObject jObject, PermissionEntityTypeBuilder permissionEntityTypeBuilder, RuleProvider ruleProvider, User user, ParameterExpression subject, IEnumerable<ISecurityRuleProvider> securityRuleProviders, Permission requiredPermission) {
			var entityType = dbContext.Model.GetEntityTypes().First(et => et.ClrType == rootType);
			var rules = ruleProvider.GetRuleMapFor(rootType);
			var permissionEntityType = permissionEntityTypeBuilder.TypeMap[rootType];
			var newExpression = Expression.New(permissionEntityType);
			var key = entityType.FindPrimaryKey().Properties;

			var bindExpressions = new List<MemberBinding>();
			Action<EntityEntry, object> permissionCheckAction = null;
			if (subject != null) {
				var selfPermissionProperty = permissionEntityType.GetProperty("SelfPermission");
				var selfPermissionMemberBindingExpression = Expression.Bind(
					selfPermissionProperty,
					rules.Self.Select(rule => {
						var lambda = rule(user);
						return lambda.Body.Replace(lambda.Parameters[0], subject);
					}).Join((result, predicate) => Expression.Or(result, predicate))
				);
				bindExpressions.Add(selfPermissionMemberBindingExpression);

				var permissionProperties = entityType.GetPropertiesAndNavigations().Join(
					jObject.Properties(),
					p => p.Name,
					jp => jp.Name.ToPascalCase(),
					(p, jp) => new { Property = p, jProperty = jp, PermissionProperty = permissionEntityTypeBuilder.PermissionPropertyMap[p.PropertyInfo] }
				).ToArray();

				if (requiredPermission != Permission.Delete && permissionProperties.Any()) {
					var permissionPropertiesMemberBindingExpressions = permissionProperties.Select(tuple => {
						var propertyRules = ruleProvider.GetRulesFor(tuple.Property.PropertyInfo).ToArray();
						if (propertyRules.Any()) {
							var predicateExpression = propertyRules.Select(rule => {
								var lambda = rule(user);
								return lambda.Body.Replace(lambda.Parameters[0], subject);
							}).Join((result, predicate) => Expression.Or(result, predicate));
							return Expression.Bind(tuple.PermissionProperty, predicateExpression);
						}
						return Expression.Bind(tuple.PermissionProperty, Expression.Constant(Permission.None));

					}).ToArray();

					bindExpressions.AddRange(permissionPropertiesMemberBindingExpressions);
				}
				var requiredWriteOnSelf = permissionProperties.Any();
				permissionCheckAction = new Action<EntityEntry, object>((entry, permissionEntity) => {
					if (!((Permission)selfPermissionProperty.GetValue(permissionEntity)).HasFlag(requiredWriteOnSelf ? Permission.Read : Permission.Read | requiredPermission)) {
						throw new UnauthorizedAccessException($"You do not have permission {requiredPermission} on entity type {entry.Entity.GetType().Name} with key ({string.Join(", ", key.Select(kp => $"{kp.PropertyInfo.Name}={kp.PropertyInfo.GetValue(entry.Entity)}"))})");
					}
					if (requiredPermission != Permission.Delete) {
						if (permissionProperties.Any()) {
							foreach (var permissionProperty in permissionProperties) {
								if (!key.Any(keyProperty => keyProperty == permissionProperty.Property)) {
									if (!((Permission)permissionProperty.PermissionProperty.GetValue(permissionEntity)).HasFlag(requiredPermission)) {
										throw new UnauthorizedAccessException($"You are not allowed to modify property {permissionProperty.Property.Name} on entity type {entry.Entity.GetType().Name} with key ({string.Join(", ", key.Select(kp => $"{kp.PropertyInfo.Name}={kp.PropertyInfo.GetValue(entry.Entity)}"))})");
									}
								}
							}
						}
					} else {
						entry.State = EntityState.Deleted;
					}
				});
			}

			var properties = entityType.GetPropertiesAndNavigations().Join(
				jObject.Properties(),
				n => n.Name,
				jp => jp.Name.ToPascalCase(),
				(n, jp) => new { Navigation = n, JProperty = jp, Property = permissionEntityTypeBuilder.PropertyMap[n.PropertyInfo] }
			).ToArray();

			var navigationProperties = properties.Where(p => p.Navigation is INavigation).Select(p => new { Navigation = (INavigation)p.Navigation, p.JProperty, Property = p.Property });

			var notifyChangesAction = new Action<EntityEntry>(entry => {
				foreach (var property in properties.Where(p => !key.Any(keyProperty => keyProperty == p.Navigation))) {
					if (property.Navigation is INavigation) {
						if (((INavigation)property.Navigation).IsCollection()) {
							entry.Collection(property.Navigation.Name).IsModified = true;
						} else {
							entry.Reference(property.Navigation.Name).IsModified = true;
						}
					} else {
						entry.Property(property.Navigation.Name).IsModified = true;
					}
				}
			});

			Action<EntityEntry, object> propertiesInspectorAction = null;
			Action<EntityEntry, object>[] collectionActions = null;
			if (requiredPermission != Permission.Delete) {

				var collectionProperties = navigationProperties.Where(np => np.Navigation.IsCollection()).ToArray();
				var scalarProperties = navigationProperties.Where(np => !np.Navigation.IsCollection()).ToArray();

				var propertiesMemberBindingExpressions = scalarProperties.Select(tuple => {
					var permissionEntityTuple = GetPermissions(
						tuple.Navigation.ClrType,
						tuple.JProperty.Value<JObject>(),
						dbContext,
						permissionEntityTypeBuilder,
						ruleProvider,
						user,
						securityRuleProviders
					);
					return (Binding: Expression.Bind(tuple.Property, permissionEntityTuple.Item1), Action: permissionEntityTuple.Item2, Properties: tuple);
				}).ToArray();

				propertiesInspectorAction = new Action<EntityEntry, object>((entry, permissionEntity) => {
					notifyChangesAction(entry);
					foreach (var t in propertiesMemberBindingExpressions) {
						t.Action(entry.Navigation(t.Properties.Navigation.Name).EntityEntry, t.Properties.Property.GetValue(permissionEntity));
					}
				});

				var collectionPropertiesMemberBindingExpressions = collectionProperties.Select(tuple => {
					var arrayValues = tuple.JProperty.Value.Select((value, index) => {
						var tuples = GetPermissions(
							tuple.Navigation.ClrType.GetEnumerableUnderlyingType(),
							(JObject)value,
							dbContext,
							permissionEntityTypeBuilder,
							ruleProvider,
							user,
							securityRuleProviders
						);
						return (Expression: tuples.Item1, Action: tuples.Item2, Properties: tuple, Index: index);
					}).ToArray();
					var binding = Expression.Bind(tuple.Property, Expression.NewArrayInit(tuple.Property.PropertyType.GetElementType(), arrayValues.Select(t => t.Expression)));
					var action = new Action<EntityEntry, object>((entry, permissionEntity) => {
						foreach (var t in arrayValues) {
							t.Action(dbContext.Entry((((IEnumerable<object>)entry.Collection(t.Properties.Navigation.Name).CurrentValue).ElementAt(t.Index))), ((object[])t.Properties.Property.GetValue(permissionEntity))[t.Index]);
						}
					});
					return (Binding: binding, Action: action);
				}).ToArray();
				collectionActions = collectionPropertiesMemberBindingExpressions.Select(cpmbe => cpmbe.Action).ToArray();
				bindExpressions.AddRange(propertiesMemberBindingExpressions.Select(t => t.Binding));
				bindExpressions.AddRange(collectionPropertiesMemberBindingExpressions.Select(t => t.Binding));
			}
			var inspector = new Action<EntityEntry, object>((entry, permission) => {
				permissionCheckAction?.Invoke(entry, permission);
				propertiesInspectorAction?.Invoke(entry, permission);
				if (collectionActions != null) {
					foreach (var collectionAction in collectionActions) {
						collectionAction(entry, permission);
					}
				}
			});

			var initExpression = Expression.MemberInit(newExpression, bindExpressions);
			return (initExpression, inspector);
		}

		protected (Expression Expression, Action<EntityEntry, object> Action) BuildSelectPermissionEntityExpression(Type rootType, Type permissionType, DbContext dbContext, JObject jObject, PermissionEntityTypeBuilder permissionEntityTypeBuilder, RuleProvider ruleProvider, User user, IEnumerable<ISecurityRuleProvider> securityRuleProviders, Permission requiredPermission) {
			var SelectMethodInfo = ExpressionHelper.ExtractMethodInfo<IQueryable<object>, Func<Expression<Func<object, object>>, IQueryable<object>>>(queryable => queryable.Select).GetGenericMethodDefinition().MakeGenericMethod(rootType, permissionType);
			var firstOrDefaultMethodInfo = ExpressionHelper.ExtractMethodInfo<IQueryable<object>, Func<object>>(queryable => queryable.FirstOrDefault).GetGenericMethodDefinition().MakeGenericMethod(permissionType);
			var findEntityExpression = BuildFindEntityQuery(rootType, dbContext, jObject);
			var parameterExpression = Expression.Parameter(rootType);
			var newPermissionEntityExpression = BuildNewPermissionEntityExpression(
				rootType,
				dbContext,
				jObject,
				permissionEntityTypeBuilder,
				ruleProvider,
				user,
				parameterExpression,
				securityRuleProviders,
				requiredPermission
			);
			var selectLambdaExpression = Expression.Lambda(newPermissionEntityExpression.Expression, parameterExpression);
			var selectCallExpression = Expression.Call(SelectMethodInfo, findEntityExpression, selectLambdaExpression);
			var firstOrDefaultCallExpression = Expression.Call(null, firstOrDefaultMethodInfo, selectCallExpression);

			return (firstOrDefaultCallExpression, newPermissionEntityExpression.Action);
		}

		protected (Expression Expression, Action<EntityEntry, object> Action) GetPermissions(Type entityType, JObject jObject, DbContext context, PermissionEntityTypeBuilder permissionEntityTypeBuilder, RuleProvider ruleProvider, User user, IEnumerable<ISecurityRuleProvider> securityRuleProviders) {
			var securityProvider = securityRuleProviders.FirstOrDefault(securityRuleProvider => securityRuleProvider.CanHandle(entityType));
			var entityInfos = context.Model.FindRuntimeEntityType(entityType);
			var jObjectProperties = jObject.Properties();
			var entityPropertyInfos = entityType.GetProperties();
			var entityProperties = entityInfos.GetProperties();
			var tuples = entityProperties.Join(jObjectProperties, entityProperty => entityProperty.Name, jProperty => jProperty.Name.ToPascalCase(), (property, jProperty) => new {
				Property = property,
				JProperty = jProperty
			});

			var primaryKey = entityInfos.FindPrimaryKey();
			var primaryKeyTuples = primaryKey.Properties.Join(
				jObjectProperties,
				property => property.Name,
				jProperty => jProperty.Name.ToPascalCase(),
				(property, jProperty) => new { Property = property, JProperty = jProperty }
			).ToArray();
			var isPrimaryKeySet = primaryKeyTuples.Length == primaryKey.Properties.Count() && primaryKeyTuples.All(tuple => {
				var jPropertyValue = tuple.JProperty.ToObject(tuple.Property.ClrType);
				return jPropertyValue != null && jPropertyValue != Activator.CreateInstance(tuple.Property.ClrType);
			});
			var isDeletion = jObjectProperties.Any(jProperty => jProperty.Name == "$isDeletion" && jProperty.Value.Value<bool>());

			SecurityReport report = null;

			if (!isDeletion) {
				if (!isPrimaryKeySet) {
					// if primary key is not set, consider add
					// check permission to add
					var canAdd = securityProvider.GetOperationPermission().HasFlag(Permission.Write);
					if (canAdd) {
						var permissionEntityExpression = BuildNewPermissionEntityExpression(
							entityType,
							context,
							jObject,
							permissionEntityTypeBuilder,
							ruleProvider,
							user,
							null,
							securityRuleProviders,
							Permission.None
						);
						return permissionEntityExpression;
					}
				} else {
					// if primary key is set, consider update
					// check permission to update
					var canUpdate = securityProvider.GetOperationPermission().HasFlag(Permission.Write);
					if (canUpdate) {
						// check permission to access
						var selectExpression = BuildSelectPermissionEntityExpression(
							entityType,
							permissionEntityTypeBuilder.TypeMap[entityType],
							context,
							jObject,
							permissionEntityTypeBuilder,
							ruleProvider,
							user,
							securityRuleProviders,
							Permission.Write
						);
						return selectExpression;
					}
				}
			} else {
				// if delete tag is set, consider delete
				var canDelete = securityProvider.GetOperationPermission().HasFlag(Permission.Write);
				// check permission to delete
				if (canDelete) {
					// check permission to access
					var selectExpression = BuildSelectPermissionEntityExpression(
						entityType,
						permissionEntityTypeBuilder.TypeMap[entityType],
						context,
						jObject,
						permissionEntityTypeBuilder,
						ruleProvider,
						user,
						securityRuleProviders,
						Permission.Delete
					);
					return selectExpression;
				}
			}
			return (null, null);
		}
	}
}
