using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Darkengines.Expressions.Mutation {
	public class JObjectReferenceIEqualityComparer : IEqualityComparer<JObject> {
		public bool Equals([AllowNull] JObject x, [AllowNull] JObject y) {
			return x == y || y.Value<string>("$ref") == x.Value<string>("$id");
		}

		public int GetHashCode([DisallowNull] JObject obj) {
			return obj.GetHashCode();
		}
	}
}
