using System.Collections.Generic;
using Entitas;
using Entitas.CodeGeneration.Attributes;

[Game]
public sealed class MyDictArrayComponent : IComponent
{
    public Dictionary<int, string[]> Dict;
    public Dictionary<int, string[]>[] DictArray;
}
