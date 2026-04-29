using Entitas.CodeGeneration.Extensions;

namespace Entitas.CodeGeneration.Components.Extensions;

public static class EntitasStringExtensions
{
    public const string ContextSuffix = "Context";
    public const string EntitySuffix = "Entity";
    public const string ComponentSuffix = "Component";
    public const string SystemSuffix = "System";
    public const string MatcherSuffix = "Matcher";
    public const string ListenerSuffix = "Listener";

    public static string AddContextSuffix(this string str) => str.AddSuffix(ContextSuffix);
    public static string RemoveContextSuffix(this string str) => str.RemoveLast(ContextSuffix);
    public static bool HasContextSuffix(this string str) => str.HasSuffix(ContextSuffix);

    public static string AddEntitySuffix(this string str) => str.AddSuffix(EntitySuffix);
    public static string RemoveEntitySuffix(this string str) => str.RemoveLast(EntitySuffix);
    public static bool HasEntitySuffix(this string str) => str.HasSuffix(EntitySuffix);

    public static string AddComponentSuffix(this string str) => str.AddSuffix(ComponentSuffix);
    public static string RemoveComponentSuffix(this string str) => str.RemoveLast(ComponentSuffix);
    public static bool HasComponentSuffix(this string str) => str.HasSuffix(ComponentSuffix);

    public static string AddSystemSuffix(this string str) => str.AddSuffix(SystemSuffix);
    public static string RemoveSystemSuffix(this string str) => str.RemoveLast(SystemSuffix);
    public static bool HasSystemSuffix(this string str) => str.HasSuffix(SystemSuffix);

    public static string AddMatcherSuffix(this string str) => str.AddSuffix(MatcherSuffix);
    public static string RemoveMatcherSuffix(this string str) => str.RemoveLast(MatcherSuffix);
    public static bool HasMatcherSuffix(this string str) => str.HasSuffix(MatcherSuffix);

    public static string AddListenerSuffix(this string str) => str.AddSuffix(ListenerSuffix);
    public static string RemoveListenerSuffix(this string str) => str.RemoveLast(ListenerSuffix);
    public static bool HasListenerSuffix(this string str) => str.HasSuffix(ListenerSuffix);
}
