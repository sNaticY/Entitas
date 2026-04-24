using System.Collections.Generic;
using Entitas;
using Entitas.CodeGeneration.Attributes;

public sealed class MyListComponent : IComponent
{
    public List<string> Value;
}
