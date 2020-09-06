using Darkengines.Expressions.Security;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Darkengines.Expressions.Rules {
	public delegate Permission PermissionResolver(object context, MethodInfo methodInfo, Type[] genericArguments, Type instanceType);
	public class MethodInfoOptions {
		public PermissionResolver PermissionResolver { get; set; }
		public bool ShouldProject { get; set; } = true;
		public bool ShouldFilter { get; set; } = true;
		public MethodInfo MethodInfo { get; }
		public Type[] GenericArguments { get; }

		public MethodInfoOptions(MethodInfo methodInfo, Type[] genericArguments) {
			MethodInfo = methodInfo;
			GenericArguments = genericArguments;
		}

		public override bool Equals(object obj) {
			return obj is MethodInfoOptions && ((MethodInfoOptions)obj).MethodInfo.MethodHandle == MethodInfo.MethodHandle;
		}
		public override int GetHashCode() {
			return MethodInfo.MethodHandle.GetHashCode();
		}
	}
}
