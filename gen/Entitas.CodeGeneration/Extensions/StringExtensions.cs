using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

namespace Entitas.CodeGeneration.Extensions;

public static class StringExtensions
{
    const string KeywordPrefix = "@"; // ex: 'class' variable name is illegal, @class is ok
    
    public static string ToUpperFirst(this string str)
    {
        return !string.IsNullOrEmpty(str) ? char.ToUpper(str[0]).ToString() + str.Substring(1) : str;
    }

    public static string ToLowerFirst(this string str)
    {
        return !string.IsNullOrEmpty(str) ? char.ToLower(str[0]).ToString() + str.Substring(1) : str;
    }
    
    public static string AddPrefixIfIsKeyword(this string name)
    {
        if (!SyntaxFacts.IsValidIdentifier(name))
            name = KeywordPrefix + name;

        return name;
    }

    public static bool HasSuffix(this string str, string suffix) =>
        str.EndsWith(suffix, System.StringComparison.Ordinal);

    public static string AddSuffix(this string str, string suffix) =>
        str.HasSuffix(suffix) ? str : str + suffix;

    public static string RemoveLast(this string str, string pattern) =>
        str.HasSuffix(pattern) ? str.Substring(0, str.Length - pattern.Length) : str;

    public static string WrapInNamespace(this string content, string? namespaceName)
    {
        if (string.IsNullOrWhiteSpace(namespaceName))
            return content;

        var indentedContent = string.Join(
            "\n",
            content.Split('\n').Select(static line => line.Length == 0 ? line : $"    {line}"));

        return $"namespace {namespaceName}\n{{\n{indentedContent}\n}}\n";
    }

    public static string NamespacedHintName(this string hintName, string? namespaceName) =>
        string.IsNullOrWhiteSpace(namespaceName)
            ? hintName
            : $"{namespaceName}.{hintName}";

    public static string Qualify(this string typeName, string? namespaceName, bool global = false)
    {
        if (string.IsNullOrWhiteSpace(namespaceName))
            return typeName;

        return global ? $"global::{namespaceName}.{typeName}" : $"{namespaceName}.{typeName}";
    }
}
