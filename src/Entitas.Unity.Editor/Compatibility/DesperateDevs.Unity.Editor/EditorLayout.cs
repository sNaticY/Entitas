using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Entitas.Unity.Editor.Compatibility.UnityEditor
{
    public static class EditorLayout
    {
        static GUIStyle _sectionHeader;
        static GUIStyle _sectionContent;
        static GUIStyle _toolbarSearchTextField;
        static GUIStyle _toolbarSearchCancelButton;

        static GUIStyle SectionHeader => _sectionHeader ??= new GUIStyle("OL Title");
        static GUIStyle SectionContent => _sectionContent ??= new GUIStyle("OL Box") { stretchHeight = false };
        static GUIStyle ToolbarSearchTextField => _toolbarSearchTextField ??= GUI.skin.FindStyle("ToolbarSearchTextField") ?? EditorStyles.toolbarSearchField;
        static GUIStyle ToolbarSearchCancelButton => _toolbarSearchCancelButton ??= GUI.skin.FindStyle("ToolbarSearchCancelButton") ?? GUI.skin.FindStyle("ToolbarSearchCancelButtonEmpty") ?? GUI.skin.button;

        public static bool DrawSectionHeaderToggle(string header, bool value) => GUILayout.Toggle(value, header, SectionHeader);

        public static void BeginSectionContent() => EditorGUILayout.BeginVertical(SectionContent);

        public static void EndSectionContent() => EditorGUILayout.EndVertical();

        public static Rect BeginVerticalBox() => EditorGUILayout.BeginVertical(GUI.skin.box);

        public static void EndVerticalBox() => EditorGUILayout.EndVertical();

        public static Texture2D LoadTexture(string label)
        {
            var guid = AssetDatabase.FindAssets(label).FirstOrDefault();
            if (string.IsNullOrEmpty(guid))
                return null;

            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        public static bool MiniButton(string content) => DrawMiniButton(content, EditorStyles.miniButton);
        public static bool MiniButtonLeft(string content) => DrawMiniButton(content, EditorStyles.miniButtonLeft);
        public static bool MiniButtonMid(string content) => DrawMiniButton(content, EditorStyles.miniButtonMid);
        public static bool MiniButtonRight(string content) => DrawMiniButton(content, EditorStyles.miniButtonRight);

        static bool DrawMiniButton(string content, GUIStyle style)
        {
            var options = content.Length == 1 ? new[] { GUILayout.Width(19f) } : Array.Empty<GUILayoutOption>();
            var pressed = GUILayout.Button(content, style, options);
            if (pressed)
                GUI.FocusControl(null);

            return pressed;
        }

        public static bool Foldout(bool foldout, string content, GUIStyle style, int leftMargin = 11)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(leftMargin);
            foldout = EditorGUILayout.Foldout(foldout, content, true, style);
            EditorGUILayout.EndHorizontal();
            return foldout;
        }

        public static string SearchTextField(string searchString)
        {
            var wasChanged = GUI.changed;

            EditorGUILayout.BeginHorizontal();
            searchString = GUILayout.TextField(searchString, ToolbarSearchTextField);
            if (GUILayout.Button(string.Empty, ToolbarSearchCancelButton))
                searchString = string.Empty;
            EditorGUILayout.EndHorizontal();

            GUI.changed = wasChanged;
            return searchString;
        }

        public static bool MatchesSearchString(string str, string searchString)
        {
            if (string.IsNullOrEmpty(searchString))
                return true;

            var searches = searchString.Split(' ');
            for (var i = 0; i < searches.Length; i++)
            {
                var search = searches[i];
                if (!string.IsNullOrEmpty(search) && !str.Contains(search, StringComparison.Ordinal))
                    return false;
            }

            return true;
        }
    }
}
