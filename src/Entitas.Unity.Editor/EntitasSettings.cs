using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Entitas.Unity.Editor
{
    public sealed class EntitasSettings : ScriptableObject
    {
        const string SettingsAssetPath = "Assets/Editor/EntitasSettings.asset";

        public static EntitasSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = AssetDatabase.LoadAssetAtPath<EntitasSettings>(SettingsAssetPath);

                    if (_instance == null)
                    {
                        var guid = AssetDatabase.FindAssets($"l:{nameof(EntitasSettings)}").FirstOrDefault();
                        if (guid != null)
                            _instance = AssetDatabase.LoadAssetAtPath<EntitasSettings>(AssetDatabase.GUIDToAssetPath(guid));
                    }

                    if (_instance == null)
                    {
                        _instance = CreateInstance<EntitasSettings>();

                        if (!_createQueued)
                        {
                            _createQueued = true;
                            EditorApplication.delayCall += CreateSettingsAsset;
                        }
                    }
                }

                return _instance;
            }
        }

        static EntitasSettings _instance;
        static bool _createQueued;

        static void CreateSettingsAsset()
        {
            _createQueued = false;

            if (AssetDatabase.LoadAssetAtPath<EntitasSettings>(SettingsAssetPath) != null)
                return;

            var existing = AssetDatabase.FindAssets($"l:{nameof(EntitasSettings)}").FirstOrDefault();
            if (existing != null)
            {
                _instance = AssetDatabase.LoadAssetAtPath<EntitasSettings>(AssetDatabase.GUIDToAssetPath(existing));
                return;
            }

            var settings = CreateInstance<EntitasSettings>();
            AssetDatabase.CreateAsset(settings, SettingsAssetPath);
            _instance = AssetDatabase.LoadAssetAtPath<EntitasSettings>(SettingsAssetPath);
        }

        [Tooltip("Duration in milliseconds after which a system is considered to be slow. The system will be highlighted in the hierarchy view.")]
        public int SystemWarningThreshold = 5;
    }
}
