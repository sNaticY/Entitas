using System.Collections.Immutable;
using Entitas.CodeGeneration.Contexts.Data;
using Entitas.CodeGeneration.VisualDebugging.ContextObserver;
using Entitas.CodeGeneration.VisualDebugging.Feature;
using Microsoft.CodeAnalysis;

namespace Entitas.CodeGeneration.VisualDebugging
{
    public static class VisualDebuggingGenerationHelper
    {
        public static void Generate(SourceProductionContext spc,
            in ImmutableArray<ContextData> contexts)
        {
            FeatureGenerationHelper.GenerateFeatureClass(spc);
            ContextObserverGenerationHelper.GenerateContextObservers(spc, contexts);
        }
    }
}
