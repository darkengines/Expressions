using DarkEngines.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Darkengines.Expressions.Security {
	public class PermissionEntityTypeBuilder {
		protected IModelProvider ModelProvider { get; }
		protected IModel Model { get; }
		protected AssemblyBuilder AssemblyBuilder { get; }
		protected ModuleBuilder ModuleBuilder { get; }
		protected PermissionEntityTypeBuilderCache PermissionEntityTypeBuilderCache { get; }

		public IEnumerable<IEntityType> Types { get => PermissionEntityTypeBuilderCache.Types; }
		public IDictionary<IEntityType, Type> TypeMap { get => PermissionEntityTypeBuilderCache.TypeMap; }
		public IDictionary<IPropertyBase, PropertyInfo> PropertyMap { get => PermissionEntityTypeBuilderCache.PropertyMap; }
		public IDictionary<IPropertyBase, PropertyInfo> PermissionPropertyMap { get => PermissionEntityTypeBuilderCache.PermissionPropertyMap; }

		public PermissionEntityTypeBuilder(IModelProvider modelProvider, PermissionEntityTypeBuilderCache permissionEntityTypeBuilderCache) {
			ModelProvider = modelProvider;
			Model = ModelProvider.Model;
			PermissionEntityTypeBuilderCache = permissionEntityTypeBuilderCache;
			if (!PermissionEntityTypeBuilderCache.Initialized) {
				lock (PermissionEntityTypeBuilderCache) {
					if (!PermissionEntityTypeBuilderCache.Initialized) {
						AssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Dynamic"), AssemblyBuilderAccess.Run);
						ModuleBuilder = AssemblyBuilder.DefineDynamicModule(GetType().Namespace);
						var entityTypes = modelProvider.Model.GetEntityTypes();
						var cache = new Dictionary<IEntityType, TypeBuilder>();
						var permissionTypes = BuildPermissionTypes(entityTypes, cache).ToArray();
						PermissionEntityTypeBuilderCache = permissionEntityTypeBuilderCache;
						PermissionEntityTypeBuilderCache.TypeMap = new ConcurrentDictionary<IEntityType, Type>(cache.ToDictionary(pair => pair.Key, pair => pair.Value.CreateType()));
						PermissionEntityTypeBuilderCache.PropertyMap = new ConcurrentDictionary<IPropertyBase, PropertyInfo>(PermissionEntityTypeBuilderCache.PropertyMap.ToDictionary(pair => pair.Key, pair => PermissionEntityTypeBuilderCache.TypeMap[(IEntityType)pair.Key.DeclaringType].GetProperty(pair.Value.Name)));
						PermissionEntityTypeBuilderCache.PermissionPropertyMap = new ConcurrentDictionary<IPropertyBase, PropertyInfo>(PermissionEntityTypeBuilderCache.PermissionPropertyMap.ToDictionary(pair => pair.Key, pair => PermissionEntityTypeBuilderCache.TypeMap[(IEntityType)pair.Key.DeclaringType].GetProperty(pair.Value.Name)));
						PermissionEntityTypeBuilderCache.Types = PermissionEntityTypeBuilderCache.TypeMap.Keys;
						PermissionEntityTypeBuilderCache.Initialized = true;
					}
				}
			}
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
				EmitAutoProperty(typeBuilder, $"SelfPermission", typeof(bool?));
				foreach (var property in properties.Where(p => p.PropertyInfo != null)) {
					var propertyBuilder = EmitAutoProperty(typeBuilder, property.Name, property.ClrType);
					PermissionEntityTypeBuilderCache.PropertyMap[property] = propertyBuilder;
					var permissionPropertyInfo = EmitAutoProperty(typeBuilder, $"{property.Name}Permission", typeof(bool?));
					PermissionEntityTypeBuilderCache.PermissionPropertyMap[property] = permissionPropertyInfo;
				}
				foreach (var navigation in navigations.Where(p => p.PropertyInfo != null)) {
					var permissionType = BuildPermissionType(navigation.GetTargetType(), cache);
					var propertyBuilder = EmitAutoProperty(typeBuilder, navigation.Name, navigation.IsCollection() ? permissionType.MakeArrayType() : permissionType);
					var permissionPropertyInfo = EmitAutoProperty(typeBuilder, $"{navigation.Name}Permission", typeof(bool?));
					PermissionEntityTypeBuilderCache.PermissionPropertyMap[navigation] = permissionPropertyInfo;
					PermissionEntityTypeBuilderCache.PropertyMap[navigation] = propertyBuilder;
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
