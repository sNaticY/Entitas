using System.Collections.Generic;
using Entitas;
using Entitas.CodeGeneration.Attributes;

[Game]
public sealed class MyHashSetComponent : IComponent
{
    public HashSet<string> Value;
}
