using UnityEngine;
using UnityEditor;

public class FindMissingScripts : EditorWindow
{
    [MenuItem("Tools/Find Missing Scripts in Scene")]
    static void ShowWindow()
    {
        GetWindow(typeof(FindMissingScripts));
    }

    void OnGUI()
    {
        if (GUILayout.Button("Find Missing Scripts in Active Scene"))
            FindMissingInScene();
    }

    static void FindMissingInScene()
    {
        var allGameObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude);
        int found = 0;
        foreach (var go in allGameObjects)
        {
            var components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    Debug.Log($"Missing script on GameObject: {GetFullPath(go)}", go);
                    found++;
                }
            }
        }
        Debug.Log($"FindMissingScripts: found {found} missing components in scene.");
    }

    static string GetFullPath(GameObject go)
    {
        string path = go.name;
        Transform t = go.transform;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
