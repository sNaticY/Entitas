using Entitas;
using Entitas.CodeGeneration.Attributes;
using UnityEngine;

[Game]
public sealed class MyVector3Component : IComponent
{
    public Vector3 Value;
}
