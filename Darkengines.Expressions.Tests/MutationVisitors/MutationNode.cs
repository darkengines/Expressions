using Darkengines.Expressions.Security;
using Darkengines.Expressions.Tests.Entities;
using Darkengines.Expressions.Tests.Rules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Darkengines.Expressions.Tests.MutationVisitors {
    public class MutationNode {
        public MutationNode() {
            Children = new HashSet<MutationNode>();
            Properties = new HashSet<Tuple<JProperty, IPropertyBase, ISet<Func<User, LambdaExpression>>>>();
        }
        public MutationNode Parent { get; set; }
        public string PermissionSql { get; set; }
        public IRuleMap RuleMap { get; set; }
        public ISet<Func<User, LambdaExpression>> RuleSet { get; set; }
        public IEntityType EntityType { get; set; }
        public JObject JObject { get; set; }
        public Permission RequestedPermission { get; set; }
        public ISet<MutationNode> Children { get; }
        public IEnumerable<Tuple<JProperty, IPropertyBase, ISet<Func<User, LambdaExpression>>>> Properties { get; set; }
        public IKey Key { get; set; }
        public string GetWithSql(int nodeIndex = 0) {
            var nodeName = $"Node{nodeIndex}";
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"{(Parent == null ? "WITH ": null)}{nodeName}Rules (");
            var keyPropertiesNamesSql = string.Join(",\n\t", Key.Properties.Select(kp => kp.Name));
            var rulesSql = string.Join(",\n\t", RuleSet.Select(r => "MAMADOU")); //WARNING
            var propertiesNameSql = string.Join(",\n\t", new[] { "Self" }.Concat(Properties.Where(p => !Key.Properties.Contains(p.Item2)).Select(p => p.Item2.Name)));
            var propertiesRulesValuesSql = string.Join(",\n\t", new[] { RuleMap.Self }.Concat(Properties.Where(p => !Key.Properties.Contains(p.Item2)).Select(p => p.Item3)).Select(ruleSet => string.Join(" | ", (ruleSet.Any() ? ruleSet : RuleMap.Self).Select(r => $"r.{/*r.Name*/"MAMADOU"}")))); //WARNING
            stringBuilder.AppendLine($"\t{keyPropertiesNamesSql},");
            stringBuilder.AppendLine($"\t{rulesSql}");
            stringBuilder.AppendLine(") AS (");
            stringBuilder.AppendLine(PermissionSql);
            stringBuilder.AppendLine($"), {nodeName}Permissions (");
            stringBuilder.AppendLine($"\t{keyPropertiesNamesSql},");
            stringBuilder.AppendLine($"\t{propertiesNameSql}");
            stringBuilder.AppendLine(") AS (");
            stringBuilder.AppendLine(
$@"  SELECT
        {string.Join(",\n\t", Key.Properties.Select(p => $"r.{p.Name}"))},
        {propertiesRulesValuesSql}
    FROM
        {EntityType.Relational().TableName} e
        JOIN {nodeName}Rules r ON
            {string.Join(" AND ", Key.Properties.Select(kp => $"e.{kp.Name} = r.{kp.Name}"))}");
            stringBuilder.AppendLine(")");

            var childrenWith = string.Join(", ", Children.Select(child => child.GetWithSql(++nodeIndex)));

            if (!string.IsNullOrWhiteSpace(childrenWith)) {
                stringBuilder.Append(",");
                stringBuilder.Append(childrenWith);
            }

            var test = GetSelfSql("Self", nodeIndex, new Dictionary<string, SqlParameter>());

            return stringBuilder.ToString();
        }
        public string GetSelfSql(string permissionProperty, int nodeIndex, IDictionary<string, SqlParameter> parameters, int? parentNodeIndex = null) {
            parameters = new Dictionary<string, SqlParameter>();
            var nodeName = $"Node{nodeIndex}";
            var parameterName = $"{nodeName}Id";
            var keys = Key.Properties.Join(
                Properties,
                kp => kp,
                jp => jp.Item2,
                (kp, jp) => new {
                    JProperty = jp.Item1,
                    Property = kp,
                    Value = jp.Item1.ToObject(kp.ClrType)
                }
            );
            var template =
$@"SELECT
    {nodeIndex},
    p.{permissionProperty}
FROM
    Nodes n
    JOIN {nodeName} Permissions p ON
        n.Id = {parentNodeIndex}
        AND {string.Join(" AND ", keys.Select((kp, index) => $"n.{kp.Property.Name} = @{parameterName}{index}"))}
";
            var keyComponentIndex = 0;
            foreach (var keyComponent in keys) {
                var keyComponentParameterName = $"{parameterName}{keyComponentIndex++}";
                parameters[keyComponentParameterName] = new SqlParameter(keyComponentParameterName, keyComponent.Value);
            }
            return template;
        }
        //public string GetUnionsSql(int nodeIndex = 0) {
        //    var nodeName = $"Node{nodeIndex}";
        //    var stringBuilder = new StringBuilder();
        //    if (Parent == null) {
        //        stringBuilder.AppendLine("Nodes (");
        //        stringBuilder.AppendLine("  NodeIndex,");
        //        stringBuilder.AppendLine("  Permission");
        //        stringBuilder.AppendLine(") AS (");
        //        stringBuilder.AppendLine("  SELECT");
        //        stringBuilder.AppendLine($"  {nodeIndex},");
        //        stringBuilder.AppendLine($"  p.Self");
        //        stringBuilder.AppendLine($"FROM");
        //        //append here
        //        stringBuilder.AppendLine(")");
        //        stringBuilder.AppendLine("SELECT");
        //        stringBuilder.AppendLine("  NodeId,");
        //        stringBuilder.AppendLine("  Permission");
        //        stringBuilder.AppendLine("FROM");
        //        stringBuilder.AppendLine("  Nodes;");
        //    }
        //}
    }
}
