using Entitas;
using Entitas.CodeGeneration.Attributes;
using UnityEngine;

[Game]
public sealed class MyGameObjectComponent : IComponent
{
    public GameObject Value;
}
