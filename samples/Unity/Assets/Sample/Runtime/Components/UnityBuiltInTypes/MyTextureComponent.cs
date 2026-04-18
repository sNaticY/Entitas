using Entitas;
using Entitas.CodeGeneration.Attributes;
using UnityEngine;

[Game]
public sealed class MyTextureComponent : IComponent
{
    public Texture Value;
}
