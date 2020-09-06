using System.Collections.Generic;
using System.Reflection;

namespace Darkengines.Expressions {
	public interface IExtensionMethodsProvider {
		IEnumerable<MethodInfo> Methods { get; }
	}
	public class CustomExtensionMethodsProvider : IExtensionMethodsProvider {
		public CustomExtensionMethodsProvider(IEnumerable<MethodInfo> methods) {
			Methods = methods;
		}

		public IEnumerable<MethodInfo> Methods { get; }
	}
}
