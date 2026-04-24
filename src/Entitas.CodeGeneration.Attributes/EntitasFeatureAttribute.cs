using System;

namespace Entitas.CodeGeneration.Attributes
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class EntitasFeatureAttribute : Attribute
    {
        public readonly string Name;

        public EntitasFeatureAttribute(string name)
        {
            Name = name;
        }
    }
}
