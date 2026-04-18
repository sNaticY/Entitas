using System.Collections.Generic;
using Entitas;
using Entitas.CodeGeneration.Attributes;

[Game]
public sealed class MyDictionaryComponent : IComponent
{
    public Dictionary<string, string> Value;
}
