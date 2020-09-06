using System.Collections.Generic;

namespace Darkengines.Expressions.Entities {
	public class EntityInfo {
		public string Name { get; set; }
		public string[] Key { get; set; }
		public ForeignKey[] ForeignKeys { get; set; }
		public Dictionary<string, Navigation> Navigations { get; set; }
	}
}
