using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Entitas.CodeGeneration.VisualDebugging.Feature
{
    public static class FeatureGenerationHelper
    {
        public static void GenerateFeatureClass(SourceProductionContext spc)
        {
            spc.AddSource("Feature.g.cs", 
                SourceText.From(FeatureTemplates.FeatureTemplate, Encoding.UTF8));
        }
    }
}
