using Darkengines.Expression.Sample;
using Darkengines.Expressions.Sample.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Darkengines.Expressions.Sample {
	public interface IIdentityProvider {
		RequestIdentity GetIdentity();
	}
}
