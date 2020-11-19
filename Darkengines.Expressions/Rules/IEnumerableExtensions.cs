using Darkengines.Expressions.Mutation;
using Darkengines.Expressions.Security;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Darkengines.Expressions.Rules {
	public static class IEnumerableExtensions {
		public static IRuleMap BuildRuleMap(this IEnumerable<IRuleMap> ruleMaps) {
			return ruleMaps != null && ruleMaps.Any() ? (ruleMaps.Skip(1).Any() ? new AggregatedRuleMap(ruleMaps) : ruleMaps.First()) : null;
		}
	}

	public class AggregatedRuleMap : IRuleMap {

		protected IEnumerable<IRuleMap> RuleMaps { get; }
		public AggregatedRuleMap(IEnumerable<IRuleMap> ruleMaps) {
			RuleMaps = ruleMaps;
		}
		public Permission PropertiesDefaultPermission {
			get {
				return RuleMaps.Aggregate(Permission.Read, (result, ruleMap) => {
					return result & ruleMap.PropertiesDefaultPermission;
				});
			}
		}
		public bool RequireProjection {
			get {
				var result = RuleMaps.Aggregate(false, (requiresProjection, ruleMap) => requiresProjection || ruleMap.RequireProjection);
				return result;
			}
		}

		public bool ShouldApplyPermission => RuleMaps.Aggregate(true, (result, ruleMap) => result && ruleMap.ShouldApplyPermission);

		public bool HasAnyInstancePermissionResolverExpression => RuleMaps.Aggregate(false, (result, ruleMap) => result || ruleMap.HasAnyInstancePermissionResolverExpression);

		public bool CanHandle(Type type, object context) {
			return false;
		}

		public Expression GetDefaultProjectionExpression(object context, Expression argumentExpression, IEnumerable<IRuleMap> ruleMaps) {
			var result = RuleMaps.First().GetDefaultProjectionExpression(context, argumentExpression, ruleMaps);
			return result;
		}

		public Expression GetInstancePermissionResolverExpression(object context, Expression instanceExpression) {
			var first = RuleMaps.First();
			var composedExpression = RuleMaps.Skip(1).Aggregate(first.GetInstancePermissionResolverExpression(context, instanceExpression), (result, ruleMap) => {
				var expression = ruleMap.GetInstancePermissionResolverExpression(context, instanceExpression);
				if (result == null && expression == null) return null;
				if (result == null) return expression;
				if (expression == null) return result;
				return Expression.Or(Expression.Convert(result, typeof(int)), Expression.Convert(expression, typeof(int)));
			});
			return composedExpression;
		}

		public Expression GetInstanceCustomResolverExpression(object context, Expression instanceExpression, object key) {
			var first = RuleMaps.First();
			var composedExpression = RuleMaps.Skip(1).Aggregate(first.GetInstanceCustomResolverExpression(context, instanceExpression, key), (result, ruleMap) => {
				var expression = ruleMap.GetInstanceCustomResolverExpression(context, instanceExpression, key);
				if (result == null && expression == null) return null;
				if (result == null) return expression;
				if (expression == null) return result;
				return ruleMap.GetInstanceCustomExpressionReducer(key)(result, expression);
			});
			return composedExpression;
		}

		public bool HasAnyInstancePropertyPermissionResolverExpression(PropertyInfo propertyInfo, object key) {
			return RuleMaps.Aggregate(false, (result, ruleMap) => result || ruleMap.HasAnyInstancePropertyPermissionResolverExpression(propertyInfo, key));
		}

		public async Task OnAfterCreation(object context, object instance, EntityEntry entry, EntityMutationInfo entityMutationInfo) {
			foreach (var ruleMap in RuleMaps) {
				await ruleMap.OnAfterCreation(context, instance, entry, entityMutationInfo);
			}
		}

		public async Task OnAfterDeletion(object context, object instance, EntityEntry entry) {
			foreach (var ruleMap in RuleMaps) {
				await ruleMap.OnAfterDeletion(context, instance, entry);
			}
		}

		public async Task OnAfterEdition(object context, object instance, EntityEntry entry) {
			foreach (var ruleMap in RuleMaps) {
				await ruleMap.OnAfterEdition(context, instance, entry);
			}
		}

		public async Task<ActionResult> OnBeforeCreation(object context, object instance, EntityEntry entry, ActionResult actionResult) {
			foreach (var ruleMap in RuleMaps) {
				actionResult = await ruleMap.OnBeforeCreation(context, instance, entry, actionResult);
			}
			return actionResult;
		}

		public async Task<ActionResult> OnBeforeDeletion(object context, object instance, EntityEntry entry, ActionResult actionResult) {
			foreach (var ruleMap in RuleMaps) {
				actionResult = await ruleMap.OnBeforeDeletion(context, instance, entry, actionResult);
			}
			return actionResult;
		}

		public async Task<ActionResult> OnBeforeEdition(object context, object instance, EntityEntry entry, ActionResult actionResult) {
			foreach (var ruleMap in RuleMaps) {
				actionResult = await ruleMap.OnBeforeEdition(context, instance, entry, actionResult);
			}
			return actionResult;
		}

		public Permission? ResolveInstancePermission(object context, object instance) {
			return RuleMaps.Aggregate((Permission?)null, (result, ruleMap) => {
				if (result == null && ruleMap == null) return null;
				return result ?? Permission.None | (ruleMap.ResolveInstancePermission(context, instance) ?? Permission.None);
			});
		}

		public Permission? ResolveMethodPermission(MethodInfo methodInfo, object context, params Type[] genericArguments) {
			return RuleMaps.Aggregate((Permission?)null, (result, ruleMap) => {
				if (result == null && ruleMap == null) return null;
				return result ?? Permission.None | (ruleMap.ResolveMethodPermission(methodInfo, context, genericArguments) ?? Permission.None);
			});
		}

		public Permission? ResolveTypePermission(object context) {
			return RuleMaps.Aggregate((Permission?)null, (result, ruleMap) => {
				if (result == null && ruleMap == null) return null;
				return result ?? Permission.None | (ruleMap.ResolveTypePermission(context) ?? Permission.None);
			});
		}

		public bool ShouldProjectForMethod(MethodInfo methodInfo, object context, Type[] genericArguments) {
			return RuleMaps.Aggregate(true, (result, ruleMap) => result && ruleMap.ShouldProjectForMethod(methodInfo, context, genericArguments));
		}

		public bool ShouldFilterForMethod(MethodInfo methodInfo, object context, Type[] genericArguments) {
			return RuleMaps.Aggregate(true, (result, ruleMap) => result && ruleMap.ShouldFilterForMethod(methodInfo, context, genericArguments));
		}
		public bool ShouldFilterForProperty(PropertyInfo propertyInfo, object context) {
			return RuleMaps.Aggregate(true, (result, ruleMap) => result && ruleMap.ShouldFilterForProperty(propertyInfo, context));
		}
		public Func<Expression, Expression, Expression> GetInstanceCustomExpressionReducer(object key) {
			throw new NotImplementedException();
		}

		public Expression ResolveInstancePropertyCustomResolverExpression(PropertyInfo propertyInfo, object key, object context, Expression instanceExpression) {
			var first = RuleMaps.First();
			return RuleMaps.Skip(1).Aggregate(first.ResolveInstancePropertyCustomResolverExpression(propertyInfo, key, context, instanceExpression), (result, ruleMap) => {
				var expression = ruleMap.ResolveInstancePropertyCustomResolverExpression(propertyInfo, key, context, instanceExpression);
				if (result == null && expression == null) return null;
				if (result == null) return expression;
				if (expression == null) return result;
				return ruleMap.GetInstanceCustomExpressionReducer(key)(result, expression);
			});
		}

		public object ResolveInstanceCustom(object context, object instance, object key) {
			var first = RuleMaps.First();
			var composedExpression = RuleMaps.Skip(1).Aggregate(first.ResolveInstanceCustom(context, instance, key), (result, ruleMap) => {
				var expression = ruleMap.ResolveInstanceCustom(context, instance, key);
				if (result == null && expression == null) return null;
				if (result == null) return expression;
				if (expression == null) return result;
				return ruleMap.GetInstanceCustomReducer(key)(result, expression);
			});
			return composedExpression;
		}

		public object ResolveInstancePropertyCustom(PropertyInfo propertyInfo, object key, object context, object instance) {
			var first = RuleMaps.First();
			return RuleMaps.Skip(1).Aggregate(first.ResolveInstancePropertyCustom(propertyInfo, key, context, instance), (result, ruleMap) => {
				var expression = ruleMap.ResolveInstancePropertyCustom(propertyInfo, key, context, instance);
				if (result == null && expression == null) return null;
				if (result == null) return expression;
				if (expression == null) return result;
				return GetInstanceCustomReducer(key)(result, expression);
			});
		}

		public Func<object, object, object> GetInstanceCustomReducer(object key) {
			throw new NotImplementedException();
		}

		public object ResolveTypeCustom(object key, object context) {
			var first = RuleMaps.First();
			var composedExpression = RuleMaps.Skip(1).Aggregate(first.ResolveTypeCustom(key, context), (result, ruleMap) => {
				var expression = ruleMap.ResolveTypeCustom(key, context);
				if (result == null && expression == null) return null;
				if (result == null) return expression;
				if (expression == null) return result;
				return GetInstanceCustomReducer(key)(result, expression);
			});
			return composedExpression;
		}

		public object ResolvePropertyCustom(object key, PropertyInfo propertyInfo, object context) {
			var first = RuleMaps.First();
			return RuleMaps.Skip(1).Aggregate(first.ResolvePropertyCustom(key, propertyInfo, context), (result, ruleMap) => {
				var expression = ruleMap.ResolvePropertyCustom(key, propertyInfo, context);
				if (result == null && expression == null) return null;
				if (result == null) return expression;
				if (expression == null) return result;
				return GetInstanceCustomReducer(key)(result, expression);
			});
		}
	}
}
