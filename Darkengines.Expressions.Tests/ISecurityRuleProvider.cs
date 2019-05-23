using Darkengines.Expressions.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Darkengines.Expressions.Tests {
	public interface ISecurityRuleProvider {
		bool CanHandle(Type type);
		Permission GetOperationPermission();
		IQueryable GetAccessibleQueryable(Permission permission);
		IQueryable GetPermissions();
		string GetAccessibleSql(Permission permission);
		string GetPermissionsSql();
	}
}
