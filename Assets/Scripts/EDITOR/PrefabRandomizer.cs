using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class PrefabRandomizer : EditorWindow
{
    [SerializeField]
    private List<GameObject> prefabsToSpawn = new();

    private bool keepOriginalRotation = true;
    private bool keepOriginalScale = true;

    [MenuItem("Tools/Prefab Randomizer")]
    public static void ShowWindow()
    {
        GetWindow<PrefabRandomizer>("Prefab Randomizer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Replace Selected Objects", EditorStyles.boldLabel);

        SerializedObject so = new SerializedObject(this);
        SerializedProperty prefabProp = so.FindProperty("prefabsToSpawn");
        EditorGUILayout.PropertyField(prefabProp, true);
        so.ApplyModifiedProperties();

        keepOriginalRotation =
            EditorGUILayout.Toggle("Keep Original Rotation", keepOriginalRotation);
        keepOriginalScale =
            EditorGUILayout.Toggle("Keep Original Scale", keepOriginalScale);

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(Selection.gameObjects.Length == 0))
        {
            if (GUILayout.Button("Replace Selected Objects"))
            {
                ReplaceSelected();
            }
        }
    }

    private void ReplaceSelected()
    {
        if (prefabsToSpawn.Count == 0)
        {
            Debug.LogWarning("Add at least one grave prefab.");
            return;
        }

        GameObject[] selection = Selection.gameObjects;

        foreach (GameObject marker in selection)
        {
            if (marker == null) continue;

            GameObject prefab =
                prefabsToSpawn[Random.Range(0, prefabsToSpawn.Count)];

            if (prefab == null)
            {
                Debug.LogWarning("Prefab list contains a null entry.");
                continue;
            }

            Transform parent = marker.transform.parent;
            int siblingIndex = marker.transform.GetSiblingIndex();

            GameObject instance;

            // SAFE prefab instantiation
            if (PrefabUtility.IsPartOfPrefabAsset(prefab))
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            }
            else
            {
                instance = Instantiate(prefab);
            }

            if (instance == null)
            {
                Debug.LogError($"Failed to instantiate prefab: {prefab.name}");
                continue;
            }

            Undo.RegisterCreatedObjectUndo(instance, "Replace With Random Prefab");

            instance.transform.SetParent(parent);
            instance.transform.SetSiblingIndex(siblingIndex);
            instance.transform.position = marker.transform.position;

            instance.transform.rotation = keepOriginalRotation
                ? marker.transform.rotation
                : prefab.transform.rotation;

            instance.transform.localScale = keepOriginalScale
                ? marker.transform.localScale
                : prefab.transform.localScale;

            instance.name = prefab.name;

            Undo.DestroyObjectImmediate(marker);
        }
    }
}
