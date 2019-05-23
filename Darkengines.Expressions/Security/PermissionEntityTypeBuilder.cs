using DarkEngines.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using QuickGraph;
using QuickGraph.Algorithms.TopologicalSort;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Darkengines.Expressions.Security {
	public class PermissionEntityTypeBuilder {
		protected DbContext DbContext { get; }
		protected AssemblyBuilder AssemblyBuilder { get; }
		protected ModuleBuilder ModuleBuilder { get; }
		public IEnumerable<Type> Types { get; }
		public Dictionary<Type, Type> TypeMap { get; }
		public Dictionary<PropertyInfo, PropertyInfo> PropertyMap { get; } = new Dictionary<PropertyInfo, PropertyInfo>();
		public Dictionary<PropertyInfo, PropertyInfo> PermissionPropertyMap { get; } = new Dictionary<PropertyInfo, PropertyInfo>();
		public PermissionEntityTypeBuilder(DbContext dbContext) {
			DbContext = dbContext;
			AssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Dynamic"), AssemblyBuilderAccess.Run);
			ModuleBuilder = AssemblyBuilder.DefineDynamicModule(GetType().Namespace);
			var entityTypes = dbContext.Model.GetEntityTypes();
			var cache = new Dictionary<IEntityType, TypeBuilder>();
			var permissionTypes = BuildPermissionTypes(entityTypes, cache).ToArray();
			TypeMap = cache.ToDictionary(pair => pair.Key.ClrType, pair => pair.Value.CreateType());
			PropertyMap = PropertyMap.ToDictionary(pair => pair.Key, pair => TypeMap[pair.Key.DeclaringType].GetProperty(pair.Value.Name));
			PermissionPropertyMap = PermissionPropertyMap.ToDictionary(pair => pair.Key, pair => TypeMap[pair.Key.DeclaringType].GetProperty(pair.Value.Name));
			Types = TypeMap.Values;
		}

		public IEnumerable<TypeBuilder> BuildPermissionTypes(IEnumerable<IEntityType> entityTypes, Dictionary<IEntityType, TypeBuilder> cache) {
			return entityTypes.Select(entityType => BuildPermissionType(entityType, cache));
		}

		protected TypeBuilder BuildPermissionType(IEntityType entityType, Dictionary<IEntityType, TypeBuilder> cache) {
			var dynamicTypeName = $"{entityType.Name}Permission";
			TypeBuilder typeBuilder = null;
			if (!cache.TryGetValue(entityType, out typeBuilder)) {
				typeBuilder = ModuleBuilder.DefineType(dynamicTypeName, TypeAttributes.Public);
				cache[entityType] = typeBuilder;
				var properties = entityType.GetProperties();
				var navigations = entityType.GetNavigations();
				EmitAutoProperty(typeBuilder, $"SelfPermission", typeof(Permission));
				foreach (var property in properties) {
					var propertyBuilder = EmitAutoProperty(typeBuilder, property.Name, property.ClrType);
					PropertyMap[property.PropertyInfo] = propertyBuilder;
					var permissionPropertyInfo = EmitAutoProperty(typeBuilder, $"{property.Name}Permission", typeof(Permission));
					PermissionPropertyMap[property.PropertyInfo] = permissionPropertyInfo;
				}
				foreach (var navigation in navigations) {
					if (navigation.IsCollection()) {
						var permissionType = BuildPermissionType(navigation.GetTargetType(), cache);
						var propertyBuilder = EmitAutoProperty(typeBuilder, navigation.Name, permissionType.MakeArrayType());
						PropertyMap[navigation.PropertyInfo] = propertyBuilder;
					}
					var permissionPropertyInfo = EmitAutoProperty(typeBuilder, $"{navigation.Name}Permission", typeof(Permission));
					PermissionPropertyMap[navigation.PropertyInfo] = permissionPropertyInfo;
				}
			}
			return typeBuilder;
		}

		protected PropertyBuilder EmitAutoProperty(TypeBuilder typeBuilder, string propertyName, Type propertyType) {
			if (typeof(string) != propertyType && typeof(IEnumerable).IsAssignableFrom(propertyType)) {
				propertyType = typeof(IEnumerable<>).MakeGenericType(propertyType.GetEnumerableUnderlyingType());
			}
			var backingField = typeBuilder.DefineField($"<{propertyName}>k__BackingField", propertyType, FieldAttributes.Private);
			var propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);

			var getMethod = typeBuilder.DefineMethod("get_" + propertyName,
				MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual,
				propertyType,
				Type.EmptyTypes
			);
			var getGenerator = getMethod.GetILGenerator();
			getGenerator.Emit(OpCodes.Ldarg_0);
			getGenerator.Emit(OpCodes.Ldfld, backingField);
			getGenerator.Emit(OpCodes.Ret);
			propertyBuilder.SetGetMethod(getMethod);

			var setMethod = typeBuilder.DefineMethod("set_" + propertyName,
				MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual,
				null,
				new[] { propertyType }
			);
			var setGenerator = setMethod.GetILGenerator();
			setGenerator.Emit(OpCodes.Ldarg_0);
			setGenerator.Emit(OpCodes.Ldarg_1);
			setGenerator.Emit(OpCodes.Stfld, backingField);
			setGenerator.Emit(OpCodes.Ret);
			propertyBuilder.SetSetMethod(setMethod);

			return propertyBuilder;
		}

	}
}
