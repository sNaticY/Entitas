using Entitas;
using Entitas.CodeGeneration.Attributes;
using UnityEngine;

[Game]
public sealed class MyVector4Component : IComponent
{
    public Vector4 Value;
}
