using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;

namespace Entitas.Unity.Editor.Compatibility.UnityEditor
{
    public sealed class ScriptingDefineSymbols
    {
        public static readonly BuildTargetGroup[] BuildTargetGroups = Enum.GetValues(typeof(BuildTargetGroup))
            .Cast<BuildTargetGroup>()
            .Where(group => group != BuildTargetGroup.Unknown)
            .Where(group => !Attribute.IsDefined(typeof(BuildTargetGroup).GetField(group.ToString()), typeof(ObsoleteAttribute)))
            .ToArray();

        public void AddForAll(string defineSymbol)
        {
            foreach (var buildTargetGroup in BuildTargetGroups)
            {
                var symbols = RemoveSymbol(buildTargetGroup, defineSymbol);
                SetScriptingDefineSymbols(buildTargetGroup, string.IsNullOrEmpty(symbols) ? defineSymbol : symbols + ";" + defineSymbol);
            }
        }

        public void RemoveForAll(string defineSymbol)
        {
            foreach (var buildTargetGroup in BuildTargetGroups)
                RemoveSymbol(buildTargetGroup, defineSymbol);
        }

        static string RemoveSymbol(BuildTargetGroup buildTargetGroup, string defineSymbol)
        {
            var symbols = GetScriptingDefineSymbols(buildTargetGroup);
            symbols = Regex.Replace(symbols, $@"\b{Regex.Escape(defineSymbol)}\b", string.Empty);
            symbols = Regex.Replace(symbols, ";{2,}", ";").Trim(';');
            SetScriptingDefineSymbols(buildTargetGroup, symbols);
            return symbols;
        }

        static string GetScriptingDefineSymbols(BuildTargetGroup buildTargetGroup) =>
            PlayerSettings.GetScriptingDefineSymbols(global::UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup));

        static void SetScriptingDefineSymbols(BuildTargetGroup buildTargetGroup, string symbols) =>
            PlayerSettings.SetScriptingDefineSymbols(global::UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup), symbols);
    }
}
