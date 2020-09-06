using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace Darkengines.Expressions.Security {
	public class PermissionEntityTypeBuilderCache {
		public IEnumerable<IEntityType> Types { get; set; }
		public ConcurrentDictionary<IEntityType, Type> TypeMap { get; set; }
		public ConcurrentDictionary<IPropertyBase, PropertyInfo> PropertyMap { get; set; } = new ConcurrentDictionary<IPropertyBase, PropertyInfo>();
		public ConcurrentDictionary<IPropertyBase, PropertyInfo> PermissionPropertyMap { get; set; } = new ConcurrentDictionary<IPropertyBase, PropertyInfo>();
		public bool Initialized { get; set; }
	}
}
