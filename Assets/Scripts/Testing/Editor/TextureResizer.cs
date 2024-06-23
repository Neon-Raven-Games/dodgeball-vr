using UnityEngine;
using UnityEditor;

public class TextureResizer : EditorWindow
{
    private int maxSize = 512;

    [MenuItem("Tools/Resize Textures")]
    public static void ShowWindow()
    {
        GetWindow<TextureResizer>("Resize Textures");
    }

    void OnGUI()
    {
        GUILayout.Label("Resize Textures", EditorStyles.boldLabel);
        maxSize = EditorGUILayout.IntField("Max Size", maxSize);

        if (GUILayout.Button("Resize Selected Textures"))
        {
            ResizeSelectedTextures();
        }
    }

    void ResizeSelectedTextures()
    {
        foreach (Object obj in Selection.objects)
        {
            if (obj is Texture2D)
            {
                Texture2D originalTexture = obj as Texture2D;
                string path = AssetDatabase.GetAssetPath(originalTexture);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (importer != null)
                {
                    // Decompress the texture
                    importer.isReadable = true;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

                    // Resize the texture
                    Texture2D resizedTexture = ResizeTexture(originalTexture, maxSize, maxSize);

                    // Compress the texture back to its original format
                    Texture2D compressedTexture = CompressTexture(resizedTexture, importer, out var tempPath);

                    // Save the resized and compressed texture as a new asset
                    string newPath = path.Replace(".png", $"_resized_{maxSize}.png").Replace(".jpg", $"_resized_{maxSize}.jpg");
                    byte[] bytes = compressedTexture.EncodeToPNG();
                    Debug.Log(path);
                    System.IO.File.WriteAllBytes(newPath, bytes);
                    AssetDatabase.ImportAsset(newPath);

                    AssetDatabase.DeleteAsset(tempPath);
                    // Texture2D newTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(newPath);
                    // UpdateTextureReferences(originalTexture, newTexture);

                    // Cleanup
                    DestroyImmediate(resizedTexture);
                    DestroyImmediate(compressedTexture);

                    Debug.Log($"Resized {originalTexture.name} and created new texture at {newPath}");
                }
            }
        }
    }

    Texture2D ResizeTexture(Texture2D originalTexture, int width, int height)
    {
        RenderTexture rt = RenderTexture.GetTemporary(width, height, 24);
        Graphics.Blit(originalTexture, rt);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D resizedTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        resizedTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        resizedTexture.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        return resizedTexture;
    }

    Texture2D CompressTexture(Texture2D texture, TextureImporter importer, out string tempPath)
    {
        byte[] bytes = texture.EncodeToPNG();
        tempPath = "Assets/temp_texture.png";
        System.IO.File.WriteAllBytes(tempPath, bytes);
        AssetDatabase.ImportAsset(tempPath, ImportAssetOptions.ForceUpdate);

        // Re-import the texture with the original compression settings
        TextureImporter tempImporter = AssetImporter.GetAtPath(tempPath) as TextureImporter;
        tempImporter.textureCompression = importer.textureCompression;
        tempImporter.compressionQuality = importer.compressionQuality;
        tempImporter.isReadable = true;
        AssetDatabase.ImportAsset(tempPath, ImportAssetOptions.ForceUpdate);

        // Load the compressed texture
        Texture2D compressedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(tempPath);

        return compressedTexture;
    }

    void UpdateTextureReferences(Texture2D original, Texture2D optimized)
    {
        Renderer[] renderers = FindObjectsOfType<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            foreach (Material mat in renderer.sharedMaterials)
            {
                if (mat.mainTexture == original)
                {
                    mat.mainTexture = optimized;
                    EditorUtility.SetDirty(mat);
                }

                // Check for additional texture properties if needed
                // Example:
                // if (mat.GetTexture("_BumpMap") == original)
                // {
                //     mat.SetTexture("_BumpMap", optimized);
                //     EditorUtility.SetDirty(mat);
                // }
            }
        }
    }
}
