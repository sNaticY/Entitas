using DesperateDevs.Unity.Editor;
using UnityEditor;
using UnityEngine;

namespace Entitas.Unity.Editor
{
    [InitializeOnLoad]
    public static class EntitasHierarchyIcon
    {
        static Texture2D GetIcon(Texture2D preferred, string fallbackIconName) =>
            preferred ?? (EditorGUIUtility.IconContent(fallbackIconName).image as Texture2D);

        static Texture2D ContextHierarchyIcon
        {
            get
            {
                if (_contextHierarchyIcon == null)
                    _contextHierarchyIcon = EditorLayout.LoadTexture("l:EntitasContextHierarchyIcon");

                return GetIcon(_contextHierarchyIcon, "SceneAsset Icon");
            }
        }

        static Texture2D ContextErrorHierarchyIcon
        {
            get
            {
                if (_contextErrorHierarchyIcon == null)
                    _contextErrorHierarchyIcon = EditorLayout.LoadTexture("l:EntitasContextErrorHierarchyIcon");

                return GetIcon(_contextErrorHierarchyIcon, "console.warnicon.sml");
            }
        }

        static Texture2D EntityHierarchyIcon
        {
            get
            {
                if (_entityHierarchyIcon == null)
                    _entityHierarchyIcon = EditorLayout.LoadTexture("l:EntitasEntityHierarchyIcon");

                return GetIcon(_entityHierarchyIcon, "GameObject Icon");
            }
        }

        static Texture2D EntityErrorHierarchyIcon
        {
            get
            {
                if (_entityErrorHierarchyIcon == null)
                    _entityErrorHierarchyIcon = EditorLayout.LoadTexture("l:EntitasEntityErrorHierarchyIcon");

                return GetIcon(_entityErrorHierarchyIcon, "console.warnicon.sml");
            }
        }

        static Texture2D EntityLinkHierarchyIcon
        {
            get
            {
                if (_entityLinkHierarchyIcon == null)
                    _entityLinkHierarchyIcon = EditorLayout.LoadTexture("l:EntitasEntityLinkHierarchyIcon");

                return GetIcon(_entityLinkHierarchyIcon, "Prefab Icon");
            }
        }

        static Texture2D EntityLinkWarnHierarchyIcon
        {
            get
            {
                if (_entityLinkWarnHierarchyIcon == null)
                    _entityLinkWarnHierarchyIcon = EditorLayout.LoadTexture("l:EntitasEntityLinkWarnHierarchyIcon");

                return GetIcon(_entityLinkWarnHierarchyIcon, "console.warnicon.sml");
            }
        }

        static Texture2D SystemsHierarchyIcon
        {
            get
            {
                if (_systemsHierarchyIcon == null)
                    _systemsHierarchyIcon = EditorLayout.LoadTexture("l:EntitasSystemsHierarchyIcon");

                return GetIcon(_systemsHierarchyIcon, "cs Script Icon");
            }
        }

        static Texture2D SystemsWarnHierarchyIcon
        {
            get
            {
                if (_systemsWarnHierarchyIcon == null)
                    _systemsWarnHierarchyIcon = EditorLayout.LoadTexture("l:EntitasSystemsWarnHierarchyIcon");

                return GetIcon(_systemsWarnHierarchyIcon, "console.warnicon.sml");
            }
        }

        static Texture2D _contextHierarchyIcon;
        static Texture2D _contextErrorHierarchyIcon;
        static Texture2D _entityHierarchyIcon;
        static Texture2D _entityErrorHierarchyIcon;
        static Texture2D _entityLinkHierarchyIcon;
        static Texture2D _entityLinkWarnHierarchyIcon;
        static Texture2D _systemsHierarchyIcon;
        static Texture2D _systemsWarnHierarchyIcon;

        static readonly int SystemWarningThreshold;

        static EntitasHierarchyIcon()
        {
            SystemWarningThreshold = EntitasSettings.Instance.SystemWarningThreshold;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemOnGUI;
        }

        static void OnHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            var gameObject = EditorUtility.EntityIdToObject(instanceID) as GameObject;
            if (gameObject == null)
                return;

            const float iconSize = 16f;
            const float iconOffset = iconSize + 2f;
            var rect = new Rect(selectionRect.x + selectionRect.width - iconOffset, selectionRect.y, iconSize, iconSize);

            if (gameObject.TryGetComponent<ContextObserverBehaviour>(out var contextObserver))
            {
                GUI.DrawTexture(rect, contextObserver.Context.RetainedEntitiesCount != 0
                    ? ContextErrorHierarchyIcon
                    : ContextHierarchyIcon);

                return;
            }

            if (gameObject.TryGetComponent<EntityBehaviour>(out var entityBehaviour))
            {
                GUI.DrawTexture(rect, entityBehaviour.Entity.IsEnabled
                    ? EntityHierarchyIcon
                    : EntityErrorHierarchyIcon);
                return;
            }

            if (gameObject.TryGetComponent<EntityLink>(out var entityLink))
            {
                GUI.DrawTexture(rect, entityLink.Entity != null
                    ? EntityLinkHierarchyIcon
                    : EntityLinkWarnHierarchyIcon);

                return;
            }

            if (gameObject.TryGetComponent<DebugSystemsBehaviour>(out var debugSystemsBehaviour))
            {
                GUI.DrawTexture(rect, debugSystemsBehaviour.Systems.ExecuteDuration < SystemWarningThreshold
                    ? SystemsHierarchyIcon
                    : SystemsWarnHierarchyIcon);

                return;
            }
        }
    }
}
