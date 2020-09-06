using Darkengines.Expressions.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Darkengines.Expressions.Rules {
    public class PropertyOptions<TContext, T> {
        public IDictionary<object, ICollection<Func<TContext, T, object>>> InstancePropertyCustomResolvers { get; }
        public IDictionary<object, ICollection<Expression<Func<TContext, T, object>>>> InstancePropertyCustomResolverExpressions { get; }
        public IDictionary<object, ICollection<Func<TContext, object>>> PropertyCustomResolvers { get; }
        public PropertyOptions() {
            PropertyCustomResolvers = new Dictionary<object, ICollection<Func<TContext, object>>>();
            InstancePropertyCustomResolvers = new Dictionary<object, ICollection<Func<TContext, T, object>>>();
            InstancePropertyCustomResolverExpressions = new Dictionary<object, ICollection<Expression<Func<TContext, T, object>>>>();
        }
        public bool ApplyFilter { get; set; } = true;
    }
}
