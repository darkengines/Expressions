using Microsoft.EntityFrameworkCore.Metadata;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Darkengines.Expressions.Mutation {
	public class PropertyPropertyTuple {
		public IProperty Property { get; set; }
		public JProperty JProperty { get; set; }
	}
}
