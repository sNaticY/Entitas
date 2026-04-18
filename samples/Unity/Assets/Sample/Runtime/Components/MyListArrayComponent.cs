using System.Collections.Generic;
using Entitas;
using Entitas.CodeGeneration.Attributes;

[Game]
public sealed class MyListArrayComponent : IComponent
{
    public List<string>[] Value;
}
