using Darkengines.Expressions.Rules;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Darkengines.Expressions.Security {
	public static class ExpressionsSecurityExtensions {
		public static RuleMap<TContext, TEntity> HasInstancePermissionCustomResolverExpression<TContext, TEntity>(this RuleMap<TContext, TEntity>  rulemap, Permission permission, Expression<Func<TContext, TEntity, bool>> resolver) where TEntity: class {
			rulemap.HasInstanceCustomResolverExpression(permission, resolver);
			return rulemap;
		}
		public static RuleMap<TContext, TEntity> HasInstancePropertyPermissionCustomResolverExpression<TContext, TEntity>(this RuleMap<TContext, TEntity> rulemap, Permission permission, Expression<Func<TEntity, object>> propertyAccessExpression, Expression<Func<TContext, TEntity, bool>> resolver) where TEntity : class {
			rulemap.HasInstancePropertyPermissionCustomResolverExpression(permission, propertyAccessExpression, resolver);
			return rulemap;
		}
	}
}
