using UnityEditor;
using UnityEngine;

public static class RemoveMissingScripts
{
    [MenuItem("Tools/Remove Missing Scripts From Selected")]
    static void Remove()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);

            Debug.Log("Removed " + removed + " missing script(s) from " + go.name);
        }
    }
}
