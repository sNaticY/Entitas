using Entitas.CodeGeneration.Attributes;

namespace Sample.MultiAssembly.Root
{
    public sealed class SharedAttribute : ContextAttribute
    {
        public SharedAttribute() : base("Shared") { }
    }
}
