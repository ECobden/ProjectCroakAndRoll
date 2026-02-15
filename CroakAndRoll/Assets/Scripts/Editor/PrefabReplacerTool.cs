using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class PrefabReplacerTool : EditorWindow
{
    private GameObject prefabToInstantiate;
    private bool preserveScale = true;
    private bool preserveName = false;
    private Vector2 scrollPosition;

    [MenuItem("Tools/Prefab Replacer")]
    public static void ShowWindow()
    {
        GetWindow<PrefabReplacerTool>("Prefab Replacer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Replace Selected Objects with Prefab", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Prefab field
        EditorGUILayout.BeginVertical("box");
        prefabToInstantiate = (GameObject)EditorGUILayout.ObjectField(
            "Replacement Prefab",
            prefabToInstantiate,
            typeof(GameObject),
            false
        );
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // Options
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Options", EditorStyles.boldLabel);
        preserveScale = EditorGUILayout.Toggle("Preserve Original Scale", preserveScale);
        preserveName = EditorGUILayout.Toggle("Preserve Original Name", preserveName);
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // Selection info
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Selection Info", EditorStyles.boldLabel);
        GameObject[] selectedObjects = Selection.gameObjects;
        EditorGUILayout.LabelField("Selected Objects:", selectedObjects.Length.ToString());
        
        if (selectedObjects.Length > 0)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));
            foreach (GameObject obj in selectedObjects)
            {
                EditorGUILayout.LabelField("  • " + obj.name);
            }
            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // Replace button
        GUI.enabled = prefabToInstantiate != null && selectedObjects.Length > 0;
        
        if (GUILayout.Button("Replace Selected Objects", GUILayout.Height(40)))
        {
            ReplaceSelectedObjects();
        }
        
        GUI.enabled = true;

        // Help text
        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "How to use:\n" +
            "1. Select one or more GameObjects in the Scene or Hierarchy\n" +
            "2. Assign a prefab in the 'Replacement Prefab' field\n" +
            "3. Configure options if needed\n" +
            "4. Click 'Replace Selected Objects'\n\n" +
            "The new objects will maintain the position and rotation of the originals.",
            MessageType.Info
        );
    }

    private void ReplaceSelectedObjects()
    {
        if (prefabToInstantiate == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign a prefab to instantiate.", "OK");
            return;
        }

        GameObject[] selectedObjects = Selection.gameObjects;
        
        if (selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "Please select at least one GameObject to replace.", "OK");
            return;
        }

        // Confirm the operation
        if (!EditorUtility.DisplayDialog(
            "Confirm Replacement",
            $"Are you sure you want to replace {selectedObjects.Length} object(s) with '{prefabToInstantiate.name}'?\n\n" +
            "This action can be undone with Ctrl+Z.",
            "Replace",
            "Cancel"))
        {
            return;
        }

        List<GameObject> newObjects = new List<GameObject>();
        int replacedCount = 0;

        // Record undo for all operations
        Undo.SetCurrentGroupName("Replace GameObjects with Prefab");
        int undoGroup = Undo.GetCurrentGroup();

        foreach (GameObject selectedObject in selectedObjects)
        {
            // Store original properties
            Vector3 position = selectedObject.transform.position;
            Quaternion rotation = selectedObject.transform.rotation;
            Vector3 scale = selectedObject.transform.localScale;
            Transform parent = selectedObject.transform.parent;
            int siblingIndex = selectedObject.transform.GetSiblingIndex();
            string originalName = selectedObject.name;

            // Instantiate the prefab
            GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefabToInstantiate);
            
            if (newObject != null)
            {
                // Register the new object for undo
                Undo.RegisterCreatedObjectUndo(newObject, "Create Replacement Object");

                // Set transform properties
                newObject.transform.position = position;
                newObject.transform.rotation = rotation;
                
                if (preserveScale)
                {
                    newObject.transform.localScale = scale;
                }

                // Set parent and sibling index to maintain hierarchy position
                if (parent != null)
                {
                    Undo.SetTransformParent(newObject.transform, parent, "Set Parent");
                }
                newObject.transform.SetSiblingIndex(siblingIndex);

                // Optionally preserve the name
                if (preserveName)
                {
                    newObject.name = originalName;
                }

                newObjects.Add(newObject);
                replacedCount++;

                // Destroy the original object
                Undo.DestroyObjectImmediate(selectedObject);
            }
        }

        // Collapse all undo operations into one
        Undo.CollapseUndoOperations(undoGroup);

        // Select the new objects
        Selection.objects = newObjects.ToArray();

        // Show success message
        Debug.Log($"Successfully replaced {replacedCount} object(s) with '{prefabToInstantiate.name}'");
        EditorUtility.DisplayDialog(
            "Success",
            $"Successfully replaced {replacedCount} object(s) with '{prefabToInstantiate.name}'",
            "OK"
        );
    }

    private void OnSelectionChange()
    {
        // Repaint the window when selection changes to update the count
        Repaint();
    }
}
