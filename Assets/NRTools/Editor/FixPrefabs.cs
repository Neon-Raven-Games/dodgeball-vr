using UnityEditor;
using UnityEngine;

public class FixPrefabs : Editor
{
    [MenuItem("Neon Raven/Delete Missing Scripts")]
    public static void FixAllPrefabs()
    {
        string[] allPrefabs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Scripts/Multiplayer/Prefabs/New" });

        foreach (string guid in allPrefabs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null)
                continue;

            bool needsSave = false;

            // Check for missing scripts
            Component[] components = prefab.GetComponentsInChildren<Component>(true);
            foreach (Component component in components)
            {
                if (component == null)
                {
                    GameObject go = component.gameObject;
                    SerializedObject so = new SerializedObject(go);
                    SerializedProperty prop = so.FindProperty("m_Component");

                    for (int i = 0; i < prop.arraySize; i++)
                    {
                        SerializedProperty componentProp = prop.GetArrayElementAtIndex(i);
                        if (componentProp.objectReferenceValue == null)
                        {
                            prop.DeleteArrayElementAtIndex(i);
                            needsSave = true;
                        }
                    }

                    so.ApplyModifiedProperties();
                }
            }

            // Save the prefab if any changes were made
            if (needsSave)
            {
                PrefabUtility.SavePrefabAsset(prefab);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}