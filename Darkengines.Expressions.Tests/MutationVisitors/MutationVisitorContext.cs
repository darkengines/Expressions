using Darkengines.Expressions.Tests.Rules;
using DarkEngines.Expressions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Darkengines.Expressions.Tests.MutationVisitors {
	public class MutationVisitorContext {
		public DbContext DbContext { get; }
		public IIdentityProvider IdentityProvider { get; }
		public RuleProvider RuleProvider { get; }
		public AnonymousTypeBuilder AnonymousTypeBuilder { get; }
	}
}
