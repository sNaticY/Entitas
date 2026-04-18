using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Entitas
{
    static class PublicMemberCopier
    {
        const BindingFlags BindingFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public;

        static readonly ConcurrentDictionary<Type, CopyableMembers> Cache = new();

        public static void Copy(object source, object target)
        {
            var copyableMembers = Cache.GetOrAdd(source.GetType(), CreateCopyableMembers);

            for (var i = 0; i < copyableMembers.Fields.Length; i++)
            {
                var field = copyableMembers.Fields[i];
                field.SetValue(target, field.GetValue(source));
            }

            for (var i = 0; i < copyableMembers.Properties.Length; i++)
            {
                var property = copyableMembers.Properties[i];
                property.SetValue(target, property.GetValue(source));
            }
        }

        static CopyableMembers CreateCopyableMembers(Type type)
        {
            var fields = type.GetFields(BindingFlags);
            var properties = type.GetProperties(BindingFlags);

            var copyablePropertyCount = 0;
            for (var i = 0; i < properties.Length; i++)
            {
                if (IsCopyable(properties[i]))
                    copyablePropertyCount++;
            }

            if (copyablePropertyCount == properties.Length)
                return new CopyableMembers(fields, properties);

            var copyableProperties = new PropertyInfo[copyablePropertyCount];
            var propertyIndex = 0;
            for (var i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                if (!IsCopyable(property))
                    continue;

                copyableProperties[propertyIndex++] = property;
            }

            return new CopyableMembers(fields, copyableProperties);
        }

        static bool IsCopyable(PropertyInfo property) =>
            property.CanRead
            && property.CanWrite
            && property.GetIndexParameters().Length == 0;

        readonly struct CopyableMembers
        {
            public readonly FieldInfo[] Fields;
            public readonly PropertyInfo[] Properties;

            public CopyableMembers(FieldInfo[] fields, PropertyInfo[] properties)
            {
                Fields = fields;
                Properties = properties;
            }
        }
    }
}
