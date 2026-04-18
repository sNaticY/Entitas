using System;
using Entitas;
using Entitas.CodeGeneration.Attributes;

[Game]
public sealed class MyDateTimeComponent : IComponent
{
    public DateTime Value;
}
