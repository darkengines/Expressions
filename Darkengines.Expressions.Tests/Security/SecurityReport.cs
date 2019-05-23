using Darkengines.Expressions.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Darkengines.Expressions.Tests.Security {
	public class SecurityReport {
		public int Id {
			get {
				if (Parent == null) return 0;
				return Parent.Id + 1;
			}
		}
		public JObject JObject { get; }
		public IEntityType EntityType { get; }
		public string AccessibleScript { get; }
		public ICollection<SecurityReport> Children { get; }
		public SecurityReport Parent { get; }
		public Permission Permission { get; }
		public SecurityReport(JObject jObject, IEntityType entityType, string accessibleScript, SecurityReport parent, Permission permission) {
			JObject = jObject;
			Permission = permission;
			EntityType = entityType;
			AccessibleScript = accessibleScript;
			Children = new Collection<SecurityReport>();
			Parent = parent;
		}
		public string[] PartialScripts {
			get {
				return new[] { PartialScript }.Concat(Children.SelectMany(child => child.PartialScripts)).ToArray();
			}
		}
		
		public void Apply(EntityAccess[] accessibleNavigations) {
			var canAccess = accessibleNavigations.Any(an => an.NodeId == Id);
			if (!canAccess) {
				JObject.Remove();
			} else {
				foreach (var child in Children) child.Apply(accessibleNavigations);
			}

		}

		public string PartialScript {
			get {
				var jObjectProperties = JObject.Properties();
				var primaryKeyProperties = EntityType.FindPrimaryKey();
				var primaryKeyTuples = primaryKeyProperties.Properties.Join(
					jObjectProperties,
					property => property.Name,
					jProperty => jProperty.Name.ToPascalCase(),
					(property, jProperty) => new { Property = property, JProperty = jProperty }
				);
				var primaryKey = primaryKeyTuples.ToDictionary(tuple => tuple.Property.Relational().ColumnName, tuple => tuple.JProperty.Value.ToObject(tuple.Property.ClrType));
				return
				$@"SELECT
					{Id},
					{1}
				FROM
					{(Parent == null ? 
						$"{EntityType.Relational().TableName} t"
						: $"{EntityType.Relational().TableName} t, Access a"
					)}
				WHERE
					{(Parent == null ? "1=1" : $"a.Id = {Parent.Id} AND a.HasAccess = 1")}
					AND {string.Join(" AND ", primaryKey.Select(pk => $"t.{pk.Key}={pk.Value}"))}
					AND EXISTS (
						SELECT 
							{string.Join(", ", primaryKey.Select(pk => pk.Key))} 
						FROM 
							({AccessibleScript}) a 
						WHERE 
							{string.Join("AND", primaryKey.Select(pk => $"a.{pk.Key} = t.{pk.Key}"))}
							AND a.Permission & {(int)Permission} <> 0
					)";
			}
		}
	}
}
