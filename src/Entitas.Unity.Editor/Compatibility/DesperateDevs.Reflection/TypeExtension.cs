using System;
using System.Collections.Generic;
using System.Reflection;

namespace Entitas.Unity.Editor.Compatibility.Reflection
{
    public static class TypeExtension
    {
        const BindingFlags BindingFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public;

        public static PublicMemberInfo[] GetPublicMemberInfos(this Type type)
        {
            var members = new List<PublicMemberInfo>();

            foreach (var fieldInfo in type.GetFields(BindingFlags))
                members.Add(new PublicMemberInfo(fieldInfo));

            foreach (var propertyInfo in type.GetProperties(BindingFlags))
            {
                if (propertyInfo.CanRead && propertyInfo.CanWrite && propertyInfo.GetIndexParameters().Length == 0)
                    members.Add(new PublicMemberInfo(propertyInfo));
            }

            return members.ToArray();
        }

        public static bool ImplementsInterface<T>(this Type type) =>
            !type.IsInterface && typeof(T).IsAssignableFrom(type);
    }
}
