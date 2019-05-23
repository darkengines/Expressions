using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Darkengines.Expressions.Tests {
	public interface INode {
		JToken JToken { get; }
		PropertyBase Property { get; }
	}
}
