using Entitas;
using Entitas.CodeGeneration.Attributes;
using UnityEngine;

[Game]
public sealed class MyBoundsComponent : IComponent
{
    public Bounds Value;
}
