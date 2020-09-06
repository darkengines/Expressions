using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Darkengines.Expressions.Rules {
	public class PropertyInfoHandleComparer : IEqualityComparer<PropertyInfo> {
		public bool Equals(PropertyInfo x, PropertyInfo y) {
			return x.Name == y.Name;
		}

		public int GetHashCode(PropertyInfo obj) {
			return obj.Name.GetHashCode();
		}
	}
}