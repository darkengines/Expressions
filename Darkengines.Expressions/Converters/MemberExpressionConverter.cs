using Darkengines.Expressions.Rules;
using Darkengines.Expressions.Security;
using Esprima.Ast;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Darkengines.Expressions.Converters {
	public class MemberExpressionConverter : ExpressionConverter<Esprima.Ast.MemberExpression> {
		public override Nodes NodeType => Nodes.MemberExpression;
		public override (System.Linq.Expressions.Expression, ExpressionConversionResult) Convert(Esprima.Ast.MemberExpression memberExpression, ExpressionConverterContext context, ExpressionConverterScope scope, bool allowTerminal, params Type[] genericArguments) {
			var objectExpression = context.ExpressionConverterResolver.Convert(memberExpression.Object, context, scope, true);
			var propertyIdentifier = memberExpression.Property.As<Identifier>();
			var propertyInfo = objectExpression.Type.GetProperty(propertyIdentifier.Name) ?? objectExpression.Type.GetProperty(propertyIdentifier.Name.ToPascalCase());
			if (propertyInfo == null) throw new Exception($"Unable to find property ${propertyIdentifier.Name} on type ${objectExpression.Type.Name}");
			if (!context.IsAdmin) {
				var ruleMap = context.RuleMapRegistry.GetRuleMap(objectExpression.Type, context.securityContext);
				if (ruleMap == null) throw new UnauthorizedAccessException($"You do not have access to type { objectExpression.Type }.");
				var typePermission = (bool?)ruleMap.ResolveTypeCustom(Permission.Read, context.securityContext);
				var hasInstancePermissionResolverExpression = ruleMap.HasAnyInstancePermissionResolverExpression;
				var propertyPermission = (bool?)ruleMap.ResolvePropertyCustom(Permission.Read, propertyInfo, context.securityContext);
				var shouldFilter = ruleMap.ShouldFilterForProperty(propertyInfo, context.securityContext);
				var hasInstancePropertyPermissionResolverExpression = ruleMap.HasAnyInstancePropertyPermissionResolverExpression(propertyInfo, Permission.Read);

				var result = new ExpressionConversionResult() {
					ShouldApplyFilter = shouldFilter,
					ShouldApplyProjection = true
				};

				//var hasAccess = typePermission == null && (hasInstancePermissionResolverExpression || (propertyPermission.HasValue && propertyPermission.Value) || hasInstancePropertyPermissionResolverExpression);
				//hasAccess = hasAccess || (typePermission.Value && !(propertyPermission.HasValue && !propertyPermission.Value));
				//hasAccess = hasAccess || (!typePermission.Value && (hasInstancePermissionResolverExpression || (propertyPermission.HasValue && propertyPermission.Value) || hasInstancePropertyPermissionResolverExpression));
				//hasAccess = hasAccess || (!hasInstancePermissionResolverExpression && ((propertyPermission.HasValue && propertyPermission.Value) || hasInstancePropertyPermissionResolverExpression));
				//hasAccess = hasAccess || (hasInstancePermissionResolverExpression && !(propertyPermission.HasValue && !propertyPermission.Value));
				//hasAccess = hasAccess || (!propertyPermission.HasValue && hasInstancePropertyPermissionResolverExpression);

				var hasAccess = ruleMap.PropertiesDefaultPermission == Permission.Read;
				

				var propertyAccessExpression = (System.Linq.Expressions.Expression)System.Linq.Expressions.Expression.MakeMemberAccess(objectExpression, propertyInfo);
				if (hasInstancePropertyPermissionResolverExpression) {
					var instancePropertyPermissionExpression = ruleMap.ResolveInstancePropertyCustomResolverExpression(propertyInfo, Permission.Read, context.securityContext, objectExpression);
					var predicateExpression = instancePropertyPermissionExpression;
					if (predicateExpression != null && predicateExpression.Type != typeof(bool)) predicateExpression = System.Linq.Expressions.Expression.Convert(predicateExpression, typeof(bool));
					propertyAccessExpression = System.Linq.Expressions.Expression.Condition(predicateExpression, propertyAccessExpression, System.Linq.Expressions.Expression.Default(propertyInfo.PropertyType));
				}

				if (!context.IsAdmin && !hasAccess)
					throw new UnauthorizedAccessException($"You do not have access to property {propertyInfo.Name} on type { objectExpression.Type }.");
				return (propertyAccessExpression, result);
			}
			return ((System.Linq.Expressions.Expression)System.Linq.Expressions.Expression.MakeMemberAccess(objectExpression, propertyInfo), DefaultResult);
		}
	}
}
