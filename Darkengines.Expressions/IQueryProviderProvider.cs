using System;
using System.Collections.Generic;
using System.Linq;

namespace Darkengines.Expressions {
	public interface IQueryProviderProvider {
		IQueryable GetQuery(Type type);
		Dictionary<string, IQueryable> Context { get; }
	}
}