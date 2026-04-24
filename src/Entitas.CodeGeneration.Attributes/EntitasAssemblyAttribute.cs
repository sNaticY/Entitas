using System;

namespace Entitas.CodeGeneration.Attributes
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class EntitasAssemblyAttribute : Attribute
    {
        public readonly string Name;

        public EntitasAssemblyAttribute(string name)
        {
            Name = name;
        }
    }
}
