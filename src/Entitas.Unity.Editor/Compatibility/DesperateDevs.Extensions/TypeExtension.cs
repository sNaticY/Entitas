using System;
using System.Collections.Generic;
using System.Text;

namespace DesperateDevs.Extensions
{
    public static class TypeExtension
    {
        static readonly Dictionary<Type, string> SpecialTypeNames = new Dictionary<Type, string>
        {
            { typeof(bool), "bool" },
            { typeof(byte), "byte" },
            { typeof(sbyte), "sbyte" },
            { typeof(char), "char" },
            { typeof(decimal), "decimal" },
            { typeof(double), "double" },
            { typeof(float), "float" },
            { typeof(int), "int" },
            { typeof(uint), "uint" },
            { typeof(long), "long" },
            { typeof(ulong), "ulong" },
            { typeof(object), "object" },
            { typeof(short), "short" },
            { typeof(ushort), "ushort" },
            { typeof(string), "string" },
            { typeof(void), "void" }
        };

        public static string ToCompilableString(this Type type)
        {
            if (SpecialTypeNames.TryGetValue(type, out var specialTypeName))
                return specialTypeName;

            if (type.IsArray)
            {
                var commas = new string(',', type.GetArrayRank() - 1);
                return $"{type.GetElementType().ToCompilableString()}[{commas}]";
            }

            if (type.IsGenericParameter)
                return type.Name;

            if (type.IsGenericType)
            {
                var genericTypeDefinition = type.GetGenericTypeDefinition();
                var typeName = GetNonGenericTypeName(genericTypeDefinition);
                var genericArguments = type.GetGenericArguments();
                var builder = new StringBuilder();
                builder.Append(typeName);
                builder.Append('<');
                for (var i = 0; i < genericArguments.Length; i++)
                {
                    if (i > 0)
                        builder.Append(", ");

                    builder.Append(genericArguments[i].ToCompilableString());
                }

                builder.Append('>');
                return builder.ToString();
            }

            return GetNonGenericTypeName(type);
        }

        public static string TypeName(this string fullTypeName)
        {
            var index = fullTypeName.LastIndexOf('.') + 1;
            return fullTypeName.Substring(index, fullTypeName.Length - index);
        }

        static string GetNonGenericTypeName(Type type)
        {
            var fullName = type.FullName ?? type.Name;
            var tickIndex = fullName.IndexOf('`');
            if (tickIndex >= 0)
                fullName = fullName.Substring(0, tickIndex);

            return fullName.Replace('+', '.');
        }
    }
}
