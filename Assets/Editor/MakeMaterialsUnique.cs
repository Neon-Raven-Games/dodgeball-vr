using UnityEngine;
using UnityEditor;

public class MakeMaterialsUnique : MonoBehaviour
{
    [MenuItem("Neon Raven/Make Materials Unique")]
    public static void MakeMaterialsUniqueForSelected()
    {
        var selectedObjects = Selection.gameObjects;

        foreach (var obj in selectedObjects)
        {
            var renderer = obj.GetComponent<Renderer>();
            if (renderer == null || renderer.sharedMaterials.Length == 0)
            {
                Debug.LogWarning($"{obj.name} has no Renderer or no materials.");
                continue;
            }

            var materials = renderer.sharedMaterials;

            for (int i = 0; i < materials.Length; i++)
            {
                var mat = materials[i];
                if (mat.name.Contains("(Unique)"))
                {
                    Debug.LogWarning($"{mat.name} is already unique.");
                    continue;
                }
                var newMat = new Material(mat);
                newMat.name = mat.name + " (Unique)";
                materials[i] = newMat;
            }

            renderer.sharedMaterials = materials;
        }

        Debug.Log("Materials made unique for selected objects.");
    }
}