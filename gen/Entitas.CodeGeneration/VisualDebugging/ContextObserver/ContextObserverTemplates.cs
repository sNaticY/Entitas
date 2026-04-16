namespace Entitas.CodeGeneration.VisualDebugging.ContextObserver;

public static class ContextObserverTemplates
{
    public const string ContextsTemplate =
        @"public partial class Contexts
{
#if (!ENTITAS_DISABLE_VISUAL_DEBUGGING && UNITY_EDITOR)

    [Entitas.CodeGeneration.Attributes.PostConstructor]
    public void InitializeContextObservers()
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

    public void CreateContextObserver(Entitas.IContext context)
    {
        if (UnityEngine.Application.isPlaying)
        {
            var observer = new Entitas.VisualDebugging.Unity.ContextObserver(context);
            UnityEngine.Object.DontDestroyOnLoad(observer.gameObject);
        }
    }

#endif
}
";

    public const string ContextObserverTemplate = @"            CreateContextObserver(${contextName});";

}