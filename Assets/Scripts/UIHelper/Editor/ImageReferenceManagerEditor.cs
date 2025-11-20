using UnityEngine;
using UnityEditor;
using Game.Utilities;

namespace Game.Editor
{
    [CustomEditor(typeof(ImageReferenceManager))]
    public class ImageReferenceManagerEditor : UnityEditor.Editor
    {
        private Vector2 scrollPosition;
        private string searchFilter = "";
        private Sprite selectedGroupSprite;

        public override void OnInspectorGUI()
        {
            ImageReferenceManager manager = (ImageReferenceManager)target;

            EditorGUILayout.Space(10);

            // Header buttons
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("Refresh References", GUILayout.Height(30)))
            {
                manager.RefreshReferences();
                EditorUtility.SetDirty(manager);
            }

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Apply All", GUILayout.Height(30)))
            {
                Undo.RecordObject(manager, "Apply All Properties");
                manager.ApplyAllProperties();
                EditorUtility.SetDirty(manager);

                // Mark prefab as dirty
                foreach (var data in manager.imageReferences)
                {
                    if (data.image != null)
                    {
                        EditorUtility.SetDirty(data.image);
                    }
                }
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // Bulk actions for Raycast Target and Maskable
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("All Raycast ON"))
            {
                Undo.RecordObject(manager, "Set All Raycast ON");
                manager.SetAllRaycastTarget(true);
                EditorUtility.SetDirty(manager);
            }

            if (GUILayout.Button("All Raycast OFF"))
            {
                Undo.RecordObject(manager, "Set All Raycast OFF");
                manager.SetAllRaycastTarget(false);
                EditorUtility.SetDirty(manager);
            }

            if (GUILayout.Button("All Maskable ON"))
            {
                Undo.RecordObject(manager, "Set All Maskable ON");
                manager.SetAllMaskable(true);
                EditorUtility.SetDirty(manager);
            }

            if (GUILayout.Button("All Maskable OFF"))
            {
                Undo.RecordObject(manager, "Set All Maskable OFF");
                manager.SetAllMaskable(false);
                EditorUtility.SetDirty(manager);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("All Both ON"))
            {
                Undo.RecordObject(manager, "Set All Both ON");
                manager.SetAllBoth(true, true);
                EditorUtility.SetDirty(manager);
            }

            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("All Both OFF"))
            {
                Undo.RecordObject(manager, "Set All Both OFF");
                manager.SetAllBoth(false, false);
                EditorUtility.SetDirty(manager);
            }
            EditorGUILayout.EndHorizontal();

            // Selected group actions
            EditorGUILayout.Space(10);
            int selectedCount = manager.GetSelectedCount();
            EditorGUILayout.LabelField($"Selected Group Actions ({selectedCount} selected)", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Select All"))
            {
                Undo.RecordObject(manager, "Select All");
                manager.SelectAll(true);
                EditorUtility.SetDirty(manager);
            }

            if (GUILayout.Button("Deselect All"))
            {
                Undo.RecordObject(manager, "Deselect All");
                manager.SelectAll(false);
                EditorUtility.SetDirty(manager);
            }

            GUI.backgroundColor = Color.magenta;
            if (GUILayout.Button("Apply Selected"))
            {
                Undo.RecordObject(manager, "Apply Selected Properties");
                manager.ApplySelectedProperties();
                EditorUtility.SetDirty(manager);

                foreach (var data in manager.imageReferences)
                {
                    if (data.isSelected && data.image != null)
                    {
                        EditorUtility.SetDirty(data.image);
                    }
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            // Selected group sprite
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Set Sprite:", GUILayout.Width(70));
            selectedGroupSprite = (Sprite)EditorGUILayout.ObjectField(selectedGroupSprite, typeof(Sprite), false, GUILayout.Width(120));

            if (GUILayout.Button("Apply to Selected", GUILayout.Width(110)))
            {
                Undo.RecordObject(manager, "Set Selected Sprite");
                manager.SetSelectedSprite(selectedGroupSprite);
                EditorUtility.SetDirty(manager);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Sel Raycast ON"))
            {
                Undo.RecordObject(manager, "Set Selected Raycast ON");
                manager.SetSelectedRaycastTarget(true);
                EditorUtility.SetDirty(manager);
            }

            if (GUILayout.Button("Sel Raycast OFF"))
            {
                Undo.RecordObject(manager, "Set Selected Raycast OFF");
                manager.SetSelectedRaycastTarget(false);
                EditorUtility.SetDirty(manager);
            }

            if (GUILayout.Button("Sel Maskable ON"))
            {
                Undo.RecordObject(manager, "Set Selected Maskable ON");
                manager.SetSelectedMaskable(true);
                EditorUtility.SetDirty(manager);
            }

            if (GUILayout.Button("Sel Maskable OFF"))
            {
                Undo.RecordObject(manager, "Set Selected Maskable OFF");
                manager.SetSelectedMaskable(false);
                EditorUtility.SetDirty(manager);
            }

            EditorGUILayout.EndHorizontal();

            // Quick set both for selected
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Sel Both ON"))
            {
                Undo.RecordObject(manager, "Set Selected Both ON");
                manager.SetSelectedBoth(true, true);
                EditorUtility.SetDirty(manager);
            }

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Sel Both OFF"))
            {
                Undo.RecordObject(manager, "Set Selected Both OFF");
                manager.SetSelectedBoth(false, false);
                EditorUtility.SetDirty(manager);
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Search filter
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            searchFilter = EditorGUILayout.TextField(searchFilter);
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                searchFilter = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Count info
            int totalCount = manager.imageReferences.Count;
            int nullSpriteCount = 0;
            foreach (var data in manager.imageReferences)
            {
                if (data.image != null && data.image.sprite == null)
                    nullSpriteCount++;
            }

            EditorGUILayout.HelpBox($"Total Images: {totalCount} | Null Sprites: {nullSpriteCount}", MessageType.Info);

            EditorGUILayout.Space(5);

            // Image list
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(500));

            for (int i = 0; i < manager.imageReferences.Count; i++)
            {
                var data = manager.imageReferences[i];

                if (data.image == null) continue;

                // Filter by search
                if (!string.IsNullOrEmpty(searchFilter))
                {
                    if (!data.path.ToLower().Contains(searchFilter.ToLower()) &&
                        !data.image.name.ToLower().Contains(searchFilter.ToLower()))
                    {
                        continue;
                    }
                }

                // Highlight selected items
                if (data.isSelected)
                {
                    GUI.backgroundColor = new Color(0.5f, 1f, 0.5f, 0.3f);
                }

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                GUI.backgroundColor = Color.white;

                // Path and select button
                EditorGUILayout.BeginHorizontal();

                // Checkbox for selection
                EditorGUI.BeginChangeCheck();
                bool isSelected = EditorGUILayout.Toggle(data.isSelected, GUILayout.Width(20));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(manager, "Toggle Selection");
                    data.isSelected = isSelected;
                    EditorUtility.SetDirty(manager);
                }

                EditorGUILayout.LabelField($"{i + 1}. {data.path}", EditorStyles.boldLabel);

                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeGameObject = data.image.gameObject;
                    EditorGUIUtility.PingObject(data.image.gameObject);

                    // Frame selected in Scene view
                    /*if (SceneView.lastActiveSceneView != null)
                    {
                        SceneView.lastActiveSceneView.FrameSelected();
                    }*/
                }

                EditorGUILayout.EndHorizontal();

                // Current sprite display
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField("Current:", GUILayout.Width(60));
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(data.image.sprite, typeof(Sprite), false, GUILayout.Width(150));
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndHorizontal();

                // New sprite field
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField("New:", GUILayout.Width(60));

                EditorGUI.BeginChangeCheck();
                Sprite newSprite = (Sprite)EditorGUILayout.ObjectField(data.newSprite, typeof(Sprite), false, GUILayout.Width(150));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(manager, "Change Sprite Reference");
                    data.newSprite = newSprite;
                    EditorUtility.SetDirty(manager);
                }

                // Apply single button
                GUI.backgroundColor = Color.yellow;
                if (GUILayout.Button("Apply", GUILayout.Width(50)))
                {
                    Undo.RecordObject(data.image, "Apply Single Properties");
                    data.image.sprite = data.newSprite;
                    data.image.raycastTarget = data.newRaycastTarget;
                    data.image.maskable = data.newMaskable;
                    EditorUtility.SetDirty(data.image);
                }
                GUI.backgroundColor = Color.white;

                // Copy current to new
                if (GUILayout.Button("Reset", GUILayout.Width(50)))
                {
                    Undo.RecordObject(manager, "Reset Sprite Reference");
                    data.newSprite = data.image.sprite;
                    data.newRaycastTarget = data.image.raycastTarget;
                    data.newMaskable = data.image.maskable;
                    EditorUtility.SetDirty(manager);
                }

                EditorGUILayout.EndHorizontal();

                // Raycast Target and Maskable toggles
                EditorGUILayout.BeginHorizontal();

                // Current values (disabled)
                EditorGUILayout.LabelField("Current:", GUILayout.Width(60));
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ToggleLeft("Raycast", data.image.raycastTarget, GUILayout.Width(70));
                EditorGUILayout.ToggleLeft("Maskable", data.image.maskable, GUILayout.Width(80));
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();

                // New values
                EditorGUILayout.LabelField("New:", GUILayout.Width(60));

                EditorGUI.BeginChangeCheck();
                bool newRaycast = EditorGUILayout.ToggleLeft("Raycast", data.newRaycastTarget, GUILayout.Width(70));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(manager, "Change Raycast Target");
                    data.newRaycastTarget = newRaycast;
                    EditorUtility.SetDirty(manager);
                }

                EditorGUI.BeginChangeCheck();
                bool newMaskable = EditorGUILayout.ToggleLeft("Maskable", data.newMaskable, GUILayout.Width(80));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(manager, "Change Maskable");
                    data.newMaskable = newMaskable;
                    EditorUtility.SetDirty(manager);
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            // Utility buttons
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Clear All New Sprites"))
            {
                Undo.RecordObject(manager, "Clear All New Sprites");
                foreach (var data in manager.imageReferences)
                {
                    data.newSprite = null;
                }
                EditorUtility.SetDirty(manager);
            }

            if (GUILayout.Button("Copy Current to New"))
            {
                Undo.RecordObject(manager, "Copy Current to New");
                foreach (var data in manager.imageReferences)
                {
                    if (data.image != null)
                    {
                        data.newSprite = data.image.sprite;
                        data.newRaycastTarget = data.image.raycastTarget;
                        data.newMaskable = data.image.maskable;
                    }
                }
                EditorUtility.SetDirty(manager);
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
