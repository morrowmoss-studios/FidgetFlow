using UnityEngine;
using UnityEngine.EventSystems;

public class InputModuleSnitch : MonoBehaviour
{
    private void Start()
    {
        BaseInputModule[] modules =
            FindObjectsByType<BaseInputModule>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        Debug.Log($"[INPUT SNITCH] Found {modules.Length} input modules.");

        foreach (BaseInputModule module in modules)
        {
            Debug.Log(
                $"[INPUT SNITCH] " +
                $"Type={module.GetType().Name} | " +
                $"Object={module.gameObject.name} | " +
                $"Scene={module.gameObject.scene.name} | " +
                $"Active={module.gameObject.activeInHierarchy}",
                module.gameObject
            );
        }
    }
}