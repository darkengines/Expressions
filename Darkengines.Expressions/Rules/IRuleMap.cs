using Darkengines.Expressions.Security;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Darkengines.Expressions.Rules {
	public interface IRuleMap {
		bool CanHandle(Type type, object context);
		Permission? ResolveTypePermission(object context);
		object ResolveTypeCustom(object key, object context);
		bool RequireProjection { get; }
		bool ShouldApplyPermission { get; }
		bool HasAnyInstancePermissionResolverExpression { get; }
		bool HasAnyInstancePropertyPermissionResolverExpression(PropertyInfo propertyInfo, object key);
		Permission? ResolveInstancePermission(object context, object instance);
		Expression GetInstancePermissionResolverExpression(object context, Expression instanceExpression);
		object ResolvePropertyCustom(object key, PropertyInfo propertyInfo, object context);
		Expression ResolveInstancePropertyCustomResolverExpression(PropertyInfo propertyInfo, object key, object context, Expression instanceExpression);
		Permission? ResolveMethodPermission(MethodInfo methodInfo, object context, params Type[] genericArguments);
		Task<ActionResult> OnBeforeCreation(object context, object instance, EntityEntry entry, ActionResult actionResult);
		Task OnAfterCreation(object context, object instance, EntityEntry entry);
		Task<ActionResult> OnBeforeDeletion(object context, object instance, EntityEntry entry, ActionResult actionResult);
		Task OnAfterDeletion(object context, object instance, EntityEntry entry);
		Task<ActionResult> OnBeforeEdition(object context, object instance, EntityEntry entry, ActionResult actionResult);
		Task OnAfterEdition(object context, object instance, EntityEntry entry);
		bool ShouldProjectForMethod(MethodInfo methodInfo, object context, Type[] genericArguments);
		bool ShouldFilterForMethod(MethodInfo methodInfo, object context, Type[] genericArguments);
		bool ShouldFilterForProperty(PropertyInfo propertyInfo, object context);
		Expression GetInstanceCustomResolverExpression(object context, Expression instanceExpression, object key);
		Func<Expression, Expression, Expression> GetInstanceCustomExpressionReducer(object key);
		Func<object, object, object> GetInstanceCustomReducer(object key);
		object ResolveInstanceCustom(object context, object instance, object key);
		object ResolveInstancePropertyCustom(PropertyInfo propertyInfo, object key, object context, object instance);
	}
}
