using Entitas.CodeGeneration.Components.Data;
using Entitas.CodeGeneration.Extensions;

namespace Entitas.CodeGeneration.Components.Extensions;

public static class ComponentDataExtensions
{
    public static string PrefixedComponentName(this ComponentData data) =>
        data.FlagPrefix.ToLowerFirst() + data.GetComponentName(ComponentGenerationHelper.IgnoreNamespaces);

    public static string ComponentNameValidLowerFirst(this ComponentData data) =>
        data.GetComponentName().ToLowerFirst().AddPrefixIfIsKeyword();

    public static string GetComponentGetterMethodName(this ComponentData data) =>
        "Get" + data.GetComponentName();

    public static string GetHasComponentMethodName(this ComponentData data) =>
        "Has" + data.GetComponentName();

    public static string GetFlagCheckMethodName(this ComponentData data) =>
        data.PrefixedComponentName().ToUpperFirst();

    public static string GetFlagSetMethodName(this ComponentData data) =>
        "Set" + data.GetComponentName();

    public static string GetUniqueEntityGetterMethodName(this ComponentData data) =>
        data.GetComponentGetterMethodName() + "Entity";
}
