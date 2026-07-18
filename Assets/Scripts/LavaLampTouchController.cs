using UnityEngine;
using UnityEngine.InputSystem;

public class LavaLampTouchController : MonoBehaviour
{
    public Material lavaLampMaterial;

    [Header("Explosion Settings")]
    public float explosionDuration = 1.5f;
    public float scatterDuration = 3.0f;
    public float reformDuration = 2.0f;

    const int MAX_BLOBS = 15;

    enum BlobState { Normal, Exploding, Scattered, Reforming }
    BlobState[] _blobStates = new BlobState[MAX_BLOBS];
    float[] _blobTimers = new float[MAX_BLOBS];
    float[] _blobProgress = new float[MAX_BLOBS];

    int _blobCount = 8;
    float _flowSpeed = 0.15f;

    float Hash(float n)
    {
        return Mathf.Abs(Mathf.Sin(n) * 43758.5453f % 1f);
    }

    Vector3 BlobPosition(int index, float t)
    {
        float fi = (float)index;
        float speed = (Hash(fi * 3.7f) - 0.5f) * 2.0f * _flowSpeed;
        return new Vector3(
            Mathf.Sin(t * speed       + fi * 52.5f) * 1.8f,
            Mathf.Cos(t * speed * 0.8f + fi * 23.1f) * 1.8f,
            Mathf.Sin(t * speed * 0.6f + fi * 17.9f) * 1.8f
        );
    }

    void Update()
    {
        _blobCount = Mathf.RoundToInt(lavaLampMaterial.GetFloat("_BlobCount"));
        _flowSpeed = lavaLampMaterial.GetFloat("_FlowSpeed");

        HandleTouch();
        UpdateAllBlobStates();
        PushStatesToShader();
    }

    void HandleTouch()
    {
        var activeTouches = new System.Collections.Generic.List<Vector2>();

#if UNITY_EDITOR
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            activeTouches.Add(Mouse.current.position.ReadValue());
#else
        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
                    activeTouches.Add(touch.position.ReadValue());
            }
        }
#endif

        float aspect = lavaLampMaterial.GetFloat("_AspectRatio");
        float fov = lavaLampMaterial.GetFloat("_FOV");
        float cameraZ = lavaLampMaterial.GetFloat("_CameraZ");
        float t = Time.time;

        foreach (Vector2 touchPos in activeTouches)
        {
            float uvX = touchPos.x / Screen.width;
            float uvY = touchPos.y / Screen.height;

            float worldX = (uvX * 2f - 1f) * aspect;
            float worldY = uvY * 2f - 1f;

            Vector3 camPos = new Vector3(0, 0, cameraZ);
            Vector3 rayDir = new Vector3(worldX, worldY, -fov).normalized;

            int closestBlob = -1;
            float closestDist = float.MaxValue;

            for (int i = 0; i < _blobCount; i++)
            {
                if (_blobStates[i] != BlobState.Normal) continue;

                Vector3 blobCenter = BlobPosition(i, t);
                Vector3 toBlob = blobCenter - camPos;
                float rayT = Vector3.Dot(toBlob, rayDir);
                Vector3 closestPoint = camPos + rayDir * rayT;
                float dist = Vector3.Distance(closestPoint, blobCenter);

                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestBlob = i;
                }
            }

            if (closestBlob >= 0 && closestDist < 1.5f)
                TriggerExplosion(closestBlob);
        }
    }

    void TriggerExplosion(int blobIndex)
    {
        _blobStates[blobIndex] = BlobState.Exploding;
        _blobTimers[blobIndex] = 0f;
        _blobProgress[blobIndex] = 0f;
    }

    void UpdateAllBlobStates()
    {
        for (int i = 0; i < _blobCount; i++)
        {
            if (_blobStates[i] == BlobState.Normal) continue;

            _blobTimers[i] += Time.deltaTime;

            switch (_blobStates[i])
            {
                case BlobState.Exploding:
                    _blobProgress[i] = Mathf.Clamp01(_blobTimers[i] / explosionDuration);
                    if (_blobTimers[i] >= explosionDuration)
                    {
                        _blobStates[i] = BlobState.Scattered;
                        _blobTimers[i] = 0f;
                    }
                    break;

                case BlobState.Scattered:
                    _blobProgress[i] = 1f;
                    if (_blobTimers[i] >= scatterDuration)
                    {
                        _blobStates[i] = BlobState.Reforming;
                        _blobTimers[i] = 0f;
                    }
                    break;

                case BlobState.Reforming:
                    float reformT = Mathf.Clamp01(_blobTimers[i] / reformDuration);
                    _blobProgress[i] = 1f - reformT;
                    if (_blobTimers[i] >= reformDuration)
                    {
                        _blobStates[i] = BlobState.Normal;
                        _blobTimers[i] = 0f;
                        _blobProgress[i] = 0f;
                    }
                    break;
            }
        }
    }

    void PushStatesToShader()
    {
        float[] states = new float[MAX_BLOBS];
        float[] progress = new float[MAX_BLOBS];

        for (int i = 0; i < MAX_BLOBS; i++)
        {
            states[i] = (float)_blobStates[i];
            progress[i] = _blobProgress[i];
        }

        lavaLampMaterial.SetFloatArray("_BlobStates", states);
        lavaLampMaterial.SetFloatArray("_BlobProgress", progress);
    }
}