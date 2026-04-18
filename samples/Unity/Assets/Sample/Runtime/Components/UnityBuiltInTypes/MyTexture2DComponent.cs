using Entitas;
using Entitas.CodeGeneration.Attributes;
using UnityEngine;

[Game]
public sealed class MyTexture2DComponent : IComponent
{
    public Texture2D Value;
}
