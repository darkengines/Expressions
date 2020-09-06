using System.Collections.Generic;
using System.Reflection;

namespace Darkengines.Expressions {
	public interface IStaticMethodsProvider {
		IEnumerable<MethodInfo> Methods { get; }
	}
	public class StaticExtensionMethodsProvider : IStaticMethodsProvider {
		public StaticExtensionMethodsProvider(IEnumerable<MethodInfo> methods) {
			Methods = methods;
		}

		public IEnumerable<MethodInfo> Methods { get; }
	}
}
