using Darkengines.Expressions.Mutation;
using Darkengines.Expressions.Security;
using DarkEngines.Expressions;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Darkengines.Expressions.Rules {
	public class RuleMap<TContext, T> : IRuleMap where T : class {
		protected AnonymousTypeBuilder AnonymousTypeBuilder { get; }
		protected IModel Model { get; }
		public bool ShouldApplyPermission { get; } = true;
		public Permission PropertiesDefaultPermission { get; set; } = Permission.Read;
		public RuleMap(AnonymousTypeBuilder anonymousTypeBuilder, IModel model) {
			PropertyOptions = new Dictionary<PropertyInfo, PropertyOptions<TContext, T>>(new PropertyInfoHandleComparer());
			TypePermissionResolvers = new Collection<Func<TContext, Permission>>();
			TypeCustomResolvers = new Dictionary<object, ICollection<Func<TContext, object>>>();
			InstancePermissionResolvers = new Collection<Func<TContext, T, Permission>>();
			InstancePermissionResolverExpressions = new Collection<Expression<Func<TContext, T, Permission>>>();
			InstanceCustomResolverExpressions = new Dictionary<object, ICollection<Expression<Func<TContext, T, object>>>>();
			InstanceCustomResolverExpressionReducers = new Dictionary<object, Func<Expression, Expression, Expression>>();
			InstanceCustomResolvers = new Dictionary<object, ICollection<Func<TContext, T, object>>>();
			InstanceCustomResolverReducers = new Dictionary<object, Func<object, object, object>>();
			MethodPermissionResolvers = new ParameterizedGenericMethodInfoRuleRegistry();
			AnonymousTypeBuilder = anonymousTypeBuilder;
			PreCreationActions = new Collection<Func<TContext, T, EntityEntry, ActionResult, Task<ActionResult>>>();
			PostCreationActions = new Collection<Func<TContext, T, EntityEntry, EntityMutationInfo, Task>>();
			PreEditionActions = new Collection<Func<TContext, T, EntityEntry, ActionResult, Task<ActionResult>>>();
			PostEditionActions = new Collection<Func<TContext, T, EntityEntry, Task>>();
			PreDeletionActions = new Collection<Func<TContext, T, EntityEntry, ActionResult, Task<ActionResult>>>();
			PostDeletionActions = new Collection<Func<TContext, T, EntityEntry, Task>>();
			Model = model;
			InstanceOptions = new InstanceOptions<TContext, T>();
		}
		public IDictionary<PropertyInfo, PropertyOptions<TContext, T>> PropertyOptions { get; }
		public InstanceOptions<TContext, T> InstanceOptions { get; set; }
		public virtual bool RequireProjection { get; } = true;
		public virtual bool CanHandle(Type type, TContext context) => typeof(T).IsAssignableFrom(type);
		bool IRuleMap.CanHandle(Type type, object context) => CanHandle(type, (TContext)context);

		public virtual bool HasAnyInstancePermissionResolverExpression {
			get {
				return InstanceCustomResolverExpressions.Any(tuple => tuple.Key is Permission);
			}
		}

		public virtual bool HasAnyInstancePropertyPermissionResolverExpression(PropertyInfo propertyInfo, object key) {
			return PropertyOptions.ContainsKey(propertyInfo) && PropertyOptions[propertyInfo].InstancePropertyCustomResolverExpressions.ContainsKey(key);
		}

		protected MemberInitExpression DefaultProjectionExpression { get; set; }
		public RuleMap<TContext, T> HasDefaultProjectionExpression(Expression<Func<T, object>> projectionExpression) {
			DefaultProjectionExpression = ((MemberInitExpression)projectionExpression.Body);
			return this;
		}
		public virtual Expression GetDefaultProjectionExpression(TContext context, Expression argumentExpression, IEnumerable<IRuleMap> ruleMaps) {
			if (DefaultProjectionExpression != null) return DefaultProjectionExpression;
			var entityType = Model.FindEntityType(typeof(T).FullName);
			IEnumerable<PropertyInfo> propertyInfos;
			if (entityType != null) {
				var entityProperties = entityType.GetProperties();
				propertyInfos = entityProperties.Where(entityProperty => {
					ICollection<Func<TContext, object>> resolvers;
					PropertyOptions<TContext, T> options;
					return !PropertyOptions.TryGetValue(entityProperty.PropertyInfo, out options)
					|| options.InstancePropertyCustomResolverExpressions.ContainsKey(Permission.Read)
					|| !options.PropertyCustomResolvers.TryGetValue(Permission.Read, out resolvers)
					|| resolvers.Any(resolver => (bool)resolver(context));
				}).Select(entityProperty => entityProperty.PropertyInfo);
			} else {
				propertyInfos = typeof(T).GetProperties().Where(property => property.PropertyType.IsValueType || property.PropertyType.IsPrimitive);
			}
			var anonymousType = AnonymousTypeBuilder.BuildAnonymousType(propertyInfos.Select(propertyInfo => (propertyInfo.PropertyType, propertyInfo.Name).ToTuple()).ToHashSet(), $"{entityType?.Name ?? typeof(T).Name}Projection{Guid.NewGuid()}");
			var newExpression = Expression.New(anonymousType.GetConstructor(new Type[0]));
			argumentExpression = Expression.MemberInit(newExpression, propertyInfos.Join(anonymousType.GetProperties(), propertyInfo => propertyInfo.Name, pi => pi.Name, (propertyInfo, pi) => {
				var propertyRuleMap = ruleMaps.Where(p => p.CanHandle(propertyInfo.PropertyType, context)).BuildRuleMap();
				var predicateExpression = ResolveInstancePropertyCustomResolverExpression(propertyInfo, Permission.Read, context, argumentExpression);
				if (predicateExpression != null && predicateExpression.Type != typeof(bool)) predicateExpression = Expression.Convert(predicateExpression, typeof(bool));
				var rightHandExpression = (Expression)Expression.MakeMemberAccess(argumentExpression, propertyInfo);
				if (predicateExpression != null) rightHandExpression = Expression.Condition(predicateExpression, rightHandExpression, Expression.Convert(Expression.Constant(propertyInfo.PropertyType.IsValueType ? Activator.CreateInstance(propertyInfo.PropertyType) : null), rightHandExpression.Type));
				if (propertyRuleMap != null && propertyRuleMap.RequireProjection) {
					rightHandExpression = propertyRuleMap.GetDefaultProjectionExpression(context, rightHandExpression, ruleMaps);
				}
				return Expression.Bind(pi, rightHandExpression);
			}));
			return argumentExpression;
		}
		Expression IRuleMap.GetDefaultProjectionExpression(object context, Expression argumentExpression, IEnumerable<IRuleMap> ruleMaps) {
			return GetDefaultProjectionExpression((TContext)context, argumentExpression, ruleMaps);
		}

		protected IDictionary<object, Func<Expression, Expression, Expression>> InstanceCustomResolverExpressionReducers { get; }
		private static Func<Expression, Expression, Expression> DefaultExpressionReducer = new Func<Expression, Expression, Expression>((previous, current) => Expression.OrElse(Expression.Convert(previous, typeof(bool)), Expression.Convert(current, typeof(bool))));
		protected IDictionary<object, ICollection<Expression<Func<TContext, T, object>>>> InstanceCustomResolverExpressions { get; }
		public RuleMap<TContext, T> HasInstanceCustomResolverExpressionReducer<TResolved>(object key, Func<Expression, Expression, Expression> reducer = null) {
			InstanceCustomResolverExpressionReducers[key] = reducer;
			return this;
		}
		public RuleMap<TContext, T> HasInstanceCustomResolverExpression<TResolved>(object key, Expression<Func<TContext, T, TResolved>> resolverExpression) {
			ICollection<Expression<Func<TContext, T, object>>> resolvers = null;
			if (!InstanceCustomResolverExpressions.TryGetValue(key, out resolvers)) {
				InstanceCustomResolverExpressions[key] = resolvers = new Collection<Expression<Func<TContext, T, object>>>();
			}
			resolvers.Add(Expression.Lambda<Func<TContext, T, object>>(Expression.Convert(resolverExpression.Body, typeof(object)), resolverExpression.Parameters));
			return this;
		}
		public Expression GetInstanceCustomResolverExpression(TContext context, Expression instanceExpression, object key) {
			ICollection<Expression<Func<TContext, T, object>>> resolvers = null;
			if (!InstanceCustomResolverExpressions.TryGetValue(key, out resolvers) || !resolvers.Any()) return null;
			var generatedResolvers = resolvers.Select(resolver => {
				var contextParameter = resolver.Parameters[0];
				var entityParameter = resolver.Parameters[1];
				var replacementExpressionVisitor = new ReplacementExpressionVisitor(new Dictionary<Expression, Expression>() {
					{ contextParameter, Expression.Constant(context) },
					{ entityParameter, instanceExpression }
				});
				return replacementExpressionVisitor.Visit(resolver.Body);
			});

			var firstExpression = generatedResolvers.First();
			if (!generatedResolvers.Skip(1).Any()) return firstExpression;
			Func<Expression, Expression, Expression> reducer = null;
			if (!InstanceCustomResolverExpressionReducers.TryGetValue(key, out reducer)) reducer = DefaultExpressionReducer;
			return generatedResolvers.Skip(1).Aggregate(firstExpression, (expression, resolver) => reducer(expression, resolver));
		}
		Expression IRuleMap.GetInstanceCustomResolverExpression(object context, Expression instanceExpression, object key) {
			return GetInstanceCustomResolverExpression((TContext)context, instanceExpression, key);
		}
		public Func<Expression, Expression, Expression> GetInstanceCustomExpressionReducer(object key) {
			Func<Expression, Expression, Expression> reducer = null;
			if (!InstanceCustomResolverExpressionReducers.TryGetValue(key, out reducer)) reducer = DefaultExpressionReducer;
			return reducer;
		}
		Func<Expression, Expression, Expression> IRuleMap.GetInstanceCustomExpressionReducer(object key) {
			return GetInstanceCustomExpressionReducer(key);
		}

		protected ICollection<Expression<Func<TContext, T, Permission>>> InstancePermissionResolverExpressions { get; }
		public Expression GetInstancePermissionResolverExpression(TContext context, Expression instanceExpression) {
			if (!InstancePermissionResolverExpressions.Any()) return null;
			var generatedResolvers = InstancePermissionResolverExpressions.Select(resolver => {
				var contextParameter = resolver.Parameters[0];
				var entityParameter = resolver.Parameters[1];
				var replacementExpressionVisitor = new ReplacementExpressionVisitor(new Dictionary<Expression, Expression>() {
					{ contextParameter, Expression.Constant(context) },
					{ entityParameter, instanceExpression }
				});
				return replacementExpressionVisitor.Visit(resolver.Body);
			});

			var firstExpression = generatedResolvers.First();
			if (!generatedResolvers.Skip(1).Any()) return firstExpression;
			return generatedResolvers.Skip(1).Aggregate(firstExpression, (expression, resolver) => Expression.And(expression, resolver));
		}
		Expression IRuleMap.GetInstancePermissionResolverExpression(object context, Expression instanceExpression) {
			return GetInstancePermissionResolverExpression((TContext)context, instanceExpression);
		}

		public RuleMap<TContext, T> HasInstancePropertyCustomResolverExpression(object key, PropertyInfo propertyInfo, Expression<Func<TContext, T, object>> permissionResolver) {
			PropertyOptions<TContext, T> options = null;
			if (!PropertyOptions.TryGetValue(propertyInfo, out options)) {
				PropertyOptions[propertyInfo] = options = new PropertyOptions<TContext, T>();
			}
			ICollection<Expression<Func<TContext, T, object>>> resolvers;
			if (!options.InstancePropertyCustomResolverExpressions.TryGetValue(key, out resolvers)) {
				options.InstancePropertyCustomResolverExpressions[key] = resolvers = new Collection<Expression<Func<TContext, T, object>>>();
			}
			resolvers.Add(permissionResolver);
			return this;
		}
		public Expression ResolveInstancePropertyCustomResolverExpression(PropertyInfo propertyInfo, object key, TContext context, Expression instanceExpression) {
			PropertyOptions<TContext, T> options;
			if (!PropertyOptions.TryGetValue(propertyInfo, out options)) return null;
			ICollection<Expression<Func<TContext, T, object>>> resolvers;
			if (!options.InstancePropertyCustomResolverExpressions.TryGetValue(key, out resolvers) || !resolvers.Any()) return null;

			var generatedResolvers = resolvers.Select(resolver => {
				var contextParameter = resolver.Parameters[0];
				var entityParameter = resolver.Parameters[1];
				var replacementExpressionVisitor = new ReplacementExpressionVisitor(new Dictionary<Expression, Expression>() {
					{ contextParameter, Expression.Constant(context) },
					{ entityParameter, instanceExpression }
				});
				return replacementExpressionVisitor.Visit(resolver.Body);
			});

			var firstExpression = generatedResolvers.First();
			if (!generatedResolvers.Skip(1).Any()) return firstExpression;
			Func<Expression, Expression, Expression> reducer = null;
			if (!InstanceCustomResolverExpressionReducers.TryGetValue(key, out reducer)) reducer = DefaultExpressionReducer;
			return generatedResolvers.Skip(1).Aggregate(firstExpression, reducer);
		}
		Expression IRuleMap.ResolveInstancePropertyCustomResolverExpression(PropertyInfo propertyInfo, object key, object context, Expression instanceExpression) {
			return ResolveInstancePropertyCustomResolverExpression(propertyInfo, key, (TContext)context, instanceExpression);
		}
		protected ICollection<Func<TContext, T, Permission>> InstancePermissionResolvers { get; }
		public Permission? ResolveInstancePermission(TContext context, T instance) {
			if (!InstancePermissionResolvers.Any()) return null;
			return InstancePermissionResolvers.Aggregate(Permission.Admin, (permission, resolver) => permission & resolver(context, instance));
		}
		Permission? IRuleMap.ResolveInstancePermission(object context, object instance) {
			return ResolveInstancePermission((TContext)context, (T)instance);
		}

		protected IDictionary<object, Func<object, object, object>> InstanceCustomResolverReducers { get; }
		private static Func<object, object, object> DefaultReducer = new Func<object, object, object>((previous, current) => (bool)previous || (bool)current);
		public RuleMap<TContext, T> HasInstanceCustomResolverReducer<TResolved>(object key, Func<object, object, object> reducer = null) {
			InstanceCustomResolverReducers[key] = reducer;
			return this;
		}
		protected IDictionary<object, ICollection<Func<TContext, T, object>>> InstanceCustomResolvers { get; }
		public RuleMap<TContext, T> HasInstanceCustomResolver<TResolved>(object key, Func<TContext, T, TResolved> resolver) {
			ICollection<Func<TContext, T, object>> resolvers = null;
			if (!InstanceCustomResolvers.TryGetValue(key, out resolvers)) {
				InstanceCustomResolvers[key] = resolvers = new Collection<Func<TContext, T, object>>();
			}
			resolvers.Add(new Func<TContext, T, object>((context, instance) => resolver(context, instance)));
			return this;
		}
		public object ResolveInstanceCustom(TContext context, T instance, object key) {
			ICollection<Func<TContext, T, object>> resolvers = null;
			if (!InstanceCustomResolvers.TryGetValue(key, out resolvers) || !resolvers.Any()) return null;
			var firstResolver = resolvers.First();
			if (!resolvers.Skip(1).Any()) return firstResolver(context, instance);
			Func<object, object, object> reducer = null;
			if (!InstanceCustomResolverReducers.TryGetValue(key, out reducer)) reducer = DefaultReducer;
			return resolvers.Skip(1).Aggregate(firstResolver(context, instance), (expression, resolver) => reducer(expression, resolver(context, instance)));
		}
		object IRuleMap.ResolveInstanceCustom(object context, object instance, object key) {
			return ResolveInstanceCustom((TContext)context, (T)instance, key);
		}

		public RuleMap<TContext, T> HasInstancePropertyCustomResolver(object key, PropertyInfo propertyInfo, Func<TContext, T, object> resolver) {
			PropertyOptions<TContext, T> options = null;
			if (!PropertyOptions.TryGetValue(propertyInfo, out options)) {
				PropertyOptions[propertyInfo] = options = new PropertyOptions<TContext, T>();
			}
			ICollection<Func<TContext, T, object>> resolvers;
			if (!options.InstancePropertyCustomResolvers.TryGetValue(key, out resolvers)) {
				options.InstancePropertyCustomResolvers[key] = resolvers = new Collection<Func<TContext, T, object>>();
			}
			resolvers.Add(resolver);
			return this;
		}
		public object ResolveInstancePropertyCustom(PropertyInfo propertyInfo, object key, TContext context, T instance) {
			PropertyOptions<TContext, T> options;
			if (!PropertyOptions.TryGetValue(propertyInfo, out options)) return null;
			ICollection<Func<TContext, T, object>> resolvers;
			if (!options.InstancePropertyCustomResolvers.TryGetValue(key, out resolvers) || !resolvers.Any()) return null;

			var firstResolver = resolvers.First();
			if (!resolvers.Skip(1).Any()) return firstResolver(context, instance);
			Func<object, object, object> reducer = null;
			if (!InstanceCustomResolverReducers.TryGetValue(key, out reducer)) reducer = DefaultReducer;
			return resolvers.Skip(1).Aggregate(firstResolver(context, instance), (resolved, resolver) => reducer(resolved, resolver(context, instance)));
		}
		object IRuleMap.ResolveInstancePropertyCustom(PropertyInfo propertyInfo, object key, object context, object instance) {
			return ResolveInstancePropertyCustom(propertyInfo, key, (TContext)context, (T)instance);
		}

		protected ParameterizedGenericMethodInfoRuleRegistry MethodPermissionResolvers { get; set; }
		public RuleMap<TContext, T> DisableProjectionForMethod<TMethod>(Expression<Func<T, TMethod>> methodAccessExpression, Type[] genericArguments) {
			var methodInfo = ExpressionHelper.ExtractMethodInfo(methodAccessExpression);
			if (methodInfo.IsGenericMethod) methodInfo = methodInfo.GetGenericMethodDefinition();
			return DisableProjectionForMethod(methodInfo, genericArguments);
		}
		public RuleMap<TContext, T> DisableProjectionForMethod(MethodInfo methodInfo, Type[] genericArguments) {
			var options = MethodPermissionResolvers[(methodInfo, genericArguments)];
			if (options == null) MethodPermissionResolvers[(methodInfo, genericArguments)] = (options = new MethodInfoOptions(methodInfo, genericArguments));
			options.ShouldProject = false;
			return this;
		}
		public RuleMap<TContext, T> DisableFilterForMethod<TInstance, TMethod>(Expression<Func<TInstance, TMethod>> methodAccessExpression, Type[] genericArguments) {
			var methodInfo = ExpressionHelper.ExtractMethodInfo(methodAccessExpression);
			if (methodInfo.IsGenericMethod) methodInfo = methodInfo.GetGenericMethodDefinition();
			return DisableFilterForMethod(methodInfo, genericArguments);
		}
		public RuleMap<TContext, T> DisableFilterForMethod<TMethod>(Expression<Func<T, TMethod>> methodAccessExpression, Type[] genericArguments) {
			var methodInfo = ExpressionHelper.ExtractMethodInfo(methodAccessExpression);
			if (methodInfo.IsGenericMethod) methodInfo = methodInfo.GetGenericMethodDefinition();
			return DisableFilterForMethod(methodInfo, genericArguments);
		}
		public RuleMap<TContext, T> DisableFilterForMethod(MethodInfo methodInfo, Type[] genericArguments) {
			var options = MethodPermissionResolvers[(methodInfo, genericArguments)];
			if (options == null) MethodPermissionResolvers[(methodInfo, genericArguments)] = (options = new MethodInfoOptions(methodInfo, genericArguments));
			options.ShouldFilter = false;
			return this;
		}
		public RuleMap<TContext, T> DisableFilterForProperty(Expression<Func<T, object>> propertyAccessExpression) {
			var propertyInfo = ExpressionHelper.ExtractPropertyInfo(propertyAccessExpression);
			return DisableFilterForProperty(propertyInfo);
		}
		public RuleMap<TContext, T> DisableFilterForProperty(PropertyInfo propertyInfo) {
			PropertyOptions<TContext, T> options;
			if (!PropertyOptions.TryGetValue(propertyInfo, out options)) {
				PropertyOptions[propertyInfo] = options = new PropertyOptions<TContext, T>();
			}
			options.ApplyFilter = false;
			return this;
		}
		public bool ShouldProjectForMethod(MethodInfo methodInfo, TContext context, params Type[] genericArguments) {
			return MethodPermissionResolvers[(methodInfo, genericArguments)]?.ShouldProject ?? true;
		}
		bool IRuleMap.ShouldProjectForMethod(MethodInfo methodInfo, object context, Type[] genericArguments) {
			return ShouldProjectForMethod(methodInfo, (TContext)context, genericArguments);
		}
		public bool ShouldFilterForMethod(MethodInfo methodInfo, TContext context, params Type[] genericArguments) {
			return MethodPermissionResolvers[(methodInfo, genericArguments)]?.ShouldFilter ?? true;
		}
		bool IRuleMap.ShouldFilterForMethod(MethodInfo methodInfo, object context, Type[] genericArguments) {
			return ShouldFilterForMethod(methodInfo, (TContext)context, genericArguments);
		}

		public bool ShouldFilterForProperty(PropertyInfo propertyInfo, TContext context) {
			return !PropertyOptions.ContainsKey(propertyInfo) || PropertyOptions[propertyInfo].ApplyFilter;
		}
		bool IRuleMap.ShouldFilterForProperty(PropertyInfo propertyInfo, object context) {
			return ShouldFilterForProperty(propertyInfo, (TContext)context);
		}

		public RuleMap<TContext, T> HasMethodPermissionResolver<TMethod>(Expression<Func<T, TMethod>> methodAccessExpression, Func<TContext, Permission> resolver, params Type[] genericArguments) {
			var methodInfo = ExpressionHelper.ExtractMethodInfo(methodAccessExpression);
			if (methodInfo.IsGenericMethod) methodInfo = methodInfo.GetGenericMethodDefinition();
			return HasMethodPermissionResolver(methodInfo, resolver, genericArguments);
		}
		public RuleMap<TContext, T> HasMethodPermissionResolver<TInstance, TMethod>(Expression<Func<TInstance, TMethod>> methodAccessExpression, Func<TContext, Permission> resolver, params Type[] genericArguments) where TInstance : class {
			var methodInfo = ExpressionHelper.ExtractMethodInfo(methodAccessExpression);
			if (methodInfo.IsGenericMethod) methodInfo = methodInfo.GetGenericMethodDefinition();
			return HasMethodPermissionResolver(methodInfo, resolver, genericArguments);
		}
		public RuleMap<TContext, T> HasMethodPermissionResolver(MethodInfo methodInfo, Func<TContext, Permission> resolver, Type[] genericArguments) {
			var options = new MethodInfoOptions(methodInfo, genericArguments);
			MethodPermissionResolvers[(methodInfo, genericArguments)] = options;
			options.PermissionResolver = (context, mi, ga, instanceType) => resolver((TContext)context);
			return this;
		}
		public Permission? ResolveMethodPermission(MethodInfo methodInfo, TContext context, params Type[] genericArguments) {
			return MethodPermissionResolvers[(methodInfo, genericArguments)]?.PermissionResolver?.Invoke(context, methodInfo, genericArguments, null);
		}
		Permission? IRuleMap.ResolveMethodPermission(MethodInfo methodInfo, object context, params Type[] genericArguments) {
			return ResolveMethodPermission(methodInfo, (TContext)context, genericArguments);
		}

		public RuleMap<TContext, T> HasPropertyCustomResolver(object key, PropertyInfo propertyInfo, Func<TContext, object> resolver) {
			PropertyOptions<TContext, T> options;
			if (!PropertyOptions.TryGetValue(propertyInfo, out options)) {
				PropertyOptions[propertyInfo] = options = new PropertyOptions<TContext, T>();
			}
			ICollection<Func<TContext, object>> resolvers;
			if (!options.PropertyCustomResolvers.TryGetValue(key, out resolvers)) {
				options.PropertyCustomResolvers[key] = resolvers = new Collection<Func<TContext, object>>();
			}
			resolvers.Add(resolver);
			return this;
		}
		public RuleMap<TContext, T> HasPropertyCustomResolver(object key, Expression<Func<T, object>> propertyAccessExpression, Func<TContext, object> resolver) {
			return HasPropertyCustomResolver(key, ExpressionHelper.ExtractPropertyInfo(propertyAccessExpression), resolver);
		}
		public object ResolvePropertyCustom(PropertyInfo propertyInfo, object key, TContext context) {
			PropertyOptions<TContext, T> options;
			if (!PropertyOptions.TryGetValue(propertyInfo, out options)) return null;
			ICollection<Func<TContext, object>> resolvers;
			if (!options.PropertyCustomResolvers.TryGetValue(key, out resolvers) || !resolvers.Any()) return null;

			var firstResolver = resolvers.First();
			if (!resolvers.Skip(1).Any()) return firstResolver(context);
			Func<object, object, object> reducer = null;
			if (!InstanceCustomResolverReducers.TryGetValue(key, out reducer)) reducer = DefaultReducer;
			return resolvers.Skip(1).Aggregate(firstResolver(context), (resolved, resolver) => reducer(resolved, resolver(context)));
		}
		object IRuleMap.ResolvePropertyCustom(object key, PropertyInfo propertyInfo, object context) {
			return ResolvePropertyCustom(propertyInfo, key, (TContext)context);
		}

		protected ICollection<Func<TContext, Permission>> TypePermissionResolvers { get; set; }
		//public RuleMap<TContext, T> HasTypePermissionResolver(Func<TContext, Permission> typePermissionResolver) {
		//	TypePermissionResolvers.Add(typePermissionResolver);
		//	return this;
		//}
		public Permission? ResolveTypePermission(TContext context) {
			if (!TypePermissionResolvers.Any()) return null;
			return TypePermissionResolvers.Aggregate(Permission.Admin, (permission, resolver) => permission & resolver(context));
		}
		Permission? IRuleMap.ResolveTypePermission(object context) {
			return ResolveTypePermission((TContext)context);
		}

		protected IDictionary<object, ICollection<Func<TContext, object>>> TypeCustomResolvers { get; }
		public RuleMap<TContext, T> HasTypeCustomResolver<TResolved>(object key, Func<TContext, TResolved> resolver) {
			ICollection<Func<TContext, object>> resolvers = null;
			if (!TypeCustomResolvers.TryGetValue(key, out resolvers)) {
				TypeCustomResolvers[key] = resolvers = new Collection<Func<TContext, object>>();
			}
			resolvers.Add(new Func<TContext, object>((context) => resolver(context)));
			return this;
		}
		public object ResolveTypeCustom(object key, TContext context) {
			ICollection<Func<TContext, object>> resolvers = null;
			if (!TypeCustomResolvers.TryGetValue(key, out resolvers) || !resolvers.Any()) return null;
			var firstResolver = resolvers.First();
			if (!resolvers.Skip(1).Any()) return firstResolver(context);
			Func<object, object, object> reducer = null;
			if (!InstanceCustomResolverReducers.TryGetValue(key, out reducer)) reducer = DefaultReducer;
			return resolvers.Skip(1).Aggregate(firstResolver(context), (expression, resolver) => reducer(expression, resolver(context)));
		}
		object IRuleMap.ResolveTypeCustom(object key, object context) {
			return ResolveTypeCustom(key, (TContext)context);
		}

		protected ICollection<Func<TContext, T, EntityEntry, ActionResult, Task<ActionResult>>> PreCreationActions;
		public RuleMap<TContext, T> HasPreCreationAction(Func<TContext, T, EntityEntry, ActionResult, Task<ActionResult>> action) {
			PreCreationActions.Add(action);
			return this;
		}
		public async Task<ActionResult> OnBeforeCreation(object context, object instance, EntityEntry entry, ActionResult actionResult) {
			foreach (var action in PreCreationActions) actionResult = await action((TContext)context, (T)instance, entry, actionResult);
			return actionResult;
		}
		protected ICollection<Func<TContext, T, EntityEntry, EntityMutationInfo, Task>> PostCreationActions;
		public RuleMap<TContext, T> HasPostCreationAction(Func<TContext, T, EntityEntry, EntityMutationInfo, Task> action) {
			PostCreationActions.Add(action);
			return this;
		}
		public async Task OnAfterCreation(object context, object instance, EntityEntry entry, EntityMutationInfo entityMutationInfo) {
			foreach (var action in PostCreationActions) await action((TContext)context, (T)instance, entry.Context.Entry((T)instance), entityMutationInfo);
		}
		protected ICollection<Func<TContext, T, EntityEntry, ActionResult, Task<ActionResult>>> PreDeletionActions;
		public RuleMap<TContext, T> HasPreDeletionAction(Func<TContext, T, EntityEntry, ActionResult, Task<ActionResult>> action) {
			PreDeletionActions.Add(action);
			return this;
		}
		public async Task<ActionResult> OnBeforeDeletion(object context, object instance, EntityEntry entry, ActionResult actionResult) {
			foreach (var action in PreDeletionActions) actionResult = await action((TContext)context, (T)instance, entry, actionResult);
			return actionResult;
		}
		protected ICollection<Func<TContext, T, EntityEntry, Task>> PostDeletionActions;
		public RuleMap<TContext, T> HasPostDeletionAction(Func<TContext, T, EntityEntry, Task> action) {
			PostDeletionActions.Add(action);
			return this;
		}
		public async Task OnAfterDeletion(object context, object instance, EntityEntry entry) {
			foreach (var action in PostDeletionActions) await action((TContext)context, (T)instance, entry);
		}
		protected ICollection<Func<TContext, T, EntityEntry, ActionResult, Task<ActionResult>>> PreEditionActions;
		public RuleMap<TContext, T> HasPreEditionAction(Func<TContext, T, EntityEntry, ActionResult, Task<ActionResult>> action) {
			PreEditionActions.Add(action);
			return this;
		}
		public async Task<ActionResult> OnBeforeEdition(object context, object instance, EntityEntry entry, ActionResult actionResult) {
			foreach (var action in PreEditionActions) actionResult = await action((TContext)context, (T)instance, entry, actionResult);
			return actionResult;
		}
		protected ICollection<Func<TContext, T, EntityEntry, Task>> PostEditionActions;
		public RuleMap<TContext, T> HasPostEditionAction(Func<TContext, T, EntityEntry, Task> action) {
			PostEditionActions.Add(action);
			return this;
		}
		public async Task OnAfterEdition(object context, object instance, EntityEntry entry) {
			foreach (var action in PostEditionActions) await action((TContext)context, (T)instance, entry);
		}

		public Func<object, object, object> GetInstanceCustomReducer(object key) {
			Func<object, object, object> reducer = null;
			if (!InstanceCustomResolverReducers.TryGetValue(key, out reducer)) reducer = DefaultReducer;
			return reducer;
		}
		public RuleMap<TContext, T> HasTypePermissionCustomResolver(Permission permission, Func<TContext, bool> resolver) {
			var permissions = Enum.GetValues(typeof(Permission)).Cast<Permission>().Where(value => permission.HasFlag(value));
			foreach (var value in permissions) HasTypeCustomResolver(permission, resolver);
			return this;
		}
		public RuleMap<TContext, T> HasPropertyPermissionCustomResolver(Permission permission, Expression<Func<T, object>> propertyAccessExpression, Func<TContext, bool> resolver) {
			var permissions = Enum.GetValues(typeof(Permission)).Cast<Permission>().Where(value => permission.HasFlag(value));
			foreach (var value in permissions) HasPropertyCustomResolver(value, ExpressionHelper.ExtractPropertyInfo(propertyAccessExpression), new Func<TContext, object>(context => resolver(context)));
			return this;
		}
		public RuleMap<TContext, T> HasInstancePermissionCustomResolver(Permission permission, Func<TContext, T, bool> resolver) {
			var permissions = Enum.GetValues(typeof(Permission)).Cast<Permission>().Where(value => permission.HasFlag(value));
			foreach (var value in permissions) HasInstanceCustomResolver(value, resolver);
			return this;
		}
		public RuleMap<TContext, T> HasInstancePermissionCustomResolverExpression(Expression<Func<TContext, T, bool>> resolver, params Permission[] permissions) {
			foreach (var permission in permissions) HasInstanceCustomResolverExpression(permission, resolver);
			return this;
		}
		public RuleMap<TContext, T> HasInstancePropertyPermissionCustomResolverExpression(Permission permission, Expression<Func<T, object>> propertyAccessExpression, Expression<Func<TContext, T, bool>> resolver) {
			var permissions = Enum.GetValues(typeof(Permission)).Cast<Permission>().Where(value => permission.HasFlag(value));
			foreach (var value in permissions) HasInstancePropertyCustomResolverExpression(
				value,
				ExpressionHelper.ExtractPropertyInfo(propertyAccessExpression),
				Expression.Lambda<Func<TContext, T, object>>(Expression.Convert(resolver.Body, typeof(object)), resolver.Parameters)
			);
			return this;
		}
		public Expression<Func<TRelated, bool>> GetRelationMapPredicateExpression<TRelated>(object key, ParameterExpression relatedParameterExpression, T entity) {
			IDictionary<object, ICollection<Expression>> expressionsMap = null;
			if (!InstanceOptions.RelationMapExpressions.TryGetValue(typeof(TRelated), out expressionsMap)) {
				return null;
			}
			ICollection<Expression> expressions = null;
			if (!expressionsMap.TryGetValue(key, out expressions)) {
				return null;
			}
			var replacementDictionary = new Dictionary<Expression, Expression>();
			var entityExpression = Expression.Constant(entity);
			var replacer = new ReplacementExpressionVisitor(replacementDictionary);
			var predicateExpressions = expressions.Cast<Expression<Func<TRelated, T, bool>>>().Select(predicate => {
				replacementDictionary[predicate.Parameters[0]] = relatedParameterExpression;
				replacementDictionary[predicate.Parameters[1]] = entityExpression;
				return replacer.Visit(predicate.Body);
			}).ToArray();
			var firstPredicate = predicateExpressions.First();
			var tail = predicateExpressions.Skip(1);

			var predicateBody = predicateExpressions.Aggregate(firstPredicate, (predicate, finalPredicate) => {
				return Expression.OrElse(predicate, finalPredicate);
			});

			var lambda = Expression.Lambda<Func<TRelated, bool>>(predicateBody, relatedParameterExpression);
			return lambda;
		}
	}
}
