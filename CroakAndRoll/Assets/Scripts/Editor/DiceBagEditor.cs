using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Custom editor for DiceBag that displays the current dice collection in the inspector.
/// </summary>
[CustomEditor(typeof(DiceBag))]
public class DiceBagEditor : Editor
{
    private bool showDiceCollection = true;
    private bool showGroupedView = true;

    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();

        DiceBag diceBag = (DiceBag)target;

        // Only show dice collection in play mode
        if (Application.isPlaying)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Runtime Dice Collection", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This shows the current dice in the bag during play mode.", MessageType.Info);

            // Get the dice collection via reflection since it's private
            var diceCollectionField = typeof(DiceBag).GetField("diceCollection", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (diceCollectionField != null)
            {
                List<DieData> diceCollection = diceCollectionField.GetValue(diceBag) as List<DieData>;
                
                if (diceCollection != null)
                {
                    // Foldout for dice collection
                    showDiceCollection = EditorGUILayout.Foldout(showDiceCollection, 
                        $"Dice Collection ({diceCollection.Count} dice)", true);

                    if (showDiceCollection)
                    {
                        EditorGUI.indentLevel++;

                        // Toggle between grouped and individual view
                        showGroupedView = EditorGUILayout.Toggle("Show Grouped View", showGroupedView);

                        EditorGUILayout.Space(5);

                        if (diceCollection.Count == 0)
                        {
                            EditorGUILayout.HelpBox("No dice in the bag.", MessageType.Warning);
                        }
                        else
                        {
                            if (showGroupedView)
                            {
                                DrawGroupedDiceView(diceCollection);
                            }
                            else
                            {
                                DrawIndividualDiceView(diceCollection);
                            }
                        }

                        EditorGUI.indentLevel--;
                    }

                    // Display bag summary
                    EditorGUILayout.Space(5);
                    string summary = diceBag.GetBagSummary();
                    EditorGUILayout.LabelField("Bag Summary:", EditorStyles.miniLabel);
                    EditorGUILayout.SelectableLabel(summary, EditorStyles.helpBox, GUILayout.MinHeight(20));
                }
            }
        }
        else
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("Enter Play Mode to see the runtime dice collection.", MessageType.Info);
        }

        // Force repaint in play mode to keep the inspector updated
        if (Application.isPlaying)
        {
            Repaint();
        }
    }

    /// <summary>
    /// Draw dice grouped by type with counts.
    /// </summary>
    private void DrawGroupedDiceView(List<DieData> diceCollection)
    {
        var grouped = diceCollection
            .Where(d => d != null)
            .GroupBy(d => d)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key.dieName);

        foreach (var group in grouped)
        {
            DieData die = group.Key;
            int count = group.Count();

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            
            // Die icon/preview (using prefab preview if available)
            if (die.diePrefab != null)
            {
                Texture2D preview = AssetPreview.GetAssetPreview(die.diePrefab);
                if (preview != null)
                {
                    GUILayout.Label(preview, GUILayout.Width(40), GUILayout.Height(40));
                }
                else
                {
                    GUILayout.Label("🎲", GUILayout.Width(40), GUILayout.Height(40));
                }
            }
            else
            {
                GUILayout.Label("🎲", GUILayout.Width(40), GUILayout.Height(40));
            }

            // Die info
            EditorGUILayout.BeginVertical();
            
            EditorGUILayout.LabelField(die.dieName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Count: {count}", EditorStyles.miniLabel);
            
            if (die.faceValues != null && die.faceValues.Length > 0)
            {
                string faceValuesStr = string.Join(", ", die.faceValues);
                EditorGUILayout.LabelField($"Faces: {faceValuesStr}", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndVertical();

            // Show reference button
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                Selection.activeObject = die;
                EditorGUIUtility.PingObject(die);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2);
        }
    }

    /// <summary>
    /// Draw individual dice entries.
    /// </summary>
    private void DrawIndividualDiceView(List<DieData> diceCollection)
    {
        for (int i = 0; i < diceCollection.Count; i++)
        {
            DieData die = diceCollection[i];
            
            if (die == null)
            {
                EditorGUILayout.LabelField($"[{i}] NULL", EditorStyles.helpBox);
                continue;
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(35));
            
            // Die icon
            if (die.diePrefab != null)
            {
                Texture2D preview = AssetPreview.GetAssetPreview(die.diePrefab);
                if (preview != null)
                {
                    GUILayout.Label(preview, GUILayout.Width(30), GUILayout.Height(30));
                }
            }

            EditorGUILayout.ObjectField(die, typeof(DieData), false);

            EditorGUILayout.EndHorizontal();
        }
    }
}
