using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Darkengines.Expressions.Converters {
	public class MethodInfoOptions {
		public MethodInfo MethodInfo { get; }
		public bool AllowTerminalInstance { get; }
		public MethodInfoOptions(MethodInfo methodInfo, bool allowTerminalInstance) {
			MethodInfo = methodInfo;
			AllowTerminalInstance = allowTerminalInstance;
		}
	}
}
