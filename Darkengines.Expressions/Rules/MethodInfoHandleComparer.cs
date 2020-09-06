using System.Collections.Generic;
using System.Reflection;

namespace Darkengines.Expressions.Rules {
	public class MethodInfoHandleComparer : IEqualityComparer<MethodInfo> {
		public bool Equals(MethodInfo x, MethodInfo y) {
			return x.MethodHandle == y.MethodHandle;
		}

		public int GetHashCode(MethodInfo obj) {
			return obj.MethodHandle.GetHashCode();
		}
	}
}
