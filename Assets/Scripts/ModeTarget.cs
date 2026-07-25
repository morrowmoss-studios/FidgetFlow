using UnityEngine;

public class ModeTarget : MonoBehaviour
{
    [Header("Stable Mode Identity")]
    [Tooltip("Must exactly match the portal's Mode ID.")]
    [SerializeField] private string modeId;

    public string ModeId => modeId;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(modeId))
        {
            modeId = gameObject.name
                .Replace("Quad_", string.Empty)
                .Trim();
        }
    }
#endif
}