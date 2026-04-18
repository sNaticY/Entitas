using Entitas;
using Entitas.CodeGeneration.Attributes;
using UnityEngine;

[Game]
public sealed class MyAnimationCurveComponent : IComponent
{
    public AnimationCurve Value;
}
