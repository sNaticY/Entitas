namespace Entitas.CodeGeneration.VisualDebugging.ContextObserver;

public static class ContextObserverTemplates
{
    public const string ContextsTemplate =
        @"namespace Entitas
{
public static class ContextsVisualDebuggingExtension
{
#if (!ENTITAS_DISABLE_VISUAL_DEBUGGING && UNITY_EDITOR)

    public static void InitializeContextObservers(this global::Entitas.Contexts contexts)
    {
        try
        {
${contextObservers}
        }
        catch(System.Exception e)
        {
            UnityEngine.Debug.LogError(e);
        }
    }

    public static void InitializeAllContextObservers(this global::Entitas.Contexts contexts)
    {
        try
        {
            foreach (var context in contexts.AllContexts)
            {
                CreateContextObserver(context);
            }
        }
        catch(System.Exception e)
        {
            UnityEngine.Debug.LogError(e);
        }
    }

    static void CreateContextObserver(global::Entitas.IContext context)
    {
        if (UnityEngine.Application.isPlaying)
        {
            global::Entitas.Unity.ContextObserverExtension.CreateContextObserver(context);
        }
    }

#endif
}
}
";

    public const string ContextObserverTemplate = @"            CreateContextObserver(contexts.Get${ContextName}());";

}
