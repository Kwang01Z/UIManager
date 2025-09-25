using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(LayerReferenceSO))]
public class LayerReferenceSOEditor : Editor
{
    private string searchFilter = "";
    private bool showDictionary = false;
    private Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        LayerReferenceSO layerReferenceSO = (LayerReferenceSO)target;
        
        // Header
        EditorGUILayout.Space();
        GUILayout.Label("Layer Reference Configuration", EditorStyles.boldLabel);
        
        // Search field
        EditorGUILayout.Space();
        searchFilter = EditorGUILayout.TextField("Search by LayerType:", searchFilter);
        
        // Display filtered list
        SerializedProperty layerReferenceListProperty = serializedObject.FindProperty("layerReferenceList");
        if (layerReferenceListProperty != null)
        {
            // Check if we need to delete an item first
            int itemToDelete = -1;
            string itemToDeleteName = "";
            
            // Get filtered list based on search
            var filteredIndices = GetFilteredIndices(layerReferenceListProperty, searchFilter);
            
            // Header with count
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Layer References ({filteredIndices.Count} of {layerReferenceListProperty.arraySize})", EditorStyles.boldLabel);
            if (GUILayout.Button("+", GUILayout.Width(25)))
            {
                AddNewItem(layerReferenceListProperty);
                serializedObject.ApplyModifiedProperties();
                return; // Exit to refresh the GUI
            }
            EditorGUILayout.EndHorizontal();
            
            // Display filtered items and handle deletion
            for (int i = 0; i < filteredIndices.Count; i++)
            {
                int index = filteredIndices[i];
                
                // Get properties for this item
                SerializedProperty item = layerReferenceListProperty.GetArrayElementAtIndex(index);
                SerializedProperty layerTypeProperty = item.FindPropertyRelative("layerType");
                
                if (layerTypeProperty != null)
                {
                    string layerTypeString = layerTypeProperty.enumDisplayNames[layerTypeProperty.enumValueIndex];
                    
                    // Check if this item should be deleted
                    string foldoutKey = $"{target.GetInstanceID()}_item_{index}_{layerTypeString}";
                    if (!foldoutStates.ContainsKey(foldoutKey))
                    {
                        foldoutStates[foldoutKey] = true;
                    }
                    
                    // Draw item in a box with foldout
                    EditorGUILayout.BeginVertical("box");
                    
                    // Header with label and delete button
                    EditorGUILayout.BeginHorizontal();
                    foldoutStates[foldoutKey] = EditorGUILayout.Foldout(
                        foldoutStates[foldoutKey], 
                        $"Layer {index + 1}: {layerTypeString}", 
                        true
                    );
                    
                    if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(20)))
                    {
                        if (EditorUtility.DisplayDialog("Delete Layer Reference", 
                            $"Are you sure you want to delete '{layerTypeString}'?", 
                            "Delete", "Cancel"))
                        {
                            itemToDelete = index;
                            itemToDeleteName = layerTypeString;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    if (foldoutStates[foldoutKey])
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(layerTypeProperty, new GUIContent("Layer Type"));
                        EditorGUILayout.PropertyField(item.FindPropertyRelative("layerBase"), new GUIContent("Layer Base"));
                        EditorGUI.indentLevel--;
                    }
                    
                    EditorGUILayout.EndVertical();
                }
            }
            
            // Handle deletion after drawing all items
            if (itemToDelete >= 0)
            {
                layerReferenceListProperty.DeleteArrayElementAtIndex(itemToDelete);
                serializedObject.ApplyModifiedProperties();
                return; // Exit to refresh the GUI
            }
            
            // Clear filter button if there's an active search
            if (!string.IsNullOrEmpty(searchFilter))
            {
                EditorGUILayout.Space();
                if (GUILayout.Button("Clear Search Filter"))
                {
                    searchFilter = "";
                }
            }
        }
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private List<int> GetFilteredIndices(SerializedProperty listProperty, string filter)
    {
        List<int> filteredIndices = new List<int>();
        
        for (int i = 0; i < listProperty.arraySize; i++)
        {
            SerializedProperty item = listProperty.GetArrayElementAtIndex(i);
            SerializedProperty layerTypeProperty = item.FindPropertyRelative("layerType");
            
            if (layerTypeProperty != null)
            {
                string layerTypeString = layerTypeProperty.enumDisplayNames[layerTypeProperty.enumValueIndex];
                
                if (string.IsNullOrEmpty(filter) || 
                    layerTypeString.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    filteredIndices.Add(i);
                }
            }
        }
        
        return filteredIndices;
    }
    
    private void DrawLayerReferenceItem(SerializedProperty listProperty, int index)
    {
        SerializedProperty item = listProperty.GetArrayElementAtIndex(index);
        SerializedProperty layerTypeProperty = item.FindPropertyRelative("layerType");
        SerializedProperty layerBaseProperty = item.FindPropertyRelative("layerBase");
        
        if (layerTypeProperty == null || layerBaseProperty == null) return;
        
        string layerTypeString = layerTypeProperty.enumDisplayNames[layerTypeProperty.enumValueIndex];
        string foldoutKey = $"{target.GetInstanceID()}_item_{index}_{layerTypeString}";
        
        if (!foldoutStates.ContainsKey(foldoutKey))
        {
            foldoutStates[foldoutKey] = true;
        }
        
        // Draw item in a box with foldout
        EditorGUILayout.BeginVertical("box");
        
        // Header with label and delete button
        EditorGUILayout.BeginHorizontal();
        foldoutStates[foldoutKey] = EditorGUILayout.Foldout(
            foldoutStates[foldoutKey], 
            $"Layer {index + 1}: {layerTypeString}", 
            true
        );
        
        if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(20)))
        {
            if (EditorUtility.DisplayDialog("Delete Layer Reference", 
                $"Are you sure you want to delete '{layerTypeString}'?", 
                "Delete", "Cancel"))
            {
                listProperty.DeleteArrayElementAtIndex(index);
            }
        }
        EditorGUILayout.EndHorizontal();
        
        if (foldoutStates[foldoutKey])
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(layerTypeProperty, new GUIContent("Layer Type"));
            EditorGUILayout.PropertyField(layerBaseProperty, new GUIContent("Layer Base"));
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void AddNewItem(SerializedProperty listProperty)
    {
        int newSize = listProperty.arraySize;
        listProperty.arraySize = newSize + 1;
        SerializedProperty newItem = listProperty.GetArrayElementAtIndex(newSize);
        newItem.FindPropertyRelative("layerBase").objectReferenceValue = null;
    }
}