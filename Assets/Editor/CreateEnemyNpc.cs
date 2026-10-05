using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CreateEnemyNpc
{
    [MenuItem("Tools/Gameplay/Create Enemy NPC")]
    public static void Create()
    {
        GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Undo.RegisterCreatedObjectUndo(enemy, "Create Enemy NPC");
        enemy.name = "Enemy NPC";

        Transform player = Object.FindAnyObjectByType<PlayerHealth>()?.transform;
        if (player != null)
        {
            Vector3 forward = player.forward;
            forward.y = 0f;
            enemy.transform.position = player.position + forward.normalized * 7f;
        }
        else
        {
            enemy.transform.position = Vector3.zero;
        }

        Renderer renderer = enemy.GetComponent<Renderer>();
        renderer.material.color = new Color(0.7f, 0.12f, 0.12f, 1f);

        Object.DestroyImmediate(enemy.GetComponent<CapsuleCollider>());
        enemy.AddComponent<CharacterController>();
        enemy.AddComponent<Damageable>();
        enemy.AddComponent<EnemyController>();
        enemy.AddComponent<EnemyHealthBar>();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = enemy;
        Debug.Log("Enemy NPC created. Replace the Capsule mesh with your own model whenever you are ready.");
    }
}
