using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TerrainTreeFixer : MonoBehaviour
{
    #if UNITY_EDITOR
    [MenuItem("Tools/Fix Terrain Tree Prototypes")]
    public static void FixTerrainTrees()
    {
        Terrain[] allTerrains = Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include);
        
        foreach (Terrain terrain in allTerrains)
        {
            if (terrain.terrainData == null) continue;
            
            TreePrototype[] treePrototypes = terrain.terrainData.treePrototypes;
            System.Collections.Generic.List<TreePrototype> validPrototypes = 
                new System.Collections.Generic.List<TreePrototype>();
            
            for (int i = 0; i < treePrototypes.Length; i++)
            {
                TreePrototype proto = treePrototypes[i];
                
                if (proto.prefab != null && proto.prefab.GetComponent<MeshRenderer>() != null)
                {
                    validPrototypes.Add(proto);
                }
                else
                {
                    Debug.LogWarning($"[TerrainTreeFixer] Removing invalid tree prototype: {(proto.prefab ? proto.prefab.name : "Missing Prefab")}", terrain);
                }
            }
            
            if (validPrototypes.Count != treePrototypes.Length)
            {
                terrain.terrainData.treePrototypes = validPrototypes.ToArray();
                terrain.terrainData.RefreshPrototypes();
                EditorUtility.SetDirty(terrain.terrainData);
                Debug.Log($"[TerrainTreeFixer] Fixed {terrain.name}: removed {treePrototypes.Length - validPrototypes.Count} invalid prototypes");
            }
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log("[TerrainTreeFixer] Terrain tree prototypes fixed");
    }

    [MenuItem("Tools/Cleanup Missing Scripts")]
    public static void CleanupMissingScripts()
    {
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        int totalRemoved = 0;

        foreach (GameObject go in allObjects)
        {
            int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            if (count > 0)
            {
                Undo.RegisterCompleteObjectUndo(go, "Remove missing scripts");
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                totalRemoved += count;
                Debug.Log($"[Cleanup] Removed {count} missing scripts from '{go.name}'", go);
            }
        }

        if (totalRemoved > 0)
            Debug.Log($"[Cleanup] Done! Removed {totalRemoved} missing scripts in total.");
        else
            Debug.Log("[Cleanup] No missing scripts found.");
    }
    #endif
}
