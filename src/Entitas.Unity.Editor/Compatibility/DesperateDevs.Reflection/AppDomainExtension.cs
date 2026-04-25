using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Entitas.Unity.Editor.Compatibility.Reflection
{
    public static class AppDomainExtension
    {
        public static IEnumerable<Type> GetAllTypes(this AppDomain appDomain)
        {
            foreach (var assembly in appDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException exception)
                {
                    types = exception.Types;
                }

                foreach (var type in types)
                {
                    if (type != null)
                        yield return type;
                }
            }
        }

        public static IEnumerable<T> GetInstancesOf<T>(this AppDomain appDomain)
        {
            foreach (var type in appDomain.GetAllTypes())
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;

                if (!typeof(T).IsAssignableFrom(type))
                    continue;

                T instance;
                try
                {
                    if (!(Activator.CreateInstance(type) is T createdInstance))
                        continue;

                    instance = createdInstance;
                }
                catch
                {
                    // Ignore unconstructable types to match discovery behavior.
                    continue;
                }

                yield return instance;
            }
        }
    }
}
