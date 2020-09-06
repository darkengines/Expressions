using Darkengines.Expressions.Rules;
using Darkengines.Expressions.Security;
using DarkEngines.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Newtonsoft.Json.Linq;
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Darkengines.Expressions.Mutation {
	public class MutationNode : IMutationNode {
		protected IEnumerable<IRuleMap> RuleMaps { get; }
		protected IRuleMap RuleMap { get; }
		public IKey PrimaryKey { get; }
		public Type PermissionType { get; }
		public PropertyInfo SelfPermissionProperty { get; }
		public PropertyTuple[] PropertiesTuples { get; set; }
		public NavigationTuple[] NavigationTuples { get; }
		public PropertyTuple[] AllPropertiesTuples { get; }
		public PropertyTuple[] AllPropertiesTuplesButKey { get; }
		public NavigationTuple[] CollectionNavigationTuples { get; }
		public NavigationTuple[] ScalarNavigationTuples { get; }
		protected IQueryProviderProvider QueryProviderProvider { get; }
		protected PermissionEntityTypeBuilder PermissionEntityTypeBuilder { get; }
		protected JObject JObject { get; }
		protected IEntityType EntityType { get; }
		protected MethodInfo QueryMethodInfo { get; } = ExpressionHelper.ExtractMethodInfo<DbContext, Func<IQueryable<object>>>(context => context.Set<object>).GetGenericMethodDefinition();
		protected MethodInfo WhereMethodInfo { get; } = ExpressionHelper.ExtractMethodInfo<IQueryable<object>, Func<Expression<Func<object, bool>>, IQueryable<object>>>(context => context.Where).GetGenericMethodDefinition();
		protected MethodInfo SelectMethodInfo { get; } = ExpressionHelper.ExtractMethodInfo<IQueryable<object>, Func<Expression<Func<object, object>>, IQueryable<object>>>(queryable => queryable.Select).GetGenericMethodDefinition();
		protected MethodInfo FirstOrDefaultMethodInfo { get; } = ExpressionHelper.ExtractMethodInfo<IQueryable<object>, Func<object>>(queryable => queryable.FirstOrDefault).GetGenericMethodDefinition();
		public IDictionary<INavigation, MutationNode> ScalarChildren { get; }
		public IDictionary<(INavigation, int), MutationNode> CollectionChildren { get; }
		public IDictionary<(INavigation, JToken), MutationNode> JTokenCollectionChildren { get; }
		public MutationNodeType Type { get; }
		protected bool IsReference { get; }
		protected bool ShouldPrepareEntry { get; set; } = true;
		public MutationNode(
			IEnumerable<IRuleMap> ruleMaps,
			IQueryProviderProvider queryProviderProvider,
			PermissionEntityTypeBuilder permissionEntityTypeBuilder,
			JObject jObject,
			IEntityType entityType,
			object context
		) {
			IsReference = jObject.ContainsKey("$ref");
			ScalarChildren = new Dictionary<INavigation, MutationNode>();
			CollectionChildren = new Dictionary<(INavigation, int), MutationNode>();
			JTokenCollectionChildren = new Dictionary<(INavigation, JToken), MutationNode>();
			RuleMaps = ruleMaps;
			QueryProviderProvider = queryProviderProvider;
			PermissionEntityTypeBuilder = permissionEntityTypeBuilder;
			JObject = jObject;
			EntityType = entityType;
			RuleMap = RuleMaps.Where(rm => rm.CanHandle(entityType.ClrType, context)).BuildRuleMap();

			var jProperties = JObject.Properties();

			PrimaryKey = entityType.FindPrimaryKey();
			PermissionType = PermissionEntityTypeBuilder.TypeMap[entityType];
			SelfPermissionProperty = PermissionEntityTypeBuilder.TypeMap[entityType].GetProperty("SelfPermission");
			PropertiesTuples = entityType.GetProperties().Join(
				jProperties,
				p => p.Name,
				jp => jp.Name.ToPascalCase(),
				(p, jp) => new PropertyTuple(
					p,
					jp,
					PermissionEntityTypeBuilder.PermissionPropertyMap[p],
					PermissionEntityTypeBuilder.PropertyMap[p]
				)
			).ToArray();

			NavigationTuples = entityType.GetNavigations().Join(
				jProperties,
				p => p.Name,
				jp => jp.Name.ToPascalCase(),
				(p, jp) => new NavigationTuple(
					p,
					jp,
					PermissionEntityTypeBuilder.PermissionPropertyMap[p],
					PermissionEntityTypeBuilder.PropertyMap[p]
				)
			).ToArray();

			AllPropertiesTuples = PropertiesTuples.Concat(NavigationTuples.Select(np => new PropertyTuple(
				np.Property,
				np.JProperty,
				np.PermissionPropertyInfo,
				np.PermissionNavigationPropertyInfo
			))).ToArray();

			AllPropertiesTuplesButKey = AllPropertiesTuples.Where(pt => {
				var result = !PrimaryKey.Properties.Any(pk => pk == pt.Property);
				//var result = !(pt.Property is IProperty && ((IProperty)pt.Property).ValueGenerated > 0);
				return result;
			}).ToArray();
			CollectionNavigationTuples = NavigationTuples.Where(t => t.Property.IsCollection()).ToArray();
			ScalarNavigationTuples = NavigationTuples.Where(t => !t.Property.IsCollection()).ToArray();
			var touchedForeignKeys = entityType.GetForeignKeys().Where(fk => fk.Properties.Any(p => PropertiesTuples.Any(pt => pt.Property == p)));
			foreach (var touchedForeignKey in touchedForeignKeys) {
				var navigation = touchedForeignKey.GetNavigation(true);
				var navigationTuple = ScalarNavigationTuples.FirstOrDefault(n => n.Property == navigation);
				var childJObject = navigationTuple?.JProperty?.Value as JObject ?? new JObject();
				var index = 0;
				foreach (var fkProperty in touchedForeignKey.PrincipalKey.Properties) {
					if (!childJObject.Properties().Any(p => p.Name.ToPascalCase() == fkProperty.Name)) {
						childJObject.Add(fkProperty.Name.ToCamelCase(), jObject.Property(touchedForeignKey.Properties[index].Name.ToCamelCase()).Value);
					}
					index++;
				}
				if (navigationTuple == null) {
					jObject.Add(navigation.Name.ToCamelCase(), childJObject);
					navigationTuple = new NavigationTuple(navigation, jObject.Property(navigation.Name.ToCamelCase()), PermissionEntityTypeBuilder.PermissionPropertyMap[navigation], PermissionEntityTypeBuilder.PropertyMap[navigation]);
					ScalarNavigationTuples = ScalarNavigationTuples.Append(navigationTuple).ToArray();
				}
			}

			Type = ResolveType();
			switch (Type) {
				case (MutationNodeType.Creation): { RequiredPermission = Permission.Create; break; }
				case (MutationNodeType.Edition): { RequiredPermission = Permission.Write; break; }
				case (MutationNodeType.Deletion): { RequiredPermission = Permission.Delete; break; }
				case (MutationNodeType.Read): { RequiredPermission = Permission.Read; break; }
				default: { RequiredPermission = Permission.Read; break; }
			}



			foreach (var scalarChild in ScalarNavigationTuples) {
				ScalarChildren[scalarChild.Property] = new MutationNode(ruleMaps, queryProviderProvider, permissionEntityTypeBuilder, (JObject)scalarChild.JProperty.Value, scalarChild.Property.GetTargetType(), context);
			}
			foreach (var collectionChild in CollectionNavigationTuples) {
				var index = 0;
				foreach (var item in collectionChild.JProperty.Value) {
					CollectionChildren[(collectionChild.Property, index)] = JTokenCollectionChildren[(collectionChild.Property, item)] = new MutationNode(ruleMaps, queryProviderProvider, permissionEntityTypeBuilder, (JObject)collectionChild.JProperty.Value[index], collectionChild.Property.GetTargetType(), context);
					index++;
				}
			}
		}
		protected MutationNodeType ResolveType() {
			var jProperties = JObject.Properties();
			var isPrimaryKeySet = PrimaryKey.Properties.All(p => jProperties.Any(jProperty => jProperty.Name.ToPascalCase() == p.Name));
			var isGeneratedPrimaryKey = PrimaryKey.Properties[0].ValueGenerated.HasFlag(ValueGenerated.OnAdd);
			var hasTouchedProperties = PropertiesTuples.Any(p => !PrimaryKey.Properties.Any(pk => p.Property == pk));
			var hasTouchedReferences = ScalarNavigationTuples.Any(r => !PrimaryKey.Properties.Any(pk => r.Property.ForeignKey.Properties.Any(fk => fk == pk)));
			var isDeletion = jProperties.Any(p => p.Name == "$isDeletion");
			var isEdition = jProperties.Any(p => p.Name == "$isEdition");
			var isCreation = jProperties.Any(p => p.Name == "$isCreation");
			if (isPrimaryKeySet) {
				if (isDeletion) return MutationNodeType.Deletion;
				if (isEdition) return MutationNodeType.Edition;
				if (!isGeneratedPrimaryKey && isCreation) return MutationNodeType.Creation;
				if (hasTouchedProperties || hasTouchedReferences) return MutationNodeType.Edition;
				return MutationNodeType.Read;
			} else {
				return MutationNodeType.Creation;
			}
		}
		protected Permission RequiredPermission { get; }
		public void CheckPermissions(object context, object entity) {
			var expression = BuildPermissionEntityExpression(context, entity);
			//var localPermissionEntity = GetLocalPermissionEntity(context, entity);
			var permissionEntity = expression != null ? Expression.Lambda<Func<object>>(expression).Compile()() : null;
			CheckPermissions(context, permissionEntity, entity);
		}
		protected void CheckPermissions(object context, object permissionEntity, object entity) {
			if (!IsReference) {
				var permissionEntityType = permissionEntity?.GetType();
				var primaryKeyPairs = PropertiesTuples.Where(pt => PrimaryKey.Properties.Any(pk => pk == pt.Property));
				var primaryKeyTuples = PrimaryKey.Properties.Join(PropertiesTuples, pk => pk, pt => pt.Property, (pk, pt) => pt);

				var typePermission = (bool?)RuleMap.ResolveTypeCustom(RequiredPermission, context);
				var instancePermission = (bool?)permissionEntityType?.GetProperty("SelfPermission")?.GetValue(permissionEntity)
				?? (bool?)RuleMap.ResolveInstanceCustom(context, entity, this.RequiredPermission);

				if (instancePermission != null && !instancePermission.Value) {
					throw new UnauthorizedAccessException($"You do not have permission to {Enum.GetName(typeof(Permission), RequiredPermission)} on entity type {EntityType.Name} with id with key ({string.Join(", ", primaryKeyPairs.Select(kp => $"{kp.Property.Name}={kp.JProperty.Value}"))}).");
				}

				if (instancePermission == null && (typePermission == null || !typePermission.Value)) {
					throw new UnauthorizedAccessException($"You do not have permission to {Enum.GetName(typeof(Permission), RequiredPermission)} on entity type {EntityType.Name}.");
				}

				var globalPermission = instancePermission ?? typePermission;

				foreach (var primaryKeyPropertyTuple in primaryKeyTuples) {
					var propertyPermission = (bool?)RuleMap.ResolvePropertyCustom(RequiredPermission, primaryKeyPropertyTuple.Property.PropertyInfo, context);
					var instancePropertyPermission = (bool?)permissionEntityType?.GetProperty($"{primaryKeyPropertyTuple.Property.Name}Permission")?.GetValue(permissionEntity)
					?? (bool?)RuleMap.ResolveInstancePropertyCustom(primaryKeyPropertyTuple.Property.PropertyInfo, Permission.Read, context, entity);
					if (instancePropertyPermission != null && !instancePropertyPermission.Value) {
						throw new UnauthorizedAccessException($"You do not have permission to {Enum.GetName(typeof(Permission), Permission.Read)} on property {primaryKeyPropertyTuple.Property.Name} on entity type {EntityType.Name} with key ({string.Join(", ", primaryKeyPairs.Select(kp => $"{kp.Property.Name}={kp.JProperty.Value}"))}).");
					}
					if (instancePropertyPermission == null && (globalPermission == null || !globalPermission.Value) && (propertyPermission == null || !propertyPermission.Value)) {
						throw new UnauthorizedAccessException($"You do not have permission to {Enum.GetName(typeof(Permission), Permission.Read)} on property {primaryKeyPropertyTuple.Property.Name} on entity type {EntityType.Name}.");
					}
				}

				foreach (var propertyTuple in AllPropertiesTuplesButKey) {
					var propertyPermission = (bool?)RuleMap.ResolvePropertyCustom(RequiredPermission, propertyTuple.Property.PropertyInfo, context);
					var instancePropertyPermission = (bool?)permissionEntityType?.GetProperty($"{propertyTuple.Property.Name}Permission")?.GetValue(permissionEntity)
					?? (bool?)RuleMap.ResolveInstancePropertyCustom(propertyTuple.Property.PropertyInfo, RequiredPermission, context, entity);
					if (instancePropertyPermission != null && !instancePropertyPermission.Value) {
						throw new UnauthorizedAccessException($"You do not have permission to {Enum.GetName(typeof(Permission), RequiredPermission)} on property {propertyTuple.Property.Name} on entity type {EntityType.Name} with key ({string.Join(", ", primaryKeyPairs.Select(kp => $"{kp.Property.Name}={kp.JProperty.Value}"))}).");
					}
					if (instancePropertyPermission == null && (globalPermission == null || !globalPermission.Value) && (propertyPermission == null || !propertyPermission.Value)) {
						throw new UnauthorizedAccessException($"You do not have permission to {Enum.GetName(typeof(Permission), RequiredPermission)} on property {propertyTuple.Property.Name} on entity type {EntityType.Name}.");
					}
				}

				foreach (var scalarChild in ScalarChildren) {
					scalarChild.Value.CheckPermissions(context, permissionEntityType?.GetProperty(scalarChild.Key.Name)?.GetValue(permissionEntity), scalarChild.Key.PropertyInfo.GetValue(entity));
				}

				foreach (var collectionChild in CollectionChildren) {
					var collection = (IEnumerable<object>)permissionEntityType?.GetProperty(collectionChild.Key.Item1.Name)?.GetValue(permissionEntity);
					collectionChild.Value.CheckPermissions(context, collection?.ElementAt(collectionChild.Key.Item2), ((IEnumerable<object>)collectionChild.Key.Item1.PropertyInfo.GetValue(entity)).ElementAt(collectionChild.Key.Item2));
				}
			}
		}
		void IMutationNode.CheckPermissions(object context) {
			var typePermission = RuleMap.ResolveTypePermission(context);
		}
		public async Task PostPersistAction(object context, object instance, EntityEntry entry) {
			if (!IsReference) {
				switch (Type) {
					case (MutationNodeType.Creation): { await RuleMap.OnAfterCreation(context, instance, entry); break; }
					case (MutationNodeType.Edition): { await RuleMap.OnAfterEdition(context, instance, entry); break; }
					case (MutationNodeType.Deletion): { await RuleMap.OnAfterDeletion(context, instance, entry); break; }
				}
				foreach (var scalarChild in ScalarNavigationTuples) {
					await ScalarChildren[scalarChild.Property].PostPersistAction(context, scalarChild.Property.PropertyInfo.GetValue(instance), entry.Reference(scalarChild.Property.Name).TargetEntry);
				}
				foreach (var collectionChild in CollectionNavigationTuples) {
					var index = 0;
					var collection = (IEnumerable<object>)collectionChild.Property.PropertyInfo.GetValue(instance);
					foreach (var item in collectionChild.JProperty.Value) {
						if (item is JObject && !((JObject)item).ContainsKey("$isDeletion")) {
							var entityItem = collection.ElementAt(index);
							await CollectionChildren[(collectionChild.Property, index)].PostPersistAction(context, entityItem, entry.Collection(collectionChild.Property.Name).FindEntry(entityItem));
							index++;
						}
					}
				}
			}
		}
		public async Task PrepareEntry(EntityEntry entry) {
			if (!IsReference) {
				switch (Type) {
					case (MutationNodeType.Creation): { entry.State = EntityState.Added; break; }
					case (MutationNodeType.Edition): { entry.State = EntityState.Modified; break; }
					case (MutationNodeType.Deletion): { entry.State = EntityState.Deleted; break; }
					case (MutationNodeType.Read): { entry.State = EntityState.Unchanged; break; }
				}
				var isGeneratedPrimaryKey = PrimaryKey.Properties[0].ValueGenerated.HasFlag(ValueGenerated.OnAdd);
				var unwantedProperties = PropertiesTuples.Where(pt => {
					return ((IProperty)pt.Property).ValueGenerated.HasFlag(ValueGenerated.OnAdd)
					|| (Type != MutationNodeType.Creation && PrimaryKey.Properties.Any(pkp => pkp == pt.Property));
				});
				var touchedProperties = PropertiesTuples.Where(p => !unwantedProperties.Any(up => up == p));
				foreach (var property in touchedProperties) {
					entry.Property(property.Property.Name).IsModified = true;
				}
				foreach (var property in EntityType.GetProperties().Where(p => !touchedProperties.Any(tp => tp.Property == p))) {
					entry.Property(property.Name).IsModified = false;
				}
				foreach (var scalarChild in ScalarNavigationTuples) {
					var reference = entry.Reference(scalarChild.Property.Name);
					reference.IsModified = true;
					await ScalarChildren[scalarChild.Property].PrepareEntry(reference.TargetEntry);
				}
				foreach (var collectionChild in CollectionNavigationTuples) {
					var collection = entry.Collection(collectionChild.Property.Name);
					collection.IsModified = true;
					var index = 0;
					foreach (var item in collectionChild.JProperty.Value) {
						await CollectionChildren[(collectionChild.Property, index)].PrepareEntry(collection.FindEntry(collection.CurrentValue.Cast<object>().ElementAt(index)));
						index++;
					}
				}
			}
		}
		public async Task<ActionResult> PrePersistAction(object context, object instance, EntityEntry entry) {
			var actionResult = new ActionResult();
			if (!IsReference) {
				switch (Type) {
					case (MutationNodeType.Creation): { return await RuleMap.OnBeforeCreation(context, instance, entry, actionResult); }
					case (MutationNodeType.Edition): { return await RuleMap.OnBeforeEdition(context, instance, entry, actionResult); }
					case (MutationNodeType.Deletion): { return await RuleMap.OnBeforeDeletion(context, instance, entry, actionResult); }
				}
			}
			return actionResult;
		}

		protected Expression BuildPermissionEntityExpression(object context, object instance, Expression memberExpression = null) {
			var isCreation = Type == MutationNodeType.Creation;
			var entityParameterExpression = isCreation ? null : memberExpression ?? Expression.Parameter(EntityType.ClrType);
			var selfPermissionBindingExpression = isCreation ? null : BuildSelfPermissionBinding(entityParameterExpression, context, instance);
			var permissionPropertiesMemberBindingExpressions = isCreation ? Enumerable.Empty<MemberBinding>() : BuildPropertiesPermissionBindings(entityParameterExpression, AllPropertiesTuples, context);
			var scalarNavigationPropertiesMemberBindingExpressions = BuildScalarNavigationBindings(context, instance, entityParameterExpression);
			var collectionNavigationPropertiesMemberBindingExpressions = BuildCollectionNavigationBindings(context, instance);


			var memberBindingExpressions = permissionPropertiesMemberBindingExpressions.Union(scalarNavigationPropertiesMemberBindingExpressions).Union(collectionNavigationPropertiesMemberBindingExpressions);
			if (selfPermissionBindingExpression != null) memberBindingExpressions = memberBindingExpressions.Append(selfPermissionBindingExpression);

			if (memberBindingExpressions.Any()) {
				var pkPropertyTuples = PropertiesTuples.Where(p => PrimaryKey.Properties.Contains(p.Property)).ToArray();
				if (entityParameterExpression != null) {
					var primaryKeyBindingExpression = pkPropertyTuples.Select(pk => Expression.Bind(PermissionType.GetProperty(pk.PermissionNavigationPropertyInfo.Name), Expression.MakeMemberAccess(entityParameterExpression, pk.Property.PropertyInfo)));
					memberBindingExpressions = primaryKeyBindingExpression.Union(memberBindingExpressions);
				}
				var permissionEntityType = PermissionEntityTypeBuilder.TypeMap[EntityType];
				var newPermissionExpression = BuildNewPermissionMemberInitExpression(permissionEntityType, memberBindingExpressions.ToArray());
				if (memberExpression == null) {
					var primaryKeyDictionary = PrimaryKey.Properties.Join(JObject.Properties(), p => p.Name, jProperty => jProperty.Name.ToPascalCase(), (property, jProperty) => (property, jProperty)).ToDictionary(tuple => (IProperty)tuple.property, tuple => tuple.jProperty.Value.ToObject(tuple.property.ClrType));
					var findEntityExpression = Type == MutationNodeType.Creation ? null : BuildFindEntityQuery(primaryKeyDictionary);
					var permissionExpression = findEntityExpression == null ? newPermissionExpression : BuildSelectFirstOrDefaultPermissionEntityExpression(EntityType, JObject, findEntityExpression, (ParameterExpression)entityParameterExpression, newPermissionExpression);
					return permissionExpression;
				} else {
					return newPermissionExpression;
				}
			}
			return null;
		}

		protected object GetLocalPermissionEntity(object context, object instance) {
			var permissionEntityType = PermissionEntityTypeBuilder.TypeMap[EntityType];
			var permissionEntity = Activator.CreateInstance(permissionEntityType);
			var typePermission = (bool?)RuleMap.ResolveTypeCustom(RequiredPermission, context);
			var instancePermission = (bool?)RuleMap.ResolveInstanceCustom(context, instance, RequiredPermission);
			SelfPermissionProperty.SetValue(permissionEntity, instancePermission ?? typePermission);
			foreach (var propertyTuple in AllPropertiesTuplesButKey) {
				var propertyPermission = (bool?)RuleMap.ResolvePropertyCustom(RequiredPermission, propertyTuple.Property.PropertyInfo, context);
				var instancePropertyPermission = (bool?)RuleMap.ResolveInstancePropertyCustom(propertyTuple.Property.PropertyInfo, RequiredPermission, context, instance);
				propertyTuple.PermissionPropertyInfo.SetValue(permissionEntity, instancePropertyPermission ?? propertyPermission);
			}
			foreach (var referenceTuple in ScalarNavigationTuples) {
				var propertyPermission = (bool?)RuleMap.ResolvePropertyCustom(RequiredPermission, referenceTuple.Property.PropertyInfo, context);
				var instancePropertyPermission = (bool?)RuleMap.ResolveInstancePropertyCustom(referenceTuple.Property.PropertyInfo, RequiredPermission, context, instance);
				referenceTuple.PermissionPropertyInfo.SetValue(permissionEntity, instancePropertyPermission ?? propertyPermission);
				referenceTuple.PermissionNavigationPropertyInfo.SetValue(permissionEntity, ScalarChildren[referenceTuple.Property].GetLocalPermissionEntity(context, referenceTuple.Property.PropertyInfo.GetValue(instance)));
			}
			foreach (var collectionTuple in CollectionNavigationTuples.Where(tuple => tuple.JProperty.Value.Any())) {
				var propertyPermission = (bool?)RuleMap.ResolvePropertyCustom(RequiredPermission, collectionTuple.Property.PropertyInfo, context);
				var instancePropertyPermission = (bool?)RuleMap.ResolveInstancePropertyCustom(collectionTuple.Property.PropertyInfo, RequiredPermission, context, instance);
				collectionTuple.PermissionPropertyInfo.SetValue(permissionEntity, instancePropertyPermission ?? propertyPermission);
				var itemPermissions = collectionTuple.JProperty.Value.Select((value, index) => CollectionChildren[(collectionTuple.Property, index)].GetLocalPermissionEntity(context, ((IEnumerable<object>)collectionTuple.Property.PropertyInfo.GetValue(instance)).ElementAt(index))).Select(x => Convert.ChangeType(x, collectionTuple.PermissionNavigationPropertyInfo.PropertyType.GetEnumerableUnderlyingType())).ToArray();
				var array = Array.CreateInstance(collectionTuple.PermissionNavigationPropertyInfo.PropertyType.GetEnumerableUnderlyingType(), itemPermissions.Length);
				var itemIndex = 0;
				foreach (var item in itemPermissions) {
					array.SetValue(item, itemIndex);
					itemIndex++;
				}
				collectionTuple.PermissionNavigationPropertyInfo.SetValue(permissionEntity, array);
			}

			return permissionEntity;
		}

		Expression IMutationNode.BuildPermissionEntityExpression(object context, object instance) {
			return BuildPermissionEntityExpression(context, instance);
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


		protected MemberInitExpression BuildNewPermissionMemberInitExpression(Type permissionEntityType, MemberBinding[] memberBindings) {
			var newExpression = Expression.New(permissionEntityType.GetConstructor(new Type[0]));
			return Expression.MemberInit(newExpression, memberBindings);
		}

		protected Expression BuildFindEntityQuery(IDictionary<IProperty, object> keyPairs) {
			var query = QueryProviderProvider.GetQuery(EntityType.ClrType);
			var keyPredicateLambdaExpression = GetKeyFilterExpression(JObject, EntityType, keyPairs);
			var whereCallExpression = Expression.Call(null, WhereMethodInfo.MakeGenericMethod(EntityType.ClrType), Expression.Constant(query), keyPredicateLambdaExpression);
			return whereCallExpression;
		}

		protected Expression BuildSelectFirstOrDefaultPermissionEntityExpression(IEntityType entityType, JObject jObject, Expression queryableExpression, ParameterExpression parameterExpression, Expression selectBodyExpression) {
			var selectLambdaExpression = Expression.Lambda(selectBodyExpression, parameterExpression);
			var selectCallExpression = Expression.Call(SelectMethodInfo.MakeGenericMethod(entityType.ClrType, selectBodyExpression.Type), queryableExpression, selectLambdaExpression);
			var firstOrDefaultCallExpression = Expression.Call(null, FirstOrDefaultMethodInfo.MakeGenericMethod(selectBodyExpression.Type), selectCallExpression);
			return firstOrDefaultCallExpression;
		}
		protected MemberBinding BuildSelfPermissionBinding(
			Expression subject,
			object permissionResolverContext,
			object instance
		) {
			var selfPermissionExpression = RuleMap.GetInstanceCustomResolverExpression(permissionResolverContext, subject, RequiredPermission);
			return selfPermissionExpression == null ? null : Expression.Bind(SelfPermissionProperty, Expression.Convert(selfPermissionExpression, typeof(bool?)));
		}
		protected MemberBinding[] BuildPropertiesPermissionBindings(
			Expression subject,
			IEnumerable<PropertyTuple> propertiesTuples,
			object permissionResolverContext
		) {
			return propertiesTuples.Select(tuple => {
				var expression = RuleMap.ResolveInstancePropertyCustomResolverExpression(tuple.Property.PropertyInfo, RequiredPermission, permissionResolverContext, subject);
				return expression == null ? null : Expression.Bind(PermissionType.GetProperty(tuple.PermissionPropertyInfo.Name), Expression.Convert(expression, typeof(bool?)));
			}).Where(e => e != null).ToArray();
		}
		protected MemberBinding[] BuildScalarNavigationBindings(object permissionResolverContext, object instance, Expression parameter) {
			var propertiesMemberBindingExpressions = ScalarNavigationTuples.Select(tuple => {
				var propertyInfo = EntityType.ClrType.GetProperty(tuple.Property.PropertyInfo.Name);
				var memberExpression = parameter == null ? null : Expression.MakeMemberAccess(parameter, propertyInfo);
				var value = ScalarChildren[tuple.Property].BuildPermissionEntityExpression(permissionResolverContext, tuple.Property.PropertyInfo.GetValue(instance), memberExpression);
				return value == null ? null : Expression.Bind(PermissionType.GetProperty(tuple.PermissionNavigationPropertyInfo.Name), value);
			}).Where(binding => binding != null).ToArray();

			return propertiesMemberBindingExpressions;
		}
		protected MemberBinding[] BuildCollectionNavigationBindings(object permissionResolverContext, object instance) {
			var collections = CollectionNavigationTuples.Select(collection => new {
				Collection = collection,
				Values = collection.JProperty.Value.Select((value, index) => new {
					MutationNode = JTokenCollectionChildren[(collection.Property, value)],
					Value = value,
					Index = index
				}).ToArray()
			}).Where(collection => collection.Values.Any()).ToArray();
			var bindings = collections.Select(collection => {
				var values = collection.Values.Select(value => {
					var result = value.MutationNode.BuildPermissionEntityExpression(permissionResolverContext, ((IEnumerable<object>)collection.Collection.Property.PropertyInfo.GetValue(instance)).ElementAt(value.Index));
					return result;
				}).Where(value => value != null);
				if (values.Any()) {
					var bindingExpression = Expression.Bind(
						collection.Collection.PermissionNavigationPropertyInfo,
						Expression.NewArrayInit(
							collection.Collection.PermissionNavigationPropertyInfo.PropertyType.GetEnumerableUnderlyingType(),
							values
						)
					);
					return bindingExpression;
				}
				return null;
			}).Where(binding => binding != null).ToArray();
			return bindings;
		}
	}
	public class PropertyTuple {
		public IPropertyBase Property { get; }
		public JProperty JProperty { get; }
		public PropertyInfo PermissionPropertyInfo { get; }
		public PropertyInfo PermissionNavigationPropertyInfo { get; }
		public PropertyTuple(
			IPropertyBase property,
			JProperty jProperty,
			PropertyInfo permissionPropertyInfo,
			PropertyInfo permissionNavigationPropertyInfo
		) {
			Property = property;
			JProperty = jProperty;
			PermissionPropertyInfo = permissionPropertyInfo;
			PermissionNavigationPropertyInfo = permissionNavigationPropertyInfo;
		}
	}
	public class NavigationTuple {
		public INavigation Property { get; }
		public JProperty JProperty { get; }
		public PropertyInfo PermissionPropertyInfo { get; }
		public PropertyInfo PermissionNavigationPropertyInfo { get; }
		public NavigationTuple(
			INavigation property,
			JProperty jProperty,
			PropertyInfo permissionPropertyInfo,
			PropertyInfo permissionNavigationPropertyInfo
		) {
			Property = property;
			JProperty = jProperty;
			PermissionPropertyInfo = permissionPropertyInfo;
			PermissionNavigationPropertyInfo = permissionNavigationPropertyInfo;
		}
	}
	public enum MutationNodeType {
		Creation,
		Edition,
		Deletion,
		Read
	}
}
