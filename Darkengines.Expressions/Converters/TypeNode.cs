using System;
using System.Collections.Generic;
using System.Linq;

namespace Darkengines.Expressions.Converters {
	public enum TypeNodeDirection {
		Input,
		Output
	}
	public class TypeNode {
		public static TypeNode FromType(Type type, int index, TypeNodeDirection direction, TypeNode parent) {
			if (type.IsGenericTypeParameter) return new TypeNode(index, direction, parent);
			var node = new TypeNode(index, direction, parent);
			if (type.IsGenericType) {
				var genericArguments = type.GetGenericArguments();
				var children = genericArguments.Select(genericArgument => FromType(genericArgument, index, direction, node)).ToArray();
				foreach (var child in children) node.Children.Add(child);
			}
			return node;
		}
		public TypeNode(int index, TypeNodeDirection direction, TypeNode parent) {
			Index = index;
			Direction = direction;
			Children = new HashSet<TypeNode>();
			Parent = parent;
		}

		public int Index { get; }
		public TypeNodeDirection Direction { get; }
		public HashSet<TypeNode> Children { get; }
		public TypeNode Parent { get; set; }
	}
}
