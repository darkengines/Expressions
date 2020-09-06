using Darkengines.Expressions.Security;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Darkengines.Expressions.Mutation {
	public static class MutationExtensions {
		public static Permission ToPermission(this EntityState entityState) {
			switch (entityState) {
				case (EntityState.Unchanged): {
						return Permission.Read;
					}
				case (EntityState.Added): {
						return Permission.Create;
					}
				case (EntityState.Modified): {
						return Permission.Write;
					}
				case (EntityState.Deleted): {
						return Permission.Delete;
					}
				default: {
						return Permission.None;
					}
			}
		}
	}
}
