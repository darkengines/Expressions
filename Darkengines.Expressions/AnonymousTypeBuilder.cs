using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace DarkEngines.Expressions {
	public class AnonymousTypeBuilder {
		protected AssemblyBuilder AssemblyBuilder { get; }
		protected ModuleBuilder ModuleBuilder { get; }
		public AnonymousTypeBuilder(string assemblyName, string moduleName) {
			AssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Run);
			ModuleBuilder = AssemblyBuilder.DefineDynamicModule(moduleName);
		}
		public Type BuildAnonymousType(HashSet<Tuple<Type, string>> propertySet) {
			var dynamicTypeName = Guid.NewGuid().ToString();
			var typeBuilder = ModuleBuilder.DefineType(dynamicTypeName, TypeAttributes.Public);

			foreach (var tuple in propertySet) {
				EmitAutoProperty(typeBuilder, tuple.Item2, tuple.Item1);
			}
			var dynamicType = typeBuilder.CreateType();
			return dynamicType;
		}
		protected PropertyInfo EmitAutoProperty(TypeBuilder typeBuilder, string propertyName, Type propertyType) {
			if (typeof(string) != propertyType && typeof(IEnumerable).IsAssignableFrom(propertyType)) {
				propertyType = typeof(IEnumerable<>).MakeGenericType(propertyType.GetEnumerableUnderlyingType());
			}
			var backingField = typeBuilder.DefineField($"<{propertyName}>k__BackingField", propertyType, FieldAttributes.Private);
			var propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);

			var getMethod = typeBuilder.DefineMethod("get_" + propertyName,
				MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
				propertyType,
				Type.EmptyTypes
			);
			var getGenerator = getMethod.GetILGenerator();
			getGenerator.Emit(OpCodes.Ldarg_0);
			getGenerator.Emit(OpCodes.Ldfld, backingField);
			getGenerator.Emit(OpCodes.Ret);
			propertyBuilder.SetGetMethod(getMethod);

			var setMethod = typeBuilder.DefineMethod("set_" + propertyName,
				MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
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
