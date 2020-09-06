using Darkengines.Expressions.Rules;
using Darkengines.Expressions.Security;
using DarkEngines.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Darkengines.Expressions {
	public class JObjectMutationHelper {
		protected IEnumerable<IRuleMap> RuleMaps { get; }
		protected IQueryProviderProvider QueryProviderProvider { get; }
		protected IModel Model { get; }
		protected PermissionResolverExpressionBuilderProvider PermissionResolverExpressionBuilderProvider { get; }
		protected DefaultPermissionResolverExpressionBuilder DefaultPermissionResolverExpressionBuilder { get; set; } = new DefaultPermissionResolverExpressionBuilder();
		protected PermissionEntityTypeBuilder PermissionEntityTypeBuilder { get; }
		protected MethodInfo QueryMethodInfo { get; } = ExpressionHelper.ExtractMethodInfo<DbContext, Func<IQueryable<object>>>(context => context.Query<object>).GetGenericMethodDefinition();
		protected MethodInfo WhereMethodInfo { get; } = ExpressionHelper.ExtractMethodInfo<IQueryable<object>, Func<Expression<Func<object, bool>>, IQueryable<object>>>(context => context.Where).GetGenericMethodDefinition();
		protected MethodInfo SelectMethodInfo { get; } = ExpressionHelper.ExtractMethodInfo<IQueryable<object>, Func<Expression<Func<object, object>>, IQueryable<object>>>(queryable => queryable.Select).GetGenericMethodDefinition();
		protected MethodInfo FirstOrDefaultMethodInfo { get; } = ExpressionHelper.ExtractMethodInfo<IQueryable<object>, Func<object>>(queryable => queryable.FirstOrDefault).GetGenericMethodDefinition();
		public JObjectMutationHelper(
			IEnumerable<IRuleMap> ruleMaps,
			IQueryProviderProvider queryProviderProvider,
			IModelProvider modelProvider,
			PermissionResolverExpressionBuilderProvider permissionResolverExpressionBuilderProvider,
			PermissionEntityTypeBuilder permissionEntityTypeBuilder
		) {
			RuleMaps = ruleMaps;
			QueryProviderProvider = queryProviderProvider;
			Model = modelProvider.Model;
			PermissionResolverExpressionBuilderProvider = permissionResolverExpressionBuilderProvider;
			PermissionEntityTypeBuilder = permissionEntityTypeBuilder;
		}

		public MutationContext Inspect(Type type, JObject jObject, object permissionResolverContext) {
			var entityType = Model.FindRuntimeEntityType(type);
			var ruleMap = RuleMaps.FirstOrDefault(rm => rm.CanHandle(type));
			var jObjectProperties = jObject.Properties();
			var entityPropertyInfos = type.GetProperties();
			var entityProperties = entityType.GetProperties();
			var tuples = entityProperties.Join(jObjectProperties, entityProperty => entityProperty.Name, jProperty => jProperty.Name.ToPascalCase(), (property, jProperty) => new {
				Property = property,
				JProperty = jProperty
			});

			var primaryKey = entityType.FindPrimaryKey();
			var primaryKeyTuples = primaryKey.Properties.Select(property => {
				var jProperty = jObjectProperties.FirstOrDefault(jp => $"@{property.Name.ToCamelCase()}" == jp.Name) ?? jObjectProperties.FirstOrDefault(jp => property.Name == jp.Name.ToPascalCase());
				return new { Property = property, JProperty = jProperty };
			}).Where(t => t.JProperty != null).ToArray();
			var primaryKeyPairs = primaryKeyTuples.ToDictionary(tuple => tuple.Property, tuple => tuple.JProperty.Value.ToObject(tuple.Property.ClrType));
			var isPrimaryKeySet = primaryKeyPairs.Count == primaryKey.Properties.Count;
			var isDeletion = jObjectProperties.Any(jProperty => jProperty.Name == "$isDeletion" && jProperty.Value.Value<bool>());
			var isInsertion = jObjectProperties.Any(jProperty => jProperty.Name == "$isInsertion" && jProperty.Value.Value<bool>());

			var permissionType = PermissionEntityTypeBuilder.TypeMap[entityType];
			var selfPermissionProperty = PermissionEntityTypeBuilder.TypeMap[entityType].GetProperty("SelfPermission");
			var propertiesTuples = entityType.GetProperties().Join(
				jObjectProperties,
				p => p.Name,
				jp => jp.Name.ToPascalCase(),
				(p, jp) => new PropertyTuple(
					p,
					jp,
					PermissionEntityTypeBuilder.PermissionPropertyMap[p],
					PermissionEntityTypeBuilder.PropertyMap[p],
					PermissionResolverExpressionBuilderProvider.GetPermissionResolverExpressionBuilderFor(type, p.PropertyInfo) ?? DefaultPermissionResolverExpressionBuilder
				)
			).ToArray();

			var navigationTuples = entityType.GetNavigations().Join(
				jObjectProperties,
				p => p.Name,
				jp => jp.Name.ToPascalCase(),
				(p, jp) => new NavigationTuple(
					p,
					jp,
					PermissionEntityTypeBuilder.PermissionPropertyMap[p],
					PermissionEntityTypeBuilder.PropertyMap[p],
					PermissionResolverExpressionBuilderProvider.GetPermissionResolverExpressionBuilderFor(type, p.PropertyInfo) ?? DefaultPermissionResolverExpressionBuilder
				)
			).ToArray();

			var allPropertiesTuples = propertiesTuples.Concat(navigationTuples.Select(np => new PropertyTuple(
				np.Property,
				np.JProperty,
				np.PermissionPropertyInfo,
				np.PermissionNavigationPropertyInfo,
				np.PermissionResolverExpressionBuilder
			))).ToArray();
			var allPropertiesTuplesButKey = allPropertiesTuples.Where(pt => {
				var result = !primaryKey.Properties.Any(pk => pk == pt.Property);
				//var result = !(pt.Property is IProperty && ((IProperty)pt.Property).ValueGenerated > 0);
				return result;
			}).ToArray();
			var collectionNavigationTuples = navigationTuples.Where(t => t.Property.IsCollection()).ToArray();
			var scalarNavigationTuples = navigationTuples.Where(t => !t.Property.IsCollection()).ToArray();
			MutationContext context = null;

			if (!isInsertion && isPrimaryKeySet) {
				var findEntityExpression = BuildFindEntityQuery(entityType, QueryProviderProvider, jObject, primaryKeyPairs);
				var entityParameterExpression = Expression.Parameter(entityType.ClrType);
				var selfPermissionMemberBindingExpression = BuildSelfPermissionBinding(entityType, entityParameterExpression, selfPermissionProperty, permissionResolverContext);
				var memberBindingExpressions = new List<MemberBinding>() { selfPermissionMemberBindingExpression };
				Action<object> inspector = null;
				Action<EntityEntry> entryAction = null;
				Action<object> postAction = null;
				if (isDeletion) {
					//DELETE
					inspector = new Action<object>(permissionEntity => {
						var permission = (Permission)selfPermissionProperty.GetValue(permissionEntity);
						if (!permission.HasFlag(Permission.Delete)) throw new UnauthorizedAccessException($"You do not have permission to delete on entity type {entityType.Name} with key ({string.Join(", ", primaryKeyPairs.Select(kp => $"{kp.Key.Name}={kp.Value}"))})");
					});
					entryAction = new Action<EntityEntry>(entry => entry.State = EntityState.Deleted);
					postAction = new Action<object>(entity => { });
				} else {
					if (allPropertiesTuplesButKey.Any()) {
						//UPDATE
						var permissionPropertiesMemberBindingExpressions = BuildPropertiesPermissionBindings(entityParameterExpression, allPropertiesTuples, permissionResolverContext);
						var scalarNavigationPropertiesMemberBindingExpressions = BuildScalarNavigationBindings(scalarNavigationTuples, permissionResolverContext);
						var collectionNavigationPropertiesMemberBindingExpressions = BuildCollectionNavigationBindings(collectionNavigationTuples, permissionResolverContext);
						memberBindingExpressions.AddRange(permissionPropertiesMemberBindingExpressions);
						memberBindingExpressions.AddRange(scalarNavigationPropertiesMemberBindingExpressions.Select(r => r.MemberBinding));
						memberBindingExpressions.AddRange(collectionNavigationPropertiesMemberBindingExpressions.Select(r => r.MemberBinding));
						var selfPermissionCheck = new Action<object>(permissionEntity => {
							var permission = (Permission)selfPermissionProperty.GetValue(permissionEntity);
							if (!permission.HasFlag(Permission.Read | Permission.Write)) throw new UnauthorizedAccessException($"You do not have permission to modify on entity type {entityType.Name} with key ({string.Join(", ", primaryKeyPairs.Select(kp => $"{kp.Key.Name}={kp.Value}"))})");
						});
						inspector = new Action<object>(permissionEntity => {
							if (allPropertiesTuplesButKey.Any()) {
								foreach (var propertyTuple in allPropertiesTuplesButKey) {
									if (!((Permission)propertyTuple.PermissionPropertyInfo.GetValue(permissionEntity)).HasFlag(Permission.Write)) {
										throw new UnauthorizedAccessException($"You are not allowed to modify property {propertyTuple.Property.Name} on entity type {propertyTuple.Property.DeclaringType.Name} with key ({string.Join(", ", primaryKeyPairs.Select(kp => $"{kp.Key.PropertyInfo.Name}={kp.Value}"))})");
									}
								}
							}
							foreach (var scalarNavigationPropertiesMemberBindingExpression in scalarNavigationPropertiesMemberBindingExpressions) scalarNavigationPropertiesMemberBindingExpression.Inspector(permissionEntity);
							foreach (var collectionNavigationPropertiesMemberBindingExpression in collectionNavigationPropertiesMemberBindingExpressions) collectionNavigationPropertiesMemberBindingExpression.Inspector(permissionEntity);
						});
						entryAction = new Action<EntityEntry>(entry => {
							//entry.State = EntityState.Modified;
							foreach (var tuple in allPropertiesTuplesButKey) {
								var navigation = tuple.Property as INavigation;
								if (navigation != null) {
									if (navigation.IsCollection()) {
										entry.Collection(navigation.Name).IsModified = true;
									} else {
										entry.Reference(navigation.Name).IsModified = true;
									}
								} else {
									entry.Property(tuple.Property.Name).IsModified = true;
								}
							}
							foreach (var scalarNavigationPropertiesMemberBindingExpression in scalarNavigationPropertiesMemberBindingExpressions) scalarNavigationPropertiesMemberBindingExpression.EntryAction(entry);
							foreach (var collectionNavigationPropertiesMemberBindingExpression in collectionNavigationPropertiesMemberBindingExpressions) collectionNavigationPropertiesMemberBindingExpression.EntryAction(entry);
						});
						postAction = new Action<object>(entity => {
							foreach (var scalarNavigationPropertiesMemberBindingExpression in scalarNavigationPropertiesMemberBindingExpressions) scalarNavigationPropertiesMemberBindingExpression.PostAction(entity);
							foreach (var collectionNavigationPropertiesMemberBindingExpression in collectionNavigationPropertiesMemberBindingExpressions) collectionNavigationPropertiesMemberBindingExpression.PostAction(entity);
						});
					} else {
						//READ
						inspector = new Action<object>(permissionEntity => {
							var permission = (Permission)selfPermissionProperty.GetValue(permissionEntity);
							if (!permission.HasFlag(Permission.Read)) throw new UnauthorizedAccessException($"You do not have permission to read on entity type {entityType.Name} with key ({string.Join(", ", primaryKeyPairs.Select(kp => $"{kp.Key.Name}={kp.Value}"))})");
						});
						entryAction = new Action<EntityEntry>(entry => {
							if (entry != null) {
								entry.State = EntityState.Unchanged;
							}
						});
						postAction = new Action<object>(entry => {
						});
					}
				}
				var newPermissionExpression = BuildNewPermissionMemberInitExpression(permissionType, memberBindingExpressions.ToArray());
				var permissionExpression = BuildSelectFirstOrDefaultPermissionEntityExpression(entityType, jObject, findEntityExpression, entityParameterExpression, newPermissionExpression);
				context = new MutationContext() {
					PermissionEntityExpression = permissionExpression,
					Inspector = inspector,
					EntryAction = entryAction,
					PostAction = postAction
				};
			} else {
				//CREATION
				var memberBindingExpressions = new List<MemberBinding>();
				var scalarNavigationPropertiesMemberBindingExpressions = BuildScalarNavigationBindings(scalarNavigationTuples, permissionResolverContext);
				var collectionNavigationPropertiesMemberBindingExpressions = BuildCollectionNavigationBindings(collectionNavigationTuples, permissionResolverContext);
				memberBindingExpressions.AddRange(scalarNavigationPropertiesMemberBindingExpressions.Select(r => r.MemberBinding));
				memberBindingExpressions.AddRange(collectionNavigationPropertiesMemberBindingExpressions.Select(r => r.MemberBinding));
				var newPermissionExpression = BuildNewPermissionMemberInitExpression(permissionType, memberBindingExpressions.ToArray());
				var propertiesPermissionCheck = new Action<object>(permissionEntity => {
					foreach (var scalarNavigationPropertiesMemberBindingExpression in scalarNavigationPropertiesMemberBindingExpressions) scalarNavigationPropertiesMemberBindingExpression.Inspector(permissionEntity);
					foreach (var collectionNavigationPropertiesMemberBindingExpression in collectionNavigationPropertiesMemberBindingExpressions) collectionNavigationPropertiesMemberBindingExpression.Inspector(permissionEntity);
				});
				var entryAction = new Action<EntityEntry>(entry => {
					entry.State = EntityState.Added;
					foreach (var scalarNavigationPropertiesMemberBindingExpression in scalarNavigationPropertiesMemberBindingExpressions) scalarNavigationPropertiesMemberBindingExpression.EntryAction(entry);
					foreach (var collectionNavigationPropertiesMemberBindingExpression in collectionNavigationPropertiesMemberBindingExpressions) collectionNavigationPropertiesMemberBindingExpression.EntryAction(entry);
				});
				var postAction = new Action<object>(entity => {
					if (ruleMap != null && ruleMap.InsertedAction != null) ruleMap.InsertedAction(permissionResolverContext, entity);
					foreach (var scalarNavigationPropertiesMemberBindingExpression in scalarNavigationPropertiesMemberBindingExpressions) scalarNavigationPropertiesMemberBindingExpression.PostAction(entity);
					foreach (var collectionNavigationPropertiesMemberBindingExpression in collectionNavigationPropertiesMemberBindingExpressions) collectionNavigationPropertiesMemberBindingExpression.PostAction(entity);
				});
				context = new MutationContext() {
					PermissionEntityExpression = newPermissionExpression,
					Inspector = propertiesPermissionCheck,
					EntryAction = entryAction,
					PostAction = postAction
				};
			}
			return context;
		}

		protected Expression GetKeyFilterExpression(
			JObject jObject,
			IEntityType entityType,
			IDictionary<IProperty, object> keyPairs
		) {
			var parameter = Expression.Parameter(entityType.ClrType);
			var predicate = keyPairs.Select(keyPair =>
				Expression.Equal(
					Expression.Property(parameter, keyPair.Key.PropertyInfo),
					Expression.Constant(keyPair.Value)
				)
			).Join((keyPredicate, partialPredicate) => Expression.AndAlso(keyPredicate, partialPredicate));
			return Expression.Lambda(predicate, parameter);
		}

		protected Expression BuildFindEntityQuery(IEntityType entityType, IQueryProviderProvider queryProvider, JObject jObject, IDictionary<IProperty, object> keyPairs) {
			var query = queryProvider.GetQuery(entityType.ClrType);
			var keyPredicateLambdaExpression = GetKeyFilterExpression(jObject, entityType, keyPairs);
			var whereCallExpression = Expression.Call(null, WhereMethodInfo.MakeGenericMethod(entityType.ClrType), query.Expression, keyPredicateLambdaExpression);
			return whereCallExpression;
		}

		protected Expression BuildSelectFirstOrDefaultPermissionEntityExpression(IEntityType entityType, JObject jObject, Expression queryableExpression, ParameterExpression parameterExpression, Expression selectBodyExpression) {
			var selectLambdaExpression = Expression.Lambda(selectBodyExpression, parameterExpression);
			var selectCallExpression = Expression.Call(SelectMethodInfo.MakeGenericMethod(entityType.ClrType, selectBodyExpression.Type), queryableExpression, selectLambdaExpression);
			var firstOrDefaultCallExpression = Expression.Call(null, FirstOrDefaultMethodInfo.MakeGenericMethod(selectBodyExpression.Type), selectCallExpression);
			return firstOrDefaultCallExpression;
		}

		protected MemberBinding BuildSelfPermissionBinding(
			IEntityType entityType,
			Expression subject,
			PropertyInfo selfPermissionPropertyInfo,
			object permissionResolverContext
		) {
			return Expression.Bind(
				selfPermissionPropertyInfo,
				(PermissionResolverExpressionBuilderProvider.GetPermissionResolverExpressionBuilderFor(entityType.ClrType) ?? DefaultPermissionResolverExpressionBuilder).BuildResolvePermissionExpression(permissionResolverContext, subject)
			);
		}

		protected MemberBinding[] BuildPropertiesPermissionBindings(
			Expression subject,
			IEnumerable<PropertyTuple> propertiesTuples,
			object permissionResolverContext
		) {
			return propertiesTuples.Select(tuple => {
				var propertyRule = tuple.PermissionResolverExpressionBuilder.BuildResolvePermissionExpression(permissionResolverContext, subject);
				return Expression.Bind(tuple.PermissionPropertyInfo, propertyRule);
			}).ToArray();
		}

		protected (MemberAssignment MemberBinding, Action<object> Inspector, Action<EntityEntry> EntryAction, Action<object> PostAction)[] BuildScalarNavigationBindings(IEnumerable<NavigationTuple> propertiesTuples, object permissionResolverContext) {
			var propertiesMemberBindingExpressions = propertiesTuples.Select(tuple => {
				if (!tuple.Property.IsDependentToPrincipal()) {
					var fkProperties = tuple.Property.ForeignKey.Properties.ToArray();
				}
				var context = Inspect(
					tuple.Property.ClrType,
					(JObject)tuple.JProperty.Value,
					permissionResolverContext
				);
				return (
					Expression.Bind(tuple.PermissionNavigationPropertyInfo, context.PermissionEntityExpression),
					new Action<object>(permissionEntity => context.Inspector(tuple.PermissionNavigationPropertyInfo.GetValue(permissionEntity))),
					new Action<EntityEntry>(entry => context.EntryAction(entry.Reference(tuple.Property.Name).TargetEntry)),
	 				new Action<object>(entity => { context.PostAction?.Invoke(tuple.Property.PropertyInfo.GetValue(entity)); })
				);
			}).ToArray();

			return propertiesMemberBindingExpressions;
		}

		protected (MemberAssignment MemberBinding, Action<object> Inspector, Action<EntityEntry> EntryAction, Action<object> PostAction)[] BuildCollectionNavigationBindings(IEnumerable<NavigationTuple> propertiesTuples, object permissionResolverContext) {
			var collectionPropertiesMemberBindingExpressions = propertiesTuples.Where(pt => pt.JProperty.Value.Any()).Select(tuple => {
				var arrayValues = tuple.JProperty.Value.Select((value, index) => {
					var context = Inspect(
						tuple.Property.ClrType.GetEnumerableUnderlyingType(),
						(JObject)tuple.JProperty.Value[index],
						permissionResolverContext
					);
					return new { Context = context, Index = index };
				}).ToArray();
				var postActionArrayValues = tuple.JProperty.Value.Where(jp => {
					return !(jp is JObject && ((JObject)jp).Properties().Any(p => p.Name == "$isDeletion"));
				}).Select((value, index) => {
					var context = Inspect(
						tuple.Property.ClrType.GetEnumerableUnderlyingType(),
						(JObject)tuple.JProperty.Value[index],
						permissionResolverContext
					);
					return new { Context = context, Index = index };
				}).ToArray();
				var memberBinding = Expression.Bind(tuple.PermissionNavigationPropertyInfo, Expression.NewArrayInit(tuple.PermissionNavigationPropertyInfo.PropertyType.GetEnumerableUnderlyingType(), arrayValues.Select(c => c.Context.PermissionEntityExpression)));
				var action = new Action<object>(permissionEntity => {
					var collection = (object[])tuple.PermissionNavigationPropertyInfo.GetValue(permissionEntity);
					foreach (var arrayValue in arrayValues) {
						arrayValue.Context.Inspector(collection[arrayValue.Index]);
					}
				});
				var entryAction = new Action<EntityEntry>(entry => {
					var entryCollection = entry.Collection(tuple.Property.Name);
					var collection = entryCollection.CurrentValue.Cast<object>();
					foreach (var arrayValue in arrayValues) {
						arrayValue.Context.EntryAction(entry.Context.Entry(collection.ElementAt(arrayValue.Index)));
					}
					entryCollection.IsModified = true;
				});
				var postAction = new Action<object>(entity => {
					var collection = (IEnumerable<object>)tuple.Property.PropertyInfo.GetValue(entity);
					foreach (var item in postActionArrayValues) {
						item.Context.PostAction(collection.ElementAt(item.Index));
					}
				});
				return (memberBinding, action, entryAction, postAction);
			}).ToArray();
			return collectionPropertiesMemberBindingExpressions;
		}

		protected MemberInitExpression BuildNewPermissionMemberInitExpression(Type permissionEntityType, MemberBinding[] memberBindings) {
			var newExpression = Expression.New(permissionEntityType.GetConstructor(new Type[0]));
			return Expression.MemberInit(newExpression, memberBindings);
		}
	}
	
}
