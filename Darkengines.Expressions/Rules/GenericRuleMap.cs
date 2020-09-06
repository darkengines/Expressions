using Darkengines.Expressions.Security;
using DarkEngines.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Darkengines.Expressions.Rules
{
    public abstract class GenericRuleMap<TContext, TGenericType> : RuleMap<TContext, TGenericType> where TGenericType : class
    {
        protected Type GenericTypeDefinition { get; }
        protected Type[] GenericArgumentsTemplate { get; }
        public override bool CanHandle(Type type, TContext context)
        {
            var canHandle = type.IsGenericType && type.GetGenericTypeDefinition() == GenericTypeDefinition && type.GenericTypeArguments.Length == GenericArgumentsTemplate.Length && GenericArgumentsTemplate.Zip(type.GenericTypeArguments).All(tuple => tuple.First == null || tuple.First == tuple.Second);
            return canHandle;
        }
        public GenericRuleMap(Type genericTypeDefinition, Type[] genericArgumentsTemplate, AnonymousTypeBuilder anonymousTypeBuilder, IModel model) : base()
        {
            if (!genericTypeDefinition.IsGenericTypeDefinition) throw new ArgumentException($"Type {genericTypeDefinition.FullName} must be a generic type definition");
            GenericTypeDefinition = genericTypeDefinition;
            var genericTypeArguments = genericTypeDefinition.GetGenericArguments();
            if (genericTypeArguments.Length != genericArgumentsTemplate.Length) throw new ArgumentException($"Not enough generic arguments placeholders supplied for type {genericTypeDefinition.FullName}, expected {genericTypeArguments.Length}, received {genericArgumentsTemplate.Length}");
            GenericArgumentsTemplate = genericArgumentsTemplate;
        }
    }
    public class QueryableGenericRuleMap<TContext> : GenericRuleMap<TContext, IQueryable<object>>
    {

        protected MethodInfo SelectMethodInfo = ExpressionHelper.ExtractGenericDefinitionMethodInfo<IQueryable<object>, Func<Expression<Func<object, object>>, IQueryable<object>>>(query => query.Select);

        public QueryableGenericRuleMap(AnonymousTypeBuilder anonymousTypeBuilder, IModel model) : base(typeof(IQueryable<>), new Type[] { null }, anonymousTypeBuilder, model)
        {

            HasMethodPermissionResolver<Func<Expression<Func<object, object>>, IQueryable<object>>>(query => query.Select, context => Permission.Read, new Type[] { null, null });
            HasMethodPermissionResolver<Func<Expression<Func<object, bool>>, IQueryable<object>>>(query => query.Where, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<int, IQueryable<object>>>(query => query.Skip, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<int, IQueryable<object>>>(query => query.Take, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<IQueryable<object>>>(query => query.Distinct, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<Expression<Func<object, bool>>, bool>>(query => query.Any, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<Expression<Func<object, bool>>, bool>>(query => query.All, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<int>>(query => query.Count, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<Expression<Func<object, IEnumerable<object>>>, IQueryable<object>>>(query => query.SelectMany, context => Permission.Read, new Type[] { null, null });
            HasMethodPermissionResolver<Func<Expression<Func<object, object>>, IOrderedQueryable<object>>>(query => query.OrderBy, context => Permission.Read, new Type[] { null, null });
            HasMethodPermissionResolver<Func<Expression<Func<object, object>>, IOrderedQueryable<object>>>(query => query.OrderByDescending, context => Permission.Read, new Type[] { null, null });
            HasMethodPermissionResolver<Func<Expression<Func<object, object>>, IIncludableQueryable<object, object>>>(query => query.Include, context => Permission.Read, new Type[] { null, null });

            //DisableProjectionForMethod<Func<Expression<Func<object, object>>, IQueryable<object>>>(query => query.Select, new Type[] { null, null });
            //DisableProjectionForMethod<Func<Expression<Func<object, bool>>, IQueryable<object>>>(query => query.Where, new Type[] { null });
            //DisableProjectionForMethod<Func<int, IQueryable<object>>>(query => query.Skip, new Type[] { null });
            //DisableProjectionForMethod<Func<int, IQueryable<object>>>(query => query.Take, new Type[] { null });
            //DisableProjectionForMethod<Func<IQueryable<object>>>(query => query.Distinct, new Type[] { null });
            //DisableProjectionForMethod<Func<Expression<Func<object, bool>>, bool>>(query => query.Any, new Type[] { null });
            //DisableProjectionForMethod<Func<Expression<Func<object, bool>>, bool>>(query => query.All, new Type[] { null });
            //DisableProjectionForMethod<Func<int>>(query => query.Count, new Type[] { null });
            //DisableProjectionForMethod<Func<Expression<Func<object, IEnumerable<object>>>, IQueryable<object>>>(query => query.SelectMany, new Type[] { null, null });
            //DisableProjectionForMethod<Func<Expression<Func<object, object>>, IOrderedQueryable<object>>>(query => query.OrderBy, new Type[] { null, null });
            //DisableProjectionForMethod<Func<Expression<Func<object, object>>, IOrderedQueryable<object>>>(query => query.OrderByDescending, new Type[] { null, null });

            DisableFilterForMethod<Func<Expression<Func<object, object>>, IQueryable<object>>>(query => query.Select, new Type[] { null, null });
            DisableFilterForMethod<Func<Expression<Func<object, bool>>, IQueryable<object>>>(query => query.Where, new Type[] { null });
            DisableFilterForMethod<Func<int, IQueryable<object>>>(query => query.Skip, new Type[] { null });
            DisableFilterForMethod<Func<int, IQueryable<object>>>(query => query.Take, new Type[] { null });
            DisableFilterForMethod<Func<IQueryable<object>>>(query => query.Distinct, new Type[] { null });
            DisableFilterForMethod<Func<Expression<Func<object, bool>>, bool>>(query => query.Any, new Type[] { null });
            DisableFilterForMethod<Func<Expression<Func<object, bool>>, bool>>(query => query.All, new Type[] { null });
            DisableFilterForMethod<Func<int>>(query => query.Count, new Type[] { null });
            DisableFilterForMethod<Func<Expression<Func<object, IEnumerable<object>>>, IQueryable<object>>>(query => query.SelectMany, new Type[] { null, null });
            DisableFilterForMethod<Func<Expression<Func<object, object>>, IOrderedQueryable<object>>>(query => query.OrderBy, new Type[] { null, null });
            DisableFilterForMethod<Func<Expression<Func<object, object>>, IOrderedQueryable<object>>>(query => query.OrderByDescending, new Type[] { null, null });
            DisableFilterForMethod<Func<Expression<Func<object, object>>, IIncludableQueryable<object, object>>>(query => query.Include, new Type[] { null, null });
        }
    }
    public class OrderedQueryableGenericRuleMap<TContext> : GenericRuleMap<TContext, IOrderedQueryable<object>>
    {

        protected MethodInfo SelectMethodInfo = ExpressionHelper.ExtractGenericDefinitionMethodInfo<IQueryable<object>, Func<Expression<Func<object, object>>, IQueryable<object>>>(query => query.Select);
        public OrderedQueryableGenericRuleMap(AnonymousTypeBuilder anonymousTypeBuilder, IModel model) : base(typeof(IOrderedQueryable<>), new Type[] { null }, anonymousTypeBuilder, model)
        {
            HasMethodPermissionResolver<Func<Expression<Func<object, object>>, IQueryable<object>>>(query => query.Select, context => Permission.Read, new Type[] { null, null });
            HasMethodPermissionResolver<Func<Expression<Func<object, bool>>, IQueryable<object>>>(query => query.Where, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<int, IQueryable<object>>>(query => query.Skip, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<int, IQueryable<object>>>(query => query.Take, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<IQueryable<object>>>(query => query.Distinct, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<Expression<Func<object, bool>>, bool>>(query => query.Any, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<Expression<Func<object, bool>>, bool>>(query => query.All, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<int>>(query => query.Count, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<Expression<Func<object, IEnumerable<object>>>, IQueryable<object>>>(query => query.SelectMany, context => Permission.Read, new Type[] { null, null });
            HasMethodPermissionResolver<Func<Expression<Func<object, object>>, IOrderedQueryable<object>>>(query => query.OrderBy, context => Permission.Read, new Type[] { null, null });
            HasMethodPermissionResolver<Func<Expression<Func<object, object>>, IOrderedQueryable<object>>>(query => query.OrderByDescending, context => Permission.Read, new Type[] { null, null });
            HasMethodPermissionResolver<Func<Expression<Func<object, object>>, IOrderedQueryable<object>>>(query => query.ThenBy, context => Permission.Read, new Type[] { null, null });
            HasMethodPermissionResolver<Func<Expression<Func<object, object>>, IOrderedQueryable<object>>>(query => query.ThenByDescending, context => Permission.Read, new Type[] { null, null });

            //DisableProjectionForMethod<Func<Expression<Func<object, object>>, IQueryable<object>>>(query => query.Select, new Type[] { null, null });
            //DisableProjectionForMethod<Func<Expression<Func<object, bool>>, IQueryable<object>>>(query => query.Where, new Type[] { null });
            //DisableProjectionForMethod<Func<int, IQueryable<object>>>(query => query.Skip, new Type[] { null });
            //DisableProjectionForMethod<Func<int, IQueryable<object>>>(query => query.Take, new Type[] { null });
            //DisableProjectionForMethod<Func<IQueryable<object>>>(query => query.Distinct, new Type[] { null });
            //DisableProjectionForMethod<Func<Expression<Func<object, bool>>, bool>>(query => query.Any, new Type[] { null });
            //DisableProjectionForMethod<Func<Expression<Func<object, bool>>, bool>>(query => query.All, new Type[] { null });
            //DisableProjectionForMethod<Func<int>>(query => query.Count, new Type[] { null });
            //DisableProjectionForMethod<Func<Expression<Func<object, IEnumerable<object>>>, IQueryable<object>>>(query => query.SelectMany, new Type[] { null, null });
            //DisableProjectionForMethod<Func<Expression<Func<object, object>>, IOrderedQueryable<object>>>(query => query.OrderBy, new Type[] { null, null });
            //DisableProjectionForMethod<Func<Expression<Func<object, object>>, IOrderedQueryable<object>>>(query => query.OrderByDescending, new Type[] { null, null });
            //DisableProjectionForMethod<Func<Expression<Func<object, object>>, IOrderedQueryable<object>>>(query => query.ThenBy, new Type[] { null, null });
            //DisableProjectionForMethod<Func<Expression<Func<object, object>>, IOrderedQueryable<object>>>(query => query.ThenByDescending, new Type[] { null, null });

            DisableFilterForMethod<Func<Expression<Func<object, object>>, IQueryable<object>>>(query => query.Select, new Type[] { null, null });
            DisableFilterForMethod<Func<Expression<Func<object, bool>>, IQueryable<object>>>(query => query.Where, new Type[] { null });
            DisableFilterForMethod<Func<int, IQueryable<object>>>(query => query.Skip, new Type[] { null });
            DisableFilterForMethod<Func<int, IQueryable<object>>>(query => query.Take, new Type[] { null });
            DisableFilterForMethod<Func<IQueryable<object>>>(query => query.Distinct, new Type[] { null });
            DisableFilterForMethod<Func<Expression<Func<object, bool>>, bool>>(query => query.Any, new Type[] { null });
            DisableFilterForMethod<Func<Expression<Func<object, bool>>, bool>>(query => query.All, new Type[] { null });
            DisableFilterForMethod<Func<int>>(query => query.Count, new Type[] { null });
            DisableFilterForMethod<Func<Expression<Func<object, IEnumerable<object>>>, IQueryable<object>>>(query => query.SelectMany, new Type[] { null, null });
            DisableFilterForMethod<Func<Expression<Func<object, object>>, IOrderedQueryable<object>>>(query => query.OrderBy, new Type[] { null, null });
            DisableFilterForMethod<Func<Expression<Func<object, object>>, IOrderedQueryable<object>>>(query => query.OrderByDescending, new Type[] { null, null });
            DisableFilterForMethod<Func<Expression<Func<object, object>>, IOrderedQueryable<object>>>(query => query.ThenBy, new Type[] { null, null });
            DisableFilterForMethod<Func<Expression<Func<object, object>>, IOrderedQueryable<object>>>(query => query.ThenByDescending, new Type[] { null, null });
        }
    }
    public class EnumerableGenericRuleMap<TContext> : GenericRuleMap<TContext, IEnumerable<object>>
    {
        protected MethodInfo SelectMethodInfo = ExpressionHelper.ExtractGenericDefinitionMethodInfo<IEnumerable<object>, Func<Func<object, object>, IEnumerable<object>>>(query => query.Select);
        public EnumerableGenericRuleMap(AnonymousTypeBuilder anonymousTypeBuilder, IModel model) : base(typeof(IEnumerable<>), new Type[] { null }, anonymousTypeBuilder, model)
        {
            HasMethodPermissionResolver<Func<Func<object, object>, IEnumerable<object>>>(query => query.Select, context => Permission.Read, new Type[] { null, null });
            HasMethodPermissionResolver<Func<Func<object, bool>, IEnumerable<object>>>(query => query.Where, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<int, IEnumerable<object>>>(query => query.Skip, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<int, IEnumerable<object>>>(query => query.Take, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<IEnumerable<object>>>(query => query.Distinct, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<Func<object, bool>, bool>>(query => query.Any, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<Func<object, bool>, bool>>(query => query.All, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<int>>(query => ((IEnumerable<object>)query).Count, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<Func<object, IEnumerable<object>>, IEnumerable<object>>>(query => query.SelectMany, context => Permission.Read, new Type[] { null, null });
            HasMethodPermissionResolver<Func<Func<object, object>, IOrderedEnumerable<object>>>(query => query.OrderBy, context => Permission.Read, new Type[] { null, null });
            HasMethodPermissionResolver<Func<Func<object, object>, IOrderedEnumerable<object>>>(query => query.OrderByDescending, context => Permission.Read, new Type[] { null, null });

			DisableFilterForMethod<Func<int, IEnumerable<object>>>(query => query.Skip, new Type[] { null });
			DisableFilterForMethod<Func<int, IEnumerable<object>>>(query => query.Take, new Type[] { null });
		}
    }
    public class OrderedEnumerableGenericRuleMap<TContext> : GenericRuleMap<TContext, IEnumerable<object>>
    {
        protected MethodInfo SelectMethodInfo = ExpressionHelper.ExtractGenericDefinitionMethodInfo<IEnumerable<object>, Func<Func<object, object>, IEnumerable<object>>>(query => query.Select);
        public OrderedEnumerableGenericRuleMap(AnonymousTypeBuilder anonymousTypeBuilder, IModel model) : base(typeof(IEnumerable<>), new Type[] { null }, anonymousTypeBuilder, model)
        {
            HasMethodPermissionResolver<Func<Func<object, object>, IEnumerable<object>>>(query => query.Select, context => Permission.Read, new Type[] { null, null });
            HasMethodPermissionResolver<Func<Func<object, bool>, IEnumerable<object>>>(query => query.Where, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<int, IEnumerable<object>>>(query => query.Skip, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<int, IEnumerable<object>>>(query => query.Take, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<IEnumerable<object>>>(query => query.Distinct, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<Func<object, bool>, bool>>(query => query.Any, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<Func<object, bool>, bool>>(query => query.All, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<int>>(query => ((IEnumerable<object>)query).Count, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<Func<object, IEnumerable<object>>, IEnumerable<object>>>(query => query.SelectMany, context => Permission.Read, new Type[] { null, null });
            HasMethodPermissionResolver<Func<Func<object, object>, IOrderedEnumerable<object>>>(query => query.OrderBy, context => Permission.Read, new Type[] { null, null });
            HasMethodPermissionResolver<Func<Func<object, object>, IOrderedEnumerable<object>>>(query => query.OrderByDescending, context => Permission.Read, new Type[] { null, null });

			DisableFilterForMethod<Func<int, IEnumerable<object>>>(query => query.Skip, new Type[] { null });
			DisableFilterForMethod<Func<int, IEnumerable<object>>>(query => query.Take, new Type[] { null });
		}
    }
    public class CollectionGenericRuleMap<TContext> : GenericRuleMap<TContext, ICollection<object>>
    {
        protected MethodInfo SelectMethodInfo = ExpressionHelper.ExtractGenericDefinitionMethodInfo<IEnumerable<object>, Func<Func<object, object>, IEnumerable<object>>>(query => query.Select);
        public CollectionGenericRuleMap(AnonymousTypeBuilder anonymousTypeBuilder, IModel model) : base(typeof(ICollection<>), new Type[] { null }, anonymousTypeBuilder, model)
        {
            HasMethodPermissionResolver<Func<Func<object, object>, IEnumerable<object>>>(query => query.Select, context => Permission.Read, new Type[] { null, null });
            HasMethodPermissionResolver<Func<Func<object, bool>, IEnumerable<object>>>(query => query.Where, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<int, IEnumerable<object>>>(query => query.Skip, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<int, IEnumerable<object>>>(query => query.Take, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<IEnumerable<object>>>(query => query.Distinct, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<Func<object, bool>, bool>>(query => query.Any, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<Func<object, bool>, bool>>(query => query.All, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<int>>(query => ((IEnumerable<object>)query).Count, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<Func<object, IEnumerable<object>>, IEnumerable<object>>>(query => query.SelectMany, context => Permission.Read, new Type[] { null, null });
            HasMethodPermissionResolver<Func<Func<object, object>, IOrderedEnumerable<object>>>(query => query.OrderBy, context => Permission.Read, new Type[] { null, null });
            HasMethodPermissionResolver<Func<Func<object, object>, IOrderedEnumerable<object>>>(query => query.OrderByDescending, context => Permission.Read, new Type[] { null, null });

			DisableFilterForMethod<Func<int, IEnumerable<object>>>(query => query.Skip, new Type[] { null });
			DisableFilterForMethod<Func<int, IEnumerable<object>>>(query => query.Take, new Type[] { null });
		}
    }

    public class IncludableQueryGenericRuleMap<TContext> : GenericRuleMap<TContext, IIncludableQueryable<object, object>>
    {
        protected MethodInfo SelectMethodInfo = ExpressionHelper.ExtractGenericDefinitionMethodInfo<IEnumerable<object>, Func<Func<object, object>, IEnumerable<object>>>(query => query.Select);
        public IncludableQueryGenericRuleMap(AnonymousTypeBuilder anonymousTypeBuilder, IModel model) : base(typeof(IIncludableQueryable<,>), new Type[] { null, null }, anonymousTypeBuilder, model)
        {
            HasMethodPermissionResolver<Func<Expression<Func<object, object>>, IQueryable<object>>>(query => query.Select, context => Permission.Read, new Type[] { null, null });
            HasMethodPermissionResolver<Func<Expression<Func<object, bool>>, IQueryable<object>>>(query => query.Where, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<int, IQueryable<object>>>(query => query.Skip, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<int, IQueryable<object>>>(query => query.Take, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<IQueryable<object>>>(query => query.Distinct, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<Expression<Func<object, bool>>, bool>>(query => query.Any, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<Expression<Func<object, bool>>, bool>>(query => query.All, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<int>>(query => query.Count, context => Permission.Read, new Type[] { null });
            HasMethodPermissionResolver<Func<Expression<Func<object, IEnumerable<object>>>, IQueryable<object>>>(query => query.SelectMany, context => Permission.Read, new Type[] { null, null });
            HasMethodPermissionResolver<Func<Expression<Func<object, object>>, IOrderedQueryable<object>>>(query => query.OrderBy, context => Permission.Read, new Type[] { null, null });
            HasMethodPermissionResolver<Func<Expression<Func<object, object>>, IOrderedQueryable<object>>>(query => query.OrderByDescending, context => Permission.Read, new Type[] { null, null });
            HasMethodPermissionResolver<Func<Expression<Func<object, object>>, IIncludableQueryable<object, object>>>(query => query.Include, context => Permission.Read, new Type[] { null, null });
            HasMethodPermissionResolver<Func<Expression<Func<object, object>>, IIncludableQueryable<object, object>>>(query => query.ThenInclude, context => Permission.Read, new Type[] { null, null, null });
            HasMethodPermissionResolver<IIncludableQueryable<object, IEnumerable<object>>, Func<Expression<Func<object, object>>, IIncludableQueryable<object, object>>>(query => query.ThenInclude, context => Permission.Read, new Type[] { null, null, null });

            DisableFilterForMethod<Func<Expression<Func<object, object>>, IIncludableQueryable<object, object>>>(query => query.Include, new Type[] { null, null });
            DisableFilterForMethod<Func<Expression<Func<object, object>>, IIncludableQueryable<object, object>>>(query => query.ThenInclude, new Type[] { null, null });
            DisableFilterForMethod<IIncludableQueryable<object, IEnumerable<object>>, Func<Expression<Func<object, object>>, IIncludableQueryable<object, object>>>(query => query.ThenInclude, new Type[] { null, null });
        }
    }
}
