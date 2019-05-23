using Darkengines.Expressions.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Darkengines.Expressions.Tests {
	public abstract class SecurityRuleProvider<TEntity> : ISecurityRuleProvider where TEntity : class {
		protected BloggingContext BloggingContext { get; }
		protected IIdentityProvider IdentityProvider { get; }

		public SecurityRuleProvider(IIdentityProvider identityProvider, BloggingContext bloggingContext) {
			BloggingContext = bloggingContext;
			IdentityProvider = identityProvider;
		}
		public bool CanHandle(Type type) {
			return type == typeof(TEntity);
		}
		IQueryable ISecurityRuleProvider.GetAccessibleQueryable(Permission permission) {
			return GetAccessibleQueryable(permission);
		}
		public abstract IQueryable GetPermissions();
		public abstract IQueryable<TEntity> GetAccessibleQueryable(Permission permission);
		public string GetAccessibleSql(Permission permission) { return GetAccessibleQueryable(permission).ToSql(); }
		public abstract Permission GetOperationPermission();

		public string GetPermissionsSql() {
			return GetPermissions().Cast<object>().ToSql();
		}
	}
}
