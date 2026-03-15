using UnityEngine;

public class FindMissingScripts : MonoBehaviour
{
    void Start()
    {
        // Check all scene objects
        foreach (var go in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            var components = go.GetComponents<Component>();
            foreach (var c in components)
            {
                if (c == null)
                    Debug.LogError($"Missing script found on: {GetFullPath(go)}", go);
            }
        }
        Debug.Log("Missing script scan complete.");
    }

    string GetFullPath(GameObject go)
    {
        string path = go.name;
        Transform t = go.transform.parent;
        while (t != null)
        {
            path = t.name + "/" + path;
            t = t.parent;
        }
        return path;
    }
}