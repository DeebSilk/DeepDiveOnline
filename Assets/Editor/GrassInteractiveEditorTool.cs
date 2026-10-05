using System;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class GrassInteractiveEditorTool : EditorWindow
{
    private const string MaterialPath =
        "Assets/Grass/InteractiveGrass_Mat.mat";

    private const string GraphPath =
        "Assets/Grass/Grass_Interactive.shadergraph";

    private const string ExpectedShaderGuid =
        "c711efc3ba2359d48bd6fb416e42b749";

    private const string GrassTerrainName = "Terrain01";

    private const string GrassPrefabPath =
        "Assets/Grass/Prefabs/grass1.prefab";

    private const string OriginalGrassMaterialPath =
        "Assets/Grass/Materials/MAT_Grass1.mat";

    private static readonly string[] ExpectedProperties =
    {
        "_Player_Position",
        "_Interaction_Radius",
        "_Interaction_Strength",
        "_Interaction_Falloff",
        "_Wind_Strength",
        "_Wind_Speed",
        "_Wind_Phase_Scale"
    };

    [MenuItem("Tools/Grass/Interactive Grass Diagnostics")]
    public static void Open()
    {
        GetWindow<GrassInteractiveEditorTool>(
            "Interactive Grass Diagnostics"
        );
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Diagnostics only. No assets, TerrainData, materials, or shader graphs are modified.",
            MessageType.Info
        );

        if (GUILayout.Button("Inspect Terrain Details"))
            InspectTerrainDetails();

        if (GUILayout.Button("Inspect Interactive Material"))
            InspectInteractiveMaterial();

        if (GUILayout.Button("Open Shader Graph"))
            OpenShaderGraph();

        if (GUILayout.Button("Select Interactive Material"))
            SelectInteractiveMaterial();

        if (GUILayout.Button("Print Global Shader Properties"))
            PrintGlobalShaderProperties();

        EditorGUILayout.Space(8f);

        if (GUILayout.Button("Run All Diagnostics"))
            RunAllDiagnostics();

        EditorGUILayout.Space(8f);

        if (GUILayout.Button("Assign Interactive Material To Grass1"))
            AssignInteractiveMaterialToGrass1();

        if (GUILayout.Button("Inspect Grass1 Visual Properties"))
            InspectGrass1VisualProperties();

        if (GUILayout.Button("Copy Grass1 Visual Properties"))
            CopyGrass1VisualProperties();
    }

    private static void InspectTerrainDetails()
    {
        Terrain[] terrains =
            FindObjectsByType<Terrain>(
                FindObjectsInactive.Include
            );

        if (terrains.Length == 0)
        {
            Debug.Log(
                "[Grass Diagnostics] No Terrain found in the open scene."
            );

            return;
        }

        foreach (Terrain terrain in terrains)
        {
            TerrainData terrainData =
                terrain != null
                    ? terrain.terrainData
                    : null;

            Debug.Log(
                $"[Grass Diagnostics] Terrain='{terrain.name}', " +
                $"TerrainData='{GetAssetPath(terrainData)}', " +
                $"DetailPrototypes={terrainData?.detailPrototypes?.Length ?? 0}",
                terrain
            );

            if (terrainData == null)
                continue;

            DetailPrototype[] prototypes =
                terrainData.detailPrototypes;

            for (int index = 0; index < prototypes.Length; index++)
            {
                LogDetailPrototype(
                    index,
                    prototypes[index],
                    terrain
                );
            }
        }
    }

    private static void LogDetailPrototype(
        int index,
        DetailPrototype prototype,
        Terrain terrain)
    {
        GameObject prefab = prototype.prototype;
        Texture2D texture = prototype.prototypeTexture;
        Renderer renderer = prefab != null
            ? prefab.GetComponentInChildren<Renderer>(true)
            : null;
        Material material = renderer != null
            ? renderer.sharedMaterial
            : null;
        Shader shader = material != null
            ? material.shader
            : null;

        bool grass1Detected =
            string.Equals(
                prefab != null ? prefab.name : string.Empty,
                "grass1",
                StringComparison.OrdinalIgnoreCase
            ) ||
            string.Equals(
                texture != null ? texture.name : string.Empty,
                "grass1",
                StringComparison.OrdinalIgnoreCase
            ) ||
            GetAssetPath(prefab).IndexOf(
                "/grass1",
                StringComparison.OrdinalIgnoreCase
            ) >= 0;

        StringBuilder report = new StringBuilder();
        report.AppendLine("[Grass Diagnostics] Detail Prototype");
        report.AppendLine($"  Terrain: {terrain.name}");
        report.AppendLine($"  Index: {index}");
        report.AppendLine($"  grass1 detected: {grass1Detected}");
        report.AppendLine($"  prototypeTexture: {GetObjectName(texture)}");
        report.AppendLine($"  prototype: {GetObjectName(prefab)}");
        report.AppendLine($"  prototype path: {GetAssetPath(prefab)}");
        report.AppendLine($"  renderMode: {prototype.renderMode}");
        report.AppendLine($"  dryColor: {prototype.dryColor}");
        report.AppendLine($"  healthyColor: {prototype.healthyColor}");
        report.AppendLine($"  minWidth: {prototype.minWidth}");
        report.AppendLine($"  maxWidth: {prototype.maxWidth}");
        report.AppendLine($"  minHeight: {prototype.minHeight}");
        report.AppendLine($"  maxHeight: {prototype.maxHeight}");
        report.AppendLine($"  noiseSpread: {prototype.noiseSpread}");
        report.AppendLine($"  material: {GetObjectName(material)}");
        report.AppendLine($"  material path: {GetAssetPath(material)}");
        report.AppendLine($"  shader: {GetObjectName(shader)}");
        report.AppendLine(
            "  DetailPrototype has no direct material field in this Unity API; " +
            "material above is resolved from prototype Renderer when available."
        );

        Debug.Log(report.ToString(), terrain);
    }

    private static void InspectInteractiveMaterial()
    {
        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);

        if (material == null)
        {
            Debug.LogError(
                $"[Grass Diagnostics] Material not found: {MaterialPath}"
            );

            return;
        }

        Shader shader = material.shader;
        string shaderPath =
            shader != null
                ? AssetDatabase.GetAssetPath(shader)
                : string.Empty;
        string shaderGuid =
            string.IsNullOrEmpty(shaderPath)
                ? string.Empty
                : AssetDatabase.AssetPathToGUID(shaderPath);

        Debug.Log(
            $"[Grass Diagnostics] Interactive Material\n" +
            $"  path: {MaterialPath}\n" +
            $"  shader: {shader?.name ?? "<none>"}\n" +
            $"  shader path: {shaderPath}\n" +
            $"  shader GUID: {shaderGuid}\n" +
            $"  expected GUID: {ExpectedShaderGuid}\n" +
            $"  GUID matches: {shaderGuid == ExpectedShaderGuid}",
            material
        );

        if (shader == null)
            return;

        for (int index = 0; index < shader.GetPropertyCount(); index++)
        {
            Debug.Log(
                $"[Grass Diagnostics] Material shader property: " +
                $"{shader.GetPropertyName(index)} " +
                $"({shader.GetPropertyType(index)})",
                material
            );
        }
    }

    private static void OpenShaderGraph()
    {
        UnityEngine.Object graph =
            AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(GraphPath);

        if (graph == null)
        {
            Debug.LogError(
                $"[Grass Diagnostics] Shader Graph not found: {GraphPath}"
            );

            return;
        }

        Selection.activeObject = graph;
        AssetDatabase.OpenAsset(graph);
        Debug.Log(
            $"[Grass Diagnostics] Shader Graph opened: {GraphPath}",
            graph
        );
    }

    private static void SelectInteractiveMaterial()
    {
        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);

        if (material == null)
        {
            Debug.LogError(
                $"[Grass Diagnostics] Material not found: {MaterialPath}"
            );

            return;
        }

        Selection.activeObject = material;
        EditorGUIUtility.PingObject(material);
    }

    private static void PrintGlobalShaderProperties()
    {
        Debug.Log(
            "[Grass Diagnostics] Global Shader Properties\n" +
            BuildShaderPropertyReport()
        );
    }

    private static void RunAllDiagnostics()
    {
        InspectTerrainDetails();
        InspectInteractiveMaterial();
        PrintGlobalShaderProperties();
        InspectGrassInteraction();

        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        Shader shader = material != null ? material.shader : null;
        string graphGuid =
            AssetDatabase.AssetPathToGUID(GraphPath);

        Debug.Log(
            "---\n" +
            "## GRASS SYSTEM DIAGNOSTICS\n" +
            $"Unity: {Application.unityVersion}\n" +
            "URP: inspect Package Manager / project package configuration\n" +
            "Terrain: see Terrain Details diagnostics above\n" +
            "TerrainData: see Terrain Details diagnostics above\n" +
            "Grass Detail: see Detail Prototype diagnostics above\n" +
            "Interactive Material:\n" +
            $"path: {MaterialPath}\n" +
            $"shader: {shader?.name ?? "<none>"}\n" +
            "Shader Graph:\n" +
            $"path: {GraphPath}\n" +
            $"GUID: {graphGuid}\n" +
            BuildShaderPropertyReport() +
            "---"
        );
    }

    private static void AssignInteractiveMaterialToGrass1()
    {
        Terrain terrain =
            FindTerrainByName(GrassTerrainName);

        if (terrain == null || terrain.terrainData == null)
        {
            Debug.LogError(
                $"[Grass Diagnostics] Terrain '{GrassTerrainName}' " +
                "or its TerrainData was not found. No changes made."
            );

            return;
        }

        TerrainData terrainData = terrain.terrainData;
        DetailPrototype[] prototypes = terrainData.detailPrototypes;

        if (prototypes == null || prototypes.Length == 0)
        {
            Debug.LogError(
                "[Grass Diagnostics] Terrain has no DetailPrototypes. " +
                "No changes made.",
                terrain
            );

            return;
        }

        DetailPrototype prototype = prototypes[0];
        string prototypePath = GetAssetPath(prototype.prototype);

        if (!string.Equals(
                prototypePath,
                GrassPrefabPath,
                StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogError(
                "[Grass Diagnostics] Detail Prototype 0 is not grass1. " +
                $"Expected '{GrassPrefabPath}', got '{prototypePath}'. " +
                "No changes made.",
                terrain
            );

            return;
        }

        Material interactiveMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        Material originalMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(
                OriginalGrassMaterialPath
            );
        GameObject prefabContents =
            PrefabUtility.LoadPrefabContents(GrassPrefabPath);

        if (interactiveMaterial == null ||
            originalMaterial == null ||
            prefabContents == null)
        {
            if (prefabContents != null)
                PrefabUtility.UnloadPrefabContents(prefabContents);

            Debug.LogError(
                "[Grass Diagnostics] Required grass1 material or prefab " +
                "asset was not found. No changes made."
            );

            return;
        }

        try
        {
            Renderer renderer =
                prefabContents.GetComponentInChildren<Renderer>(true);

            if (renderer == null)
            {
                Debug.LogError(
                    "[Grass Diagnostics] grass1 has no Renderer. " +
                    "No changes made."
                );

                return;
            }

            Material currentMaterial = renderer.sharedMaterial;
            string currentMaterialPath =
                GetAssetPath(currentMaterial);

            Debug.Log(
                "[Grass Diagnostics] Terrain Detail material is supplied " +
                "by the grass1 prefab Renderer; DetailPrototype has no " +
                "direct material setter in this Unity API.\n" +
                $"  Current prefab material: {currentMaterialPath}\n" +
                $"  Requested material: {MaterialPath}\n" +
                "  No automatic assignment performed because the requested " +
                "material lacks the original grass texture and alpha-clip " +
                "property mapping has not been verified.",
                terrain
            );

            Debug.LogWarning(
                "[Grass Diagnostics] Assignment cancelled safely. " +
                "Use the Shader Graph/material editor to preserve grass1 " +
                "texture, alpha clipping, lighting, and shadows before " +
                "changing the prefab Renderer material.",
                interactiveMaterial
            );
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabContents);
        }
    }

    private static void InspectGrass1VisualProperties()
    {
        Material source =
            AssetDatabase.LoadAssetAtPath<Material>(
                OriginalGrassMaterialPath
            );
        Material destination =
            AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);

        if (source == null || destination == null)
        {
            Debug.LogError(
                "[Grass Diagnostics] Grass1 visual property inspection " +
                "could not load one of the materials."
            );

            return;
        }

        Shader sourceShader = source.shader;
        Shader destinationShader = destination.shader;
        StringBuilder report = new StringBuilder();

        report.AppendLine("[Grass Diagnostics] Grass1 Visual Properties");
        report.AppendLine($"  Source material: {OriginalGrassMaterialPath}");
        report.AppendLine($"  Source shader: {sourceShader?.name ?? "<none>"}");
        report.AppendLine($"  Source MainTex: {GetTextureReport(source, "_MainTex")}");
        report.AppendLine($"  Source Color: {GetColorReport(source, "_Color")}");
        report.AppendLine($"  Source Cutoff: {GetFloatReport(source, "_Cutoff")}");
        report.AppendLine($"  Source RenderQueue: {source.renderQueue}");
        report.AppendLine($"  Source MaterialKeywords: {string.Join(", ", source.shaderKeywords)}");
        report.AppendLine($"  Destination material: {MaterialPath}");
        report.AppendLine($"  Destination shader: {destinationShader?.name ?? "<none>"}");
        report.AppendLine($"  Destination MainTex: {GetTextureReport(destination, "_MainTex")}");
        report.AppendLine($"  Destination BaseColor: {GetColorReport(destination, "_BaseColor")}");
        report.AppendLine($"  Destination Color: {GetColorReport(destination, "_Color")}");
        report.AppendLine($"  Destination Cutoff: {GetFloatReport(destination, "_Cutoff")}");
        report.AppendLine($"  Destination RenderQueue: {destination.renderQueue}");
        report.AppendLine($"  Destination AlphaClip field: {GetFloatReport(destination, "_AlphaClip")}");
        report.AppendLine(
            "  Legacy shader behavior: Sample _MainTex, clip alpha against " +
            "_Cutoff, multiply sampled color by _Color, Cull Off, ZWrite On."
        );

        Debug.Log(report.ToString(), source);
    }

    private static void CopyGrass1VisualProperties()
    {
        Material source =
            AssetDatabase.LoadAssetAtPath<Material>(
                OriginalGrassMaterialPath
            );
        Material destination =
            AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);

        if (source == null || destination == null || destination.shader == null)
        {
            Debug.LogError(
                "[Grass Diagnostics] Cannot copy visual properties because " +
                "one of the materials or its shader is missing."
            );

            return;
        }

        bool hasMainTex = destination.HasProperty("_MainTex");
        bool hasColor = destination.HasProperty("_BaseColor") ||
                        destination.HasProperty("_Color");
        bool hasCutoff = destination.HasProperty("_Cutoff");

        if (!hasMainTex || !hasColor || !hasCutoff)
        {
            Debug.LogWarning(
                "[Grass Diagnostics] Copy cancelled safely. " +
                $"Interactive shader properties: _MainTex={hasMainTex}, " +
                $"Color={hasColor}, _Cutoff={hasCutoff}. " +
                "Add the missing Blackboard properties and graph connections " +
                "first; no material values were changed.",
                destination
            );

            return;
        }

        Undo.RecordObject(
            destination,
            "Copy Grass1 Visual Properties"
        );

        Texture mainTexture =
            source.GetTexture("_MainTex");
        destination.SetTexture(
            "_MainTex",
            mainTexture
        );

        Color sourceColor =
            source.GetColor("_Color");
        string destinationColorProperty =
            destination.HasProperty("_BaseColor")
                ? "_BaseColor"
                : "_Color";
        destination.SetColor(
            destinationColorProperty,
            sourceColor
        );

        destination.SetFloat(
            "_Cutoff",
            source.GetFloat("_Cutoff")
        );

        EditorUtility.SetDirty(destination);
        AssetDatabase.SaveAssets();

        Debug.Log(
            "[Grass Diagnostics] Copied _MainTex, color, and _Cutoff " +
            "to InteractiveGrass_Mat. Terrain and grass1 prefab were not changed.",
            destination
        );
    }

    private static Terrain FindTerrainByName(string terrainName)
    {
        Terrain[] terrains =
            FindObjectsByType<Terrain>(
                FindObjectsInactive.Include
            );

        foreach (Terrain terrain in terrains)
        {
            if (terrain != null && terrain.name == terrainName)
                return terrain;
        }

        return null;
    }

    private static string BuildShaderPropertyReport()
    {
        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        Shader shader = material != null ? material.shader : null;
        StringBuilder report = new StringBuilder();

        foreach (string property in ExpectedProperties)
        {
            bool exists =
                shader != null &&
                HasShaderProperty(shader, property);
            int propertyId = Shader.PropertyToID(property);
            Vector4 globalValue = Shader.GetGlobalVector(propertyId);

            report.AppendLine(
                $"{property}: {exists} " +
                $"(id={propertyId}, global={globalValue})"
            );
        }

        return report.ToString();
    }

    private static void InspectGrassInteraction()
    {
        GrassInteraction[] interactions =
            FindObjectsByType<GrassInteraction>(
                FindObjectsInactive.Include
            );

        if (interactions.Length == 0)
        {
            Debug.Log(
                "[Grass Diagnostics] GrassInteraction not found in the open scene."
            );

            return;
        }

        foreach (GrassInteraction interaction in interactions)
        {
            SerializedObject serializedInteraction =
                new SerializedObject(interaction);
            SerializedProperty playerProperty =
                serializedInteraction.FindProperty("player");
            bool playerAssigned =
                playerProperty != null &&
                playerProperty.objectReferenceValue != null;

            Debug.Log(
                $"[Grass Diagnostics] GrassInteraction: " +
                $"object={interaction.name}, " +
                $"active={interaction.isActiveAndEnabled}, " +
                $"Player assigned: {playerAssigned}",
                interaction
            );
        }
    }

    private static bool HasShaderProperty(
        Shader shader,
        string propertyName)
    {
        for (int index = 0; index < shader.GetPropertyCount(); index++)
        {
            if (shader.GetPropertyName(index) == propertyName)
                return true;
        }

        return false;
    }

    private static string GetAssetPath(UnityEngine.Object target)
    {
        return target == null
            ? "<none>"
            : AssetDatabase.GetAssetPath(target);
    }

    private static string GetObjectName(UnityEngine.Object target)
    {
        return target == null
            ? "<none>"
            : target.name;
    }

    private static string GetTextureReport(
        Material material,
        string propertyName)
    {
        if (!material.HasProperty(propertyName))
            return "<property missing>";

        Texture texture = material.GetTexture(propertyName);
        return texture == null
            ? "<none>"
            : $"{texture.name} ({AssetDatabase.GetAssetPath(texture)})";
    }

    private static string GetColorReport(
        Material material,
        string propertyName)
    {
        return material.HasProperty(propertyName)
            ? material.GetColor(propertyName).ToString()
            : "<property missing>";
    }

    private static string GetFloatReport(
        Material material,
        string propertyName)
    {
        return material.HasProperty(propertyName)
            ? material.GetFloat(propertyName).ToString("0.###")
            : "<property missing>";
    }
}