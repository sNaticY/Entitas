namespace Entitas.CodeGeneration.VisualDebugging.Feature;

public static class FeatureTemplates
{
    public const string FeatureTemplate = 
        @"#if (!ENTITAS_DISABLE_VISUAL_DEBUGGING && UNITY_EDITOR)

public class Feature : Entitas.Unity.DebugSystems
{
    public Feature(string name) : base(name)
    {
    }

    public Feature() : base(true)
    {
        var readableType = ToSpacedCamelCase(GetShortTypeName(GetType()));
        Initialize(readableType);
    }

    static string GetShortTypeName(System.Type type)
    {
        var typeName = type.Name;
        var genericIndex = typeName.IndexOf('`');
        return genericIndex >= 0
            ? typeName.Substring(0, genericIndex)
            : typeName;
    }

    static string ToSpacedCamelCase(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var builder = new System.Text.StringBuilder(value.Length * 2);
        builder.Append(value[0]);

        for (int i = 1; i < value.Length; i++)
        {
            var current = value[i];
            var previous = value[i - 1];
            var next = i + 1 < value.Length ? value[i + 1] : '\0';

            if (char.IsUpper(current) &&
                (char.IsLower(previous) || char.IsDigit(previous) || (char.IsUpper(previous) && char.IsLower(next))))
            {
                builder.Append(' ');
            }

            builder.Append(current);
        }

        return builder.ToString();
    }
}

#elif (!ENTITAS_DISABLE_DEEP_PROFILING && DEVELOPMENT_BUILD)

public class Feature : Entitas.Systems
{
    System.Collections.Generic.List<string> _initializeSystemNames;
    System.Collections.Generic.List<string> _executeSystemNames;
    System.Collections.Generic.List<string> _cleanupSystemNames;
    System.Collections.Generic.List<string> _tearDownSystemNames;

    public Feature(string name) : this()
    {
    }

    public Feature()
    {
        _initializeSystemNames = new System.Collections.Generic.List<string>();
        _executeSystemNames = new System.Collections.Generic.List<string>();
        _cleanupSystemNames = new System.Collections.Generic.List<string>();
        _tearDownSystemNames = new System.Collections.Generic.List<string>();
    }

    public override Entitas.Systems Add(Entitas.ISystem system)
    {
        var systemName = system.GetType().FullName;

        if (system is Entitas.IInitializeSystem)
        {
            _initializeSystemNames.Add(systemName);
        }

        if (system is Entitas.IExecuteSystem)
        {
            _executeSystemNames.Add(systemName);
        }

        if (system is Entitas.ICleanupSystem)
        {
            _cleanupSystemNames.Add(systemName);
        }

        if (system is Entitas.ITearDownSystem)
        {
            _tearDownSystemNames.Add(systemName);
        }

        return base.Add(system);
    }

    public override void Initialize()
    {
        for (int i = 0; i < _initializeSystems.Count; i++)
        {
            UnityEngine.Profiling.Profiler.BeginSample(_initializeSystemNames[i]);
            _initializeSystems[i].Initialize();
            UnityEngine.Profiling.Profiler.EndSample();
        }
    }

    public override void Execute()
    {
        for (int i = 0; i < _executeSystems.Count; i++)
        {
            UnityEngine.Profiling.Profiler.BeginSample(_executeSystemNames[i]);
            _executeSystems[i].Execute();
            UnityEngine.Profiling.Profiler.EndSample();
        }
    }

    public override void Cleanup()
    {
        for (int i = 0; i < _cleanupSystems.Count; i++)
        {
            UnityEngine.Profiling.Profiler.BeginSample(_cleanupSystemNames[i]);
            _cleanupSystems[i].Cleanup();
            UnityEngine.Profiling.Profiler.EndSample();
        }
    }

    public override void TearDown()
    {
        for (int i = 0; i < _tearDownSystems.Count; i++)
        {
            UnityEngine.Profiling.Profiler.BeginSample(_tearDownSystemNames[i]);
            _tearDownSystems[i].TearDown();
            UnityEngine.Profiling.Profiler.EndSample();
        }
    }
}

#else

public class Feature : Entitas.Systems
{
    public Feature(string name)
    {
    }

    public Feature()
    {
    }
}

#endif
";
}
