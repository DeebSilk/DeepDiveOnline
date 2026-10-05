using System;
using UnityEditor;
using UnityEngine;

public static class InteractiveGrassBuilder
{
    private const string GraphPath =
        "Assets/Grass/Grass_Interactive.shadergraph";

    private const string MaterialPath =
        "Assets/Grass/InteractiveGrass_Mat.mat";

    [MenuItem("Tools/Grass/Build Interactive Grass")]
    public static void BuildInteractiveGrass()
    {
        Debug.Log(
            "[Interactive Grass Builder] Build started. " +
            "No assets were modified."
        );

        if (!AssetDatabase.AssetPathExists(GraphPath))
        {
            Debug.LogError(
                $"[Interactive Grass Builder] Shader Graph not found: {GraphPath}"
            );

            return;
        }

        if (!AssetDatabase.AssetPathExists(MaterialPath))
        {
            Debug.LogError(
                $"[Interactive Grass Builder] Material not found: {MaterialPath}"
            );

            return;
        }

        Type graphDataType = FindType(
            "UnityEditor.ShaderGraph.GraphData"
        );
        Type texturePropertyType = FindType(
            "UnityEditor.ShaderGraph.Internal.Texture2DShaderProperty"
        );

        bool graphDataAvailable =
            graphDataType != null;
        bool graphDataPublic =
            graphDataAvailable && graphDataType.IsPublic;
        bool texturePropertyAvailable =
            texturePropertyType != null;
        bool texturePropertyPublic =
            texturePropertyAvailable && texturePropertyType.IsPublic;

        Debug.Log(
            "[Interactive Grass Builder] Shader Graph API inspection\n" +
            $"  GraphData found: {graphDataAvailable}\n" +
            $"  GraphData public: {graphDataPublic}\n" +
            $"  Texture2DShaderProperty found: {texturePropertyAvailable}\n" +
            $"  Texture2DShaderProperty public: {texturePropertyPublic}\n" +
            "  GraphData.AddGraphInput/AddNode/Connect are internal-package " +
            "APIs and are not callable from this project Editor assembly."
        );

        Debug.LogError(
            "[Interactive Grass Builder] Build stopped safely. " +
            "Unity Shader Graph 17.6.0 does not expose a supported public " +
            "API for loading, mutating, and saving an existing Shader Graph " +
            "from this project Editor assembly. The installed source shows " +
            "GraphData as internal and Texture2DShaderProperty's constructor " +
            "as internal. No raw JSON/YAML mutation was attempted, and no " +
            "Terrain, material, prefab, or Player asset was changed."
        );
    }

    private static Type FindType(string fullName)
    {
        foreach (System.Reflection.Assembly assembly in
                 AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(fullName, false);

            if (type != null)
                return type;
        }

        return null;
    }
}