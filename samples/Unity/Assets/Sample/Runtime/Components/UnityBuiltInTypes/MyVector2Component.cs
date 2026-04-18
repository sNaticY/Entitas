using Entitas;
using Entitas.CodeGeneration.Attributes;
using UnityEngine;

[Game]
public sealed class MyVector2Component : IComponent
{
    public Vector2 Value;
}
