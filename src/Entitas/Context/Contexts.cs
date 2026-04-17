using System;
using System.Collections.Generic;
using System.Linq;

namespace Entitas
{
    public partial class Contexts
    {
        readonly Dictionary<Type, IContext> _contexts = new Dictionary<Type, IContext>();

        public IContext[] AllContexts => _contexts.Values.ToArray();

        public Contexts Register<TContext>(TContext context) where TContext : class, IContext
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var contextType = typeof(TContext);
            if (_contexts.ContainsKey(contextType))
                throw new InvalidOperationException($"A context of type '{contextType.FullName}' is already registered.");

            _contexts.Add(contextType, context);
            return this;
        }

        public TContext Get<TContext>() where TContext : class, IContext
        {
            if (!TryGet<TContext>(out var context))
                throw new InvalidOperationException($"No context of type '{typeof(TContext).FullName}' is registered.");

            return context;
        }

        public bool TryGet<TContext>(out TContext context) where TContext : class, IContext
        {
            if (_contexts.TryGetValue(typeof(TContext), out var value))
            {
                context = (TContext)value;
                return true;
            }

            context = null;
            return false;
        }
    }
}
