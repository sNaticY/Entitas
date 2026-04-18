using Entitas;
using Entitas.CodeGeneration.Attributes;

[Game]
public sealed class MyFlagsComponent : IComponent
{
    public MyFlags Value;
}

[System.Flags]
public enum MyFlags
{
    Item1 = 1,
    Item2 = 2,
    Item3 = 4,
    Item4 = 8
}
