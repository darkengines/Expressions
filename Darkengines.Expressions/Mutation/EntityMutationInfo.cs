using Darkengines.Expressions.Rules;
using Darkengines.Expressions.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Darkengines.Expressions.Mutation {
	public class EntityMutationInfo {
		public IEntityType EntityType { get; }
		public EntityState State { get; set; }
		public IDictionary<IProperty, PropertyMutationInfo> Properties { get; }
		public IDictionary<INavigation, ReferenceMutationInfo> References { get; }
		public IDictionary<INavigation, CollectionMutationInfo> Collections { get; }
		public ISet<PropertyPropertyTuple> PropertyTuples { get; }
		public ISet<NavigationPropertyTuple> ReferenceTuples { get; }
		public ISet<NavigationPropertyTuple> CollectionTuples { get; }
		public MutationContext MutationContext { get; }
		public PropertyInfo SelfPermissionProperty { get; }
		public IRuleMap RuleMap { get; }
		public object Entity { get; }
		public Type PermissionType { get; }
		public IKey PrimaryKey { get; }
		protected MethodInfo QueryMethodInfo { get; } = ExpressionHelper.ExtractMethodInfo<DbContext, Func<IQueryable<object>>>(context => context.Set<object>).GetGenericMethodDefinition();
		protected MethodInfo WhereMethodInfo { get; } = ExpressionHelper.ExtractMethodInfo<IQueryable<object>, Func<Expression<Func<object, bool>>, IQueryable<object>>>(context => context.Where).GetGenericMethodDefinition();
		protected MethodInfo SelectMethodInfo { get; } = ExpressionHelper.ExtractMethodInfo<IQueryable<object>, Func<Expression<Func<object, object>>, IQueryable<object>>>(queryable => queryable.Select).GetGenericMethodDefinition();
		protected MethodInfo FirstOrDefaultMethodInfo { get; } = ExpressionHelper.ExtractMethodInfo<IQueryable<object>, Func<object>>(queryable => queryable.FirstOrDefault).GetGenericMethodDefinition();
		protected bool IsReference { get; }
		public EntityMutationInfo(IEntityType entityType, JObject jObject, MutationContext mutationContext) {
			IsReference = jObject.ContainsKey("$ref");
			MutationContext = mutationContext;
			EntityType = entityType;
			Properties = new Dictionary<IProperty, PropertyMutationInfo>();
			References = new Dictionary<INavigation, ReferenceMutationInfo>();
			Collections = new Dictionary<INavigation, CollectionMutationInfo>();
			Entity = Activator.CreateInstance(entityType.ClrType);
			RuleMap = mutationContext.RuleMaps.Where(ruleMap => ruleMap.CanHandle(entityType.ClrType, mutationContext.SecurityContext)).BuildRuleMap();
			PermissionType = mutationContext.PermissionEntityTypeBuilder.TypeMap[entityType];
			SelfPermissionProperty = mutationContext.PermissionEntityTypeBuilder.TypeMap[entityType].GetProperty("SelfPermission");
			PrimaryKey = entityType.FindPrimaryKey();

			var jProperties = jObject.Properties();
			var properties = entityType.GetProperties();
			var navigations = entityType.GetNavigations();
			var references = navigations.Where(navigation => !navigation.IsCollection());
			var collections = navigations.Where(navigation => navigation.IsCollection());

			Properties = properties.ToDictionary(property => property, property => new PropertyMutationInfo(MutationContext, property, this));
			References = references.ToDictionary(reference => reference, reference => new ReferenceMutationInfo(MutationContext, reference, this));
			Collections = collections.ToDictionary(collection => collection, collection => new CollectionMutationInfo(MutationContext, collection, this));


			PropertyTuples = jProperties.Join(properties, jp => jp.Name.ToPascalCase(), p => p.Name, (jp, p) => new PropertyPropertyTuple { JProperty = jp, Property = p }).ToHashSet();
			ReferenceTuples = jProperties.Join(references, jp => jp.Name.ToPascalCase(), p => p.Name, (jp, p) => new NavigationPropertyTuple { JProperty = jp, Navigation = p }).ToHashSet();
			CollectionTuples = jProperties.Join(collections, jp => jp.Name.ToPascalCase(), p => p.Name, (jp, p) => new NavigationPropertyTuple { JProperty = jp, Navigation = p }).Where(tuple => tuple.JProperty.Values().Any()).ToHashSet();

			var primaryKeyTuples = PrimaryKey.Properties.Join(PropertyTuples, pkp => pkp, pt => pt.Property, (pkp, pt) => pkp).ToArray();
			var isPrimaryKeySet = primaryKeyTuples.Length == PrimaryKey.Properties.Count;
			var nonGeneratedPropertyTuples = PropertyTuples.Where(pt => !pt.Property.ValueGenerated.HasFlag(ValueGenerated.OnAdd));

			foreach (var propertyTuple in PropertyTuples) {
				var value = propertyTuple.JProperty.Value.ToObject(propertyTuple.Property.ClrType, mutationContext.JsonSerializer);
				propertyTuple.Property.AsProperty().Setter.SetClrValue(Entity, value);
				Properties[propertyTuple.Property].IsModified = !(propertyTuple.Property.ValueGenerated.HasFlag(ValueGenerated.OnAdd) && propertyTuple.Property.IsPrimaryKey());
				Properties[propertyTuple.Property].IsTouched = true;
				var containingForeignKeys = propertyTuple.Property.GetContainingForeignKeys();
				foreach (var containingForeignKey in containingForeignKeys) {
					var navigation = containingForeignKey.GetNavigation(true);
					if (navigation != null) {
						var reference = References[containingForeignKey.GetNavigation(true)];
						reference.IsModified = true;
						var keyProperty1 = propertyTuple.Property.FindFirstPrincipal();
						if (reference.TargetEntityMutationInfo == null) {
							var propertyValue = (JObject)jObject.Property(reference.Navigation.Name.ToCamelCase())?.Value;
							if (propertyValue == null) {
								propertyValue = new JObject();
							}
							if (!propertyValue.ContainsKey(keyProperty1.Name.ToCamelCase())) {
								propertyValue.Add(keyProperty1.Name.ToCamelCase(), JToken.FromObject(value));
							}
							reference.TargetEntityMutationInfo = new EntityMutationInfo(containingForeignKey.PrincipalEntityType, propertyValue, MutationContext);
						} else {
							keyProperty1.AsProperty().Setter.SetClrValue(reference.TargetEntityMutationInfo.Entity, propertyTuple.Property.GetGetter().GetClrValue(Entity));
						}
					}
				}
			}
			foreach (var referenceTuple in ReferenceTuples) {
				var referencedEntityMutationInfo = References[referenceTuple.Navigation].TargetEntityMutationInfo;
				if (referencedEntityMutationInfo == null) References[referenceTuple.Navigation].TargetEntityMutationInfo = referencedEntityMutationInfo = new EntityMutationInfo(referenceTuple.Navigation.GetTargetType(), (JObject)referenceTuple.JProperty.Value, MutationContext);
				References[referenceTuple.Navigation].IsModified = true;
				References[referenceTuple.Navigation].IsTouched = true;
				referenceTuple.Navigation.AsNavigation().Setter.SetClrValue(Entity, References[referenceTuple.Navigation].TargetEntityMutationInfo.Entity);
				foreach (var foreignKeyProperty in referenceTuple.Navigation.ForeignKey.Properties) {
					var fixedForeignKeyProperty = referenceTuple.Navigation.IsDependentToPrincipal() ? foreignKeyProperty : foreignKeyProperty.FindFirstPrincipal();
					Properties[fixedForeignKeyProperty].IsModified = true;
					Properties[fixedForeignKeyProperty].IsTouched = true;
					fixedForeignKeyProperty.AsProperty().Setter.SetClrValue(Entity, fixedForeignKeyProperty.FindFirstPrincipal().AsProperty().Getter.GetClrValue(referencedEntityMutationInfo.Entity));
				}
				var inverseNavigation = referenceTuple.Navigation.FindInverse();
				if (inverseNavigation != null) {
					if (inverseNavigation.IsCollection()) {
						var inverseReferenceMutationInfo = referencedEntityMutationInfo.Collections[inverseNavigation];
						inverseReferenceMutationInfo.TargetEntityMutationInfos.Add(this);
						inverseReferenceMutationInfo.IsModified = true;
						inverseReferenceMutationInfo.IsTouched = true;
						inverseNavigation.AsNavigation().CollectionAccessor.Add(referencedEntityMutationInfo.Entity, Entity, false);
					} else {
						var inverseReferenceMutationInfo = referencedEntityMutationInfo.References[inverseNavigation];
						inverseReferenceMutationInfo.TargetEntityMutationInfo = this;
						inverseReferenceMutationInfo.IsModified = true;
						inverseReferenceMutationInfo.IsTouched = true;
						inverseNavigation.AsNavigation().Setter.SetClrValue(referencedEntityMutationInfo.Entity, Entity);
					}
				}
			}
			foreach (var collectionTuple in CollectionTuples) {
				var targetEntityMutationInfos = collectionTuple.JProperty.Values().Select(value => new EntityMutationInfo(collectionTuple.Navigation.GetTargetType(), (JObject)value, MutationContext)).ToHashSet();
				Collections[collectionTuple.Navigation].IsModified = true;
				Collections[collectionTuple.Navigation].IsTouched = true;
				var foreignKeyProperties = collectionTuple.Navigation.ForeignKey.Properties;
				foreach (var targetEntityMutationInfo in targetEntityMutationInfos) {
					Collections[collectionTuple.Navigation].TargetEntityMutationInfos.Add(targetEntityMutationInfo);
					foreach (var foreignKeyProperty in foreignKeyProperties) {
						var referenceKeyProperty = foreignKeyProperty.FindFirstPrincipal();
						var foreignKeyPropertyMutationInfo = targetEntityMutationInfo.Properties[foreignKeyProperty];
						foreignKeyPropertyMutationInfo.IsModified = true;
						foreignKeyPropertyMutationInfo.IsTouched = true;
						foreignKeyProperty.AsProperty().Setter.SetClrValue(targetEntityMutationInfo.Entity, referenceKeyProperty.AsProperty().Getter.GetClrValue(Entity));
					}
					var inverseNavigation = collectionTuple.Navigation.FindInverse();
					if (inverseNavigation != null) {
						var inverseReferenceMutationInfo = targetEntityMutationInfo.References[inverseNavigation];
						inverseReferenceMutationInfo.IsModified = true;
						inverseReferenceMutationInfo.IsTouched = true;
						inverseReferenceMutationInfo.TargetEntityMutationInfo = this;
						inverseNavigation.AsNavigation().Setter.SetClrValue(targetEntityMutationInfo.Entity, Entity);
					}
					collectionTuple.Navigation.AsNavigation().CollectionAccessor.Add(Entity, targetEntityMutationInfo.Entity, false);
				}
			}

			if (jObject.ContainsKey("$isCreation")) {
				State = EntityState.Added;
			} else if (isPrimaryKeySet) {
				if (jObject.ContainsKey("$isDeletion")) {
					State = EntityState.Deleted;
				} else if (jObject.ContainsKey("$isEdition") || nonGeneratedPropertyTuples.Any()) {
					State = EntityState.Modified;
				} else {
					State = EntityState.Unchanged;
				}
			} else if (PrimaryKey.Properties[0].ValueGenerated == ValueGenerated.OnAdd) {
				State = EntityState.Added;
			} else {
				State = EntityState.Unchanged;
			}
		}
		public Expression BuildPermissionEntityExpression(object context, ISet<EntityMutationInfo> cache, Expression memberExpression = null) {
			cache.Add(this);
			var isCreation = State == EntityState.Added;
			var entityParameterExpression = isCreation ? null : memberExpression ?? Expression.Parameter(EntityType.ClrType);
			var selfPermissionBindingExpression = isCreation ? null : BuildSelfPermissionBinding(entityParameterExpression, context);
			var permissionPropertiesMemberBindingExpressions = Properties.Values.Where(memberMutationInfo => memberMutationInfo.IsModified).Select(property => property.BuildMemberBindingExpression(entityParameterExpression, cache)).ToArray();
			var scalarNavigationPropertiesMemberBindingExpressions = References.Values.Where(memberMutationInfo => memberMutationInfo.IsModified).Select(reference => reference.BuildMemberBindingExpression(entityParameterExpression, cache)).ToArray();
			var collectionNavigationPropertiesMemberBindingExpressions = Collections.Values.Where(memberMutationInfo => memberMutationInfo.IsModified).Select(collection => collection.BuildMemberBindingExpression(entityParameterExpression, cache)).ToArray();


			var memberBindingExpressions = permissionPropertiesMemberBindingExpressions.Union(scalarNavigationPropertiesMemberBindingExpressions).Union(collectionNavigationPropertiesMemberBindingExpressions);
			if (selfPermissionBindingExpression != null) memberBindingExpressions = memberBindingExpressions.Append(selfPermissionBindingExpression).ToArray();
			memberBindingExpressions = memberBindingExpressions.Where(memberBinding => memberBinding != null);
			if (memberBindingExpressions.Any()) {
				if (entityParameterExpression != null) {
					var primaryKeyBindingExpression = PrimaryKey.Properties.Select(primaryKeyProperty => Expression.Bind(PermissionType.GetProperty(primaryKeyProperty.Name), Expression.MakeMemberAccess(entityParameterExpression, primaryKeyProperty.PropertyInfo)));
					memberBindingExpressions = primaryKeyBindingExpression.Union(memberBindingExpressions);
				}
				var permissionEntityType = MutationContext.PermissionEntityTypeBuilder.TypeMap[EntityType];
				var newPermissionExpression = BuildNewPermissionMemberInitExpression(permissionEntityType, memberBindingExpressions.ToArray());
				if (memberExpression == null) {
					var findEntityExpression = State == EntityState.Added ? null : BuildFindEntityQuery();
					var permissionExpression = findEntityExpression == null ? newPermissionExpression : BuildSelectFirstOrDefaultPermissionEntityExpression(findEntityExpression, (ParameterExpression)entityParameterExpression, newPermissionExpression);
					return permissionExpression;
				} else {
					return newPermissionExpression;
				}
			}
			return null;
		}
		protected MemberBinding BuildSelfPermissionBinding(
			Expression subject,
			object permissionResolverContext
		) {
			var selfPermissionExpression = RuleMap.GetInstanceCustomResolverExpression(permissionResolverContext, subject, State.ToPermission());
			return selfPermissionExpression == null ? null : Expression.Bind(SelfPermissionProperty, Expression.Convert(selfPermissionExpression, typeof(bool?)));
		}
		protected Expression GetKeyFilterExpression() {
			var parameter = Expression.Parameter(EntityType.ClrType);
			var predicate = PrimaryKey.Properties.Select(primaryKeyProperty =>
				Expression.Equal(
					Expression.Property(parameter, primaryKeyProperty.PropertyInfo),
					Expression.Constant(primaryKeyProperty.GetGetter().GetClrValue(Entity))
				)
			).Join((keyPredicate, partialPredicate) => Expression.AndAlso(keyPredicate, partialPredicate));
			return Expression.Lambda(predicate, parameter);
		}


		protected MemberInitExpression BuildNewPermissionMemberInitExpression(Type permissionEntityType, MemberBinding[] memberBindings) {
			var newExpression = Expression.New(permissionEntityType.GetConstructor(new Type[0]));
			return Expression.MemberInit(newExpression, memberBindings);
		}

		protected Expression BuildFindEntityQuery() {
			var query = MutationContext.QueryProviderProvider.GetQuery(EntityType.ClrType);
			var keyPredicateLambdaExpression = GetKeyFilterExpression();
			var whereCallExpression = Expression.Call(null, WhereMethodInfo.MakeGenericMethod(EntityType.ClrType), Expression.Constant(query), keyPredicateLambdaExpression);
			return whereCallExpression;
		}

		protected Expression BuildSelectFirstOrDefaultPermissionEntityExpression(Expression queryableExpression, ParameterExpression parameterExpression, Expression selectBodyExpression) {
			var selectLambdaExpression = Expression.Lambda(selectBodyExpression, parameterExpression);
			var selectCallExpression = Expression.Call(SelectMethodInfo.MakeGenericMethod(EntityType.ClrType, selectBodyExpression.Type), queryableExpression, selectLambdaExpression);
			var firstOrDefaultCallExpression = Expression.Call(null, FirstOrDefaultMethodInfo.MakeGenericMethod(selectBodyExpression.Type), selectCallExpression);
			return firstOrDefaultCallExpression;
		}
		public void CheckPermissions(object context, object permissionEntity, ISet<EntityMutationInfo> cache) {
			if (!(IsReference || cache.Contains(this))) {
				cache.Add(this);
				var permissionEntityType = permissionEntity?.GetType();
				var primaryKeyPairs = PropertyTuples.Where(pt => PrimaryKey.Properties.Any(pk => pk == pt.Property));
				var primaryKeyTuples = PrimaryKey.Properties.Join(PropertyTuples, pk => pk, pt => pt.Property, (pk, pt) => pt);

				var requiredPermission = State.ToPermission();
				var typePermission = (bool?)RuleMap.ResolveTypeCustom(requiredPermission, context);
				var instancePermission = (bool?)permissionEntityType?.GetProperty("SelfPermission")?.GetValue(permissionEntity)
				?? (bool?)RuleMap.ResolveInstanceCustom(context, Entity, requiredPermission);

				if (instancePermission != null && !instancePermission.Value) {
					throw new UnauthorizedAccessException($"You do not have permission to {Enum.GetName(typeof(Permission), requiredPermission)} on entity type {EntityType.Name} with id with key ({string.Join(", ", primaryKeyPairs.Select(kp => $"{kp.Property.Name}={kp.JProperty.Value}"))}).");
				}

				if (instancePermission == null && (typePermission == null || !typePermission.Value)) {
					throw new UnauthorizedAccessException($"You do not have permission to {Enum.GetName(typeof(Permission), requiredPermission)} on entity type {EntityType.Name}.");
				}

				var globalPermission = instancePermission ?? typePermission;

				foreach (var primaryKeyPropertyTuple in primaryKeyTuples) {
					var propertyPermission = (bool?)RuleMap.ResolvePropertyCustom(requiredPermission, primaryKeyPropertyTuple.Property.PropertyInfo, context);
					var instancePropertyPermission = (bool?)permissionEntityType?.GetProperty($"{primaryKeyPropertyTuple.Property.Name}Permission")?.GetValue(permissionEntity)
					?? (bool?)RuleMap.ResolveInstancePropertyCustom(primaryKeyPropertyTuple.Property.PropertyInfo, Permission.Read, context, Entity);
					if (instancePropertyPermission != null && !instancePropertyPermission.Value) {
					}
					if (instancePropertyPermission == null && (globalPermission == null || !globalPermission.Value) && (propertyPermission == null || !propertyPermission.Value)) {
						throw new UnauthorizedAccessException($"You do not have permission to {Enum.GetName(typeof(Permission), Permission.Read)} on property {primaryKeyPropertyTuple.Property.Name} on entity type {EntityType.Name}.");
					}
				}

				foreach (var property in PropertyTuples.Select(pt => pt.Property.AsPropertyBase()).Union(ReferenceTuples.Union(CollectionTuples).Select(nt => nt.Navigation.AsPropertyBase()))) {
					var propertyPermission = (bool?)RuleMap.ResolvePropertyCustom(requiredPermission, property.PropertyInfo, context);
					var instancePropertyPermission = (bool?)permissionEntityType?.GetProperty($"{property.Name}Permission")?.GetValue(permissionEntity)
					?? (bool?)RuleMap.ResolveInstancePropertyCustom(property.PropertyInfo, requiredPermission, context, Entity);
					if (instancePropertyPermission != null && !instancePropertyPermission.Value) {
						throw new UnauthorizedAccessException($"You do not have permission to {Enum.GetName(typeof(Permission), requiredPermission)} on property {property.Name} on entity type {EntityType.Name} with key ({string.Join(", ", primaryKeyPairs.Select(kp => $"{kp.Property.Name}={kp.JProperty.Value}"))}).");
					}
					if (instancePropertyPermission == null && (globalPermission == null || !globalPermission.Value) && (propertyPermission == null || !propertyPermission.Value)) {
						throw new UnauthorizedAccessException($"You do not have permission to {Enum.GetName(typeof(Permission), requiredPermission)} on property {property.Name} on entity type {EntityType.Name}.");
					}
				}

				foreach (var reference in References.Values.Where(reference => reference.TargetEntityMutationInfo != null)) {
					reference.TargetEntityMutationInfo.CheckPermissions(context, permissionEntityType?.GetProperty(reference.Navigation.Name)?.GetValue(permissionEntity), cache);
				}

				foreach (var collectionMutationInfo in Collections.Values) {
					if (permissionEntity != null) {
						var collection = (object[])MutationContext.PermissionEntityTypeBuilder.PropertyMap[collectionMutationInfo.Navigation].GetValue(permissionEntity);
						if (collection != null) {
							var index = 0;
							foreach (var itemMutationInfo in collectionMutationInfo.TargetEntityMutationInfos) {
								if (itemMutationInfo.State != EntityState.Added) {
									itemMutationInfo.CheckPermissions(context, collection[index], cache);
									index++;
								}
							}
						}
					}
				}
			}
		}
		public async Task PrepareEntry(EntityEntry entry, ISet<EntityMutationInfo> cache) {
			if (!(IsReference || cache.Contains(this))) {
				cache.Add(this);
				entry.State = State;
				var isGeneratedPrimaryKey = PrimaryKey.Properties[0].ValueGenerated.HasFlag(ValueGenerated.OnAdd);
				var unwantedProperties = Properties.Values.Where(property => {
					return (property.Property.ValueGenerated.HasFlag(ValueGenerated.OnAdd) && property.Property.IsPrimaryKey())
					|| (State != EntityState.Added && property.Property.IsPrimaryKey());
				}).ToArray();
				var touchedProperties = Properties.Values.Where(p => !unwantedProperties.Any(up => up == p)).ToArray();
				foreach (var property in touchedProperties) {
					entry.Property(property.Property.Name).IsModified = property.IsModified;
				}
				foreach (var referenceMutationInfo in References.Values.Where(r => r.IsModified)) {
					var reference = entry.Reference(referenceMutationInfo.Navigation.Name);
					reference.IsModified = referenceMutationInfo.IsModified;
					if (reference.TargetEntry != null) await referenceMutationInfo.TargetEntityMutationInfo.PrepareEntry(reference.TargetEntry, cache);
				}
				foreach (var collectionMutationInfo in Collections.Values.Where(c => c.IsModified)) {
					var collection = entry.Collection(collectionMutationInfo.Navigation.Name);
					collection.IsModified = collectionMutationInfo.IsModified;
					var index = 0;
					foreach (var item in collectionMutationInfo.TargetEntityMutationInfos) {
						await item.PrepareEntry(collection.FindEntry(collection.CurrentValue.Cast<object>().ElementAt(index)), cache);
						index++;
					}
				}
			}
		}
		public async Task<ActionResult> PrePersistAction(EntityEntry entry, ISet<EntityMutationInfo> cache) {
			var actionResult = new ActionResult();
			if (!(IsReference || cache.Contains(this))) {
				cache.Add(this);
				if (RuleMap != null) {
					switch (State) {
						case (EntityState.Added): { actionResult = await RuleMap.OnBeforeCreation(MutationContext.SecurityContext, Entity, entry, actionResult); break; }
						case (EntityState.Modified): { actionResult = await RuleMap.OnBeforeEdition(MutationContext.SecurityContext, Entity, entry, actionResult); break; }
						case (EntityState.Deleted): { actionResult = await RuleMap.OnBeforeDeletion(MutationContext.SecurityContext, Entity, entry, actionResult); break; }
					}
				}
				foreach (var reference in References.Values.Where(reference => reference.TargetEntityMutationInfo != null)) {
					var targetEntry = entry.Reference(reference.Navigation.Name).TargetEntry;
					if (targetEntry != null) {
						await reference.TargetEntityMutationInfo.PrePersistAction(targetEntry, cache);
					}
				}
				foreach (var collectionMutationInfo in Collections.Values) {
					var index = 0;
					var collection = (IEnumerable<object>)collectionMutationInfo.Navigation.GetGetter().GetClrValue(Entity);
					foreach (var targetEntityMutationInfo in collectionMutationInfo.TargetEntityMutationInfos) {
						await targetEntityMutationInfo.PrePersistAction(entry.Collection(collectionMutationInfo.Navigation.Name).FindEntry(collection.ElementAt(index)), cache);
						index++;
					}
				}
			}
			return actionResult;
		}
		public async Task PostPersistAction(EntityEntry entry, ISet<EntityMutationInfo> cache) {
			var actionResult = new ActionResult();
			if (!(IsReference || cache.Contains(this))) {
				cache.Add(this);
				if (RuleMap != null) {
					switch (State) {
						case (EntityState.Added): { await RuleMap.OnAfterCreation(MutationContext.SecurityContext, Entity, entry); break; }
						case (EntityState.Modified): { await RuleMap.OnAfterEdition(MutationContext.SecurityContext, Entity, entry); break; }
						case (EntityState.Deleted): { await RuleMap.OnAfterDeletion(MutationContext.SecurityContext, Entity, entry); break; }
					}
				}
				foreach (var reference in References.Values.Where(reference => reference.TargetEntityMutationInfo != null)) {
					var targetEntry = entry.Reference(reference.Navigation.Name).TargetEntry;
					if (targetEntry != null) {
						await reference.TargetEntityMutationInfo.PostPersistAction(targetEntry, cache);
					}
				}
				foreach (var collectionMutationInfo in Collections.Values) {
					var index = 0;
					var collection = (IEnumerable<object>)collectionMutationInfo.Navigation.GetGetter().GetClrValue(Entity);
					foreach (var targetEntityMutationInfo in collectionMutationInfo.TargetEntityMutationInfos) {
						if (targetEntityMutationInfo.State != EntityState.Deleted) {
							await targetEntityMutationInfo.PostPersistAction(entry.Collection(collectionMutationInfo.Navigation.Name).FindEntry(collection.ElementAt(index)), cache);
							index++;
						}
					}
				}
			}
		}
	}
}
