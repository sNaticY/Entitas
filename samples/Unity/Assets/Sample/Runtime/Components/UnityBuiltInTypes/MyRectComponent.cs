using Entitas;
using Entitas.CodeGeneration.Attributes;
using UnityEngine;

[Game]
public sealed class MyRectComponent : IComponent
{
    public Rect Value;
}
