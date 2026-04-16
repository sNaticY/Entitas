using System.Collections.Immutable;
using System.Text;
using Entitas.CodeGeneration.Contexts.Data;
using Entitas.CodeGeneration.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Entitas.CodeGeneration.VisualDebugging.ContextObserver
{
    public static class ContextObserverGenerationHelper
    {
        public static void GenerateContextObservers(SourceProductionContext spc,
            in ImmutableArray<ContextData> contexts)
        {
            var contextObservers = string.Join("\n", contexts
                .Select(context => ContextObserverTemplates.ContextObserverTemplate
                    .Replace("${contextName}", context.ContextName.ToLowerFirst())));

            var source = ContextObserverTemplates.ContextsTemplate
                .Replace("${contextObservers}", contextObservers);
            
            spc.AddSource("ContextObservers.g.cs", SourceText.From(source, Encoding.UTF8));
        }
    }
}
