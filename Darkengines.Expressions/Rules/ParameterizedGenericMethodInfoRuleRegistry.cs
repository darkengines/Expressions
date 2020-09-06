using Darkengines.Expressions.Security;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace Darkengines.Expressions.Rules {
	public class ParameterizedGenericMethodInfoRuleRegistry {
		public MethodInfoOptions this[(MethodInfo methodInfo, Type[] genericArguments) tuple] {
			get {
				ICollection<MethodInfoOptions> map = null;
				MethodInfosRegistry.TryGetValue(tuple.methodInfo, out map);
				if (map == null) return null;
				var rule = map.Select(template => (template, template.GenericArguments.Zip(tuple.genericArguments, (templateType, type) =>
					templateType == type ? 0
					: (templateType != null && templateType != type ?
						template.GenericArguments.Length + 1
						: 1
					)
				).Sum())).Where(scored => scored.Item2 <= scored.template.GenericArguments.Length)
				.GroupBy(scored => scored.Item2).OrderBy(g => g.Key).FirstOrDefault()?.FirstOrDefault().template;
				return rule;
			}


			set {
				ICollection<MethodInfoOptions> options = null;
				if (!MethodInfosRegistry.TryGetValue(tuple.methodInfo, out options)) {
					MethodInfosRegistry[tuple.methodInfo] = new Collection<MethodInfoOptions>() { value };
				} else {
					options.Add(value);
				}
			}
		}
		protected IDictionary<MethodInfo, ICollection<MethodInfoOptions>> MethodInfosRegistry { get; } = new Dictionary<MethodInfo, ICollection<MethodInfoOptions>>(new MethodInfoHandleComparer());
	}
}
