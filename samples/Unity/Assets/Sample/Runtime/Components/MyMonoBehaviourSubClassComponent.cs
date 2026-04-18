using Entitas;
using Entitas.CodeGeneration.Attributes;
using UnityEngine;

[Game]
public sealed class MyMonoBehaviourSubClassComponent : IComponent
{
    public MyMonoBehaviourSubClass Value;
}

public class MyMonoBehaviourSubClass : MonoBehaviour { }
