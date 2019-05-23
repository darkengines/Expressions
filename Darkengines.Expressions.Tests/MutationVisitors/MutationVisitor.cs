using Darkengines.Expressions.Security;
using Darkengines.Expressions.Tests.Entities;
using Darkengines.Expressions.Tests.Rules;
using DarkEngines.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Darkengines.Expressions.Tests.MutationVisitors {
    public class MutationVisitor {
        protected DbContext DbContext { get; }
        protected IIdentityProvider IdentityProvider { get; }
        protected RuleProvider RuleProvider { get; }
        protected AnonymousTypeBuilder AnonymousTypeBuilder { get; }
        protected IEqualityComparer<HashSet<Tuple<Type, string>>> SetComparer { get; }
        protected Dictionary<HashSet<Tuple<Type, string>>, Type> Cache { get; }
        public MutationVisitor(
            DbContext dbContext,
            IIdentityProvider identityProvider,
            RuleProvider ruleProvider,
            AnonymousTypeBuilder anonymousTypeBuilder
        ) {
            DbContext = dbContext;
            IdentityProvider = identityProvider;
            RuleProvider = ruleProvider;
            AnonymousTypeBuilder = anonymousTypeBuilder;
            SetComparer = HashSet<Tuple<Type, string>>.CreateSetComparer();
            Cache = new Dictionary<HashSet<Tuple<Type, string>>, Type>(SetComparer);
        }

        public MutationNode Visit(IEntityType entityType, JObject jObject) {
            var jObjectProperties = jObject.Properties();
            var navigationProperties = entityType.GetNavigations();
            var entityProperties = entityType.GetProperties().Cast<IPropertyBase>().Concat(navigationProperties);

            var primaryKey = entityType.FindPrimaryKey();
            var primaryKeyTuples = primaryKey.Properties.Join(
                jObjectProperties,
                property => property.Name,
                jProperty => jProperty.Name.ToPascalCase(),
                (property, jProperty) => new { Property = property, JProperty = jProperty }
            );
            var isPrimaryKeySet = primaryKeyTuples.All(tuple => {
                var jPropertyValue = tuple.JProperty.ToObject(tuple.Property.ClrType);
                return jPropertyValue != null && jPropertyValue != Activator.CreateInstance(tuple.Property.ClrType);
            });
            var isDeletion = jObjectProperties.Any(jProperty => jProperty.Name == "$isDeletion" && jProperty.Value<bool>());

            var currentUser = IdentityProvider.GetCurrentUser();
            var rules = new HashSet<Func<User, LambdaExpression>>();
            var mutationNode = new MutationNode();
            mutationNode.JObject = jObject;
            mutationNode.Key = primaryKey;
            mutationNode.EntityType = entityType;

            if (!isDeletion) {
                if (!isPrimaryKeySet) {
                    // if primary key is not set, consider add
                    // check permission to add
                    //var canAdd = securityProvider.GetOperationPermission().HasFlag(Permission.Write);
                } else {
                    // if primary key is set, consider update
                    // check permission to update
                    //var canUpdate = securityProvider.GetOperationPermission().HasFlag(Permission.Write);
                    if (true) {
                        mutationNode.RequestedPermission = Permission.Write;
                        // check permission to access
                        foreach (var rule in RuleProvider.GetRulesFor(entityType.ClrType)) {
                            if (!rules.Contains(rule)) {
                                rules.Add(rule);
                            }
                        }
                    }
                }
            } else {
                // if delete tag is set, consider delete
                //var canDelete = securityProvider.GetOperationPermission().HasFlag(Permission.Write);
                // check permission to delete
                if (true) {
                    // check permission to access
                    foreach (var rule in RuleProvider.GetRulesFor(entityType.ClrType)) {
                        if (!rules.Contains(rule)) {
                            rules.Add(rule);
                        }
                    }
                }
            }
            mutationNode.RuleMap = RuleProvider.GetRuleMapFor(entityType.ClrType);
            var propertyRules = jObjectProperties.Join(
                entityProperties,
                jProperty => jProperty.Name.ToPascalCase(),
                property => property.Name,
                (jProperty, property) => new {
                    JProperty = jProperty,
                    Property = property,
                    Rules = RuleProvider.GetRulesFor(property.PropertyInfo)
                }
            ).ToArray();

            mutationNode.Properties = propertyRules.Select(pr => new Tuple<JProperty, IPropertyBase, ISet<Func<User, LambdaExpression>>>(
                pr.JProperty,
                pr.Property,
                pr.Rules
            ));

            foreach (var propertyRule in propertyRules) {
                foreach (var rule in propertyRule.Rules) {
                    if (!rules.Contains(rule)) {
                        rules.Add(rule);
                    }
                }
            }

            mutationNode.RuleSet = rules;

            var rulesExpression = GetRuleSetScript(rules, entityType);
            var dbSet = DbContext.GetType().GetProperties().FirstOrDefault(p => p.PropertyType.GetEnumerableUnderlyingType() == entityType.ClrType).GetValue(DbContext);
            var methodInfo = ExpressionHelper.ExtractMethodInfo<IQueryable<object>, Func<Expression<Func<object, object>>, IQueryable<object>>>(queryable => queryable.Select).GetGenericMethodDefinition().MakeGenericMethod(entityType.ClrType, rulesExpression.ReturnType);
            var call = Expression.Call(methodInfo, Expression.Constant(dbSet), rulesExpression);
            var m = Expression.Lambda<Func<IQueryable<object>>>(Expression.Convert(call, typeof(IQueryable<object>))).Compile();
            var result = m().ToSql();

            mutationNode.PermissionSql = result;

            foreach (var navigationProperty in navigationProperties.Join(jObjectProperties, np => np.Name, jp => jp.Name.ToPascalCase(), (navigationProperty, jProperty) => new {
                Navigation = navigationProperty,
                JProperty = jProperty
            })) {
                // if not collection, just do a regular recursive call
                if (!navigationProperty.Navigation.IsCollection()) {
                    var childNode = Visit(navigationProperty.Navigation.GetTargetType(), (JObject)navigationProperty.JProperty.Value);
                    childNode.Parent = mutationNode;
                    mutationNode.Children.Add(childNode);
                } else {
                    var values = ((JArray)navigationProperty.JProperty.Value).Values<JObject>();
                    foreach (var value in values) {
                        var childNode = Visit(navigationProperty.Navigation.GetTargetType(), value);
                        childNode.Parent = mutationNode;
                        mutationNode.Children.Add(childNode);
                    }
                }
            }

            return mutationNode;
        }
        protected LambdaExpression GetRuleSetScript(ISet<Func<User, LambdaExpression>> rules, IEntityType entityType) {
            var surrogates = new Dictionary<Expression, Expression>();
            var newParameter = Expression.Parameter(entityType.ClrType);
            var primaryKey = entityType.FindPrimaryKey().Properties;
            var replacementExpressionVisitor = new ReplacementExpressionVisitor(surrogates);
            var expressions = rules.Select(rule => {
                var expression = rule(IdentityProvider.GetCurrentUser());
                var parameter = expression.Parameters[0];
                var body = expression.Body;
                surrogates[parameter] = newParameter;
                body = replacementExpressionVisitor.Visit(body);
                return new {
                    Rule = rule,
                    Body = body
                };
            }).ToArray();

            var tuples = entityType.FindPrimaryKey().Properties.Select(property => new Tuple<Type, string>(property.ClrType, property.Name)).Concat(
                expressions.Select(propertyValueExpression => new Tuple<Type, string>(typeof(Permission), /*propertyValueExpression.Rule.Name*/ "MAMADOU")).ToArray() //WARNING
            );
            var set = new HashSet<Tuple<Type, string>>(tuples);
            Type anonymousType = null;
            Cache.TryGetValue(set, out anonymousType);
            if (anonymousType == null) {
                anonymousType = AnonymousTypeBuilder.BuildAnonymousType(set);
                Cache[set] = anonymousType;
            }
            var newExpression = Expression.New(anonymousType.GetConstructor(new Type[0]));
            var initializationExpression = Expression.MemberInit(
                newExpression,
                primaryKey.Select(pk => Expression.Bind(anonymousType.GetProperty(pk.Name), Expression.Property(newParameter, pk.PropertyInfo))).Concat(expressions.Select(pve => Expression.Bind(anonymousType.GetProperty(/*pve.Rule.Name*/"MAMADOU"), pve.Body))) //WARNING
            );

            return Expression.Lambda(initializationExpression, newParameter);
        }
    }
}
