using Entitas;
using Entitas.CodeGeneration.Attributes;
using UnityEngine;

[Game]
public sealed class MyColorComponent : IComponent
{
    public Color Value;
}
