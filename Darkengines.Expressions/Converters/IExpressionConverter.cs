using Darkengines.Expressions.Rules;
using Darkengines.Expressions.Security;
using DarkEngines.Expressions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Darkengines.Expressions.Converters {
	public interface IExpressionConverter {
		Esprima.Ast.Nodes NodeType { get; }
		bool CanHandle(Esprima.Ast.INode expression);
		bool IsGenericType { get; }
		Type GetGenericType(Esprima.Ast.INode node, Type genericTypeDefinition);
		Expression Convert(Esprima.Ast.INode node, ExpressionConverterContext expressionConverterContext, ExpressionConverterScope expressionConverterScope, bool allowTerminals = false, params Type[] genericArguments);
		void ResolveGenericArguments(Dictionary<Type, Type> genericArgumentMapping);
		TypeNode[] GetRequiredGenericArgumentIndices(Esprima.Ast.INode node, Type genericTypeDefinition);
	}
	public abstract class ExpressionConverter<T> : IExpressionConverter where T : Esprima.Ast.INode {
		public static ExpressionConversionResult DefaultResult = new ExpressionConversionResult();
		protected static MethodInfo QueryableWhereMethodInfo { get; } = ExpressionHelper.ExtractMethodInfo<IQueryable<object>, Func<Expression<Func<object, bool>>, IQueryable<object>>>(queryable => queryable.Where).GetGenericMethodDefinition();
		protected static MethodInfo QueryableSelecteMethodInfo { get; } = ExpressionHelper.ExtractMethodInfo<IQueryable<object>, Func<Expression<Func<object, object>>, IQueryable<object>>>(queryable => queryable.Select).GetGenericMethodDefinition();
		protected static MethodInfo EnumerableWhereMethodInfo { get; } = ExpressionHelper.ExtractMethodInfo<IEnumerable<object>, Func<Func<object, bool>, IEnumerable<object>>>(enumerable => enumerable.Where).GetGenericMethodDefinition();
		protected static MethodInfo EnumerableSelecteMethodInfo { get; } = ExpressionHelper.ExtractMethodInfo<IQueryable<object>, Func<Func<object, object>, IEnumerable<object>>>(enumerable => enumerable.Select).GetGenericMethodDefinition();
		public abstract Esprima.Ast.Nodes NodeType { get; }
		public virtual bool IsGenericType { get { return false; } }
		Type IExpressionConverter.GetGenericType(Esprima.Ast.INode node, Type genericTypeDefinition) => GetGenericType((T)node, genericTypeDefinition);
		public virtual Type GetGenericType(T node, Type genericTypeDefinition) { throw new NotSupportedException(); }
		TypeNode[] IExpressionConverter.GetRequiredGenericArgumentIndices(Esprima.Ast.INode node, Type genericTypeDefinition) => GetRequiredGenericArgumentIndices((T)node, genericTypeDefinition);
		public virtual TypeNode[] GetRequiredGenericArgumentIndices(T arrowFunctionExpression, Type genericTypeDefinition) {
			throw new NotSupportedException();
		}
		public virtual void ResolveGenericArguments(Dictionary<Type, Type> genericArgumentMapping) {
			//Nothing to do
		}
		bool IExpressionConverter.CanHandle(Esprima.Ast.INode expression) => CanHandle((T)expression);
		Expression IExpressionConverter.Convert(Esprima.Ast.INode expression, ExpressionConverterContext expressionConverterContext, ExpressionConverterScope expressionConverterScope, bool allowTerminal, params Type[] genericArguments) {
			var (resultExpression, result) = Convert((T)expression, expressionConverterContext, expressionConverterScope, allowTerminal, genericArguments);
			if (!expressionConverterContext.IsAdmin && resultExpression != null && typeof(IEnumerable).IsAssignableFrom(resultExpression.Type) && typeof(string) != resultExpression.Type) {
				var underlyingType = resultExpression.Type.GetEnumerableUnderlyingType();
				var rule = expressionConverterContext.RuleMapRegistry.GetRuleMap(underlyingType, expressionConverterContext.securityContext);
				if (rule != null && result.ShouldApplyFilter) {
					var parameterExpression = Expression.Parameter(underlyingType);
					var permissionExpression = rule.GetInstanceCustomResolverExpression(expressionConverterContext.securityContext, parameterExpression, Permission.Read);
					if (permissionExpression != null && permissionExpression.Type != typeof(bool)) permissionExpression = Expression.Convert(permissionExpression, typeof(bool));
					if (permissionExpression != null) {
						var predicateExpression = permissionExpression;
						var predicateLambda = Expression.Lambda(predicateExpression, parameterExpression);
						resultExpression = typeof(IQueryable).IsAssignableFrom(resultExpression.Type) ?
						Expression.Call(QueryableWhereMethodInfo.MakeGenericMethod(underlyingType), resultExpression, predicateLambda)
						: Expression.Call(EnumerableWhereMethodInfo.MakeGenericMethod(underlyingType), resultExpression, predicateLambda);
					}
				}
			}
			var ruleMap = expressionConverterContext.RuleMapRegistry.GetRuleMap(resultExpression.Type, expressionConverterContext.securityContext);
			if (false && !expressionConverterContext.IsAdmin && ruleMap != null && !allowTerminal && ruleMap.RequireProjection && result.ShouldApplyProjection) {
				resultExpression = ruleMap.GetDefaultProjectionExpression(expressionConverterContext.securityContext, resultExpression, expressionConverterContext.RuleMaps);
			}
			return resultExpression;
		}
		public virtual bool CanHandle(T expression) { return expression is T; }
		public abstract (Expression Expression, ExpressionConversionResult ExpressionConversionResult) Convert(T node, ExpressionConverterContext expressionConverterContext, ExpressionConverterScope expressionConverterScope, bool allowTerminal = false, params Type[] genericArguments);
	}
}
