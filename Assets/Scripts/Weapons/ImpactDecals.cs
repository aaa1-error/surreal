using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Places a bullet-hole decal where a shot lands, using URP's Decal Projector so it wraps corners
/// and curved surfaces. Decals are pooled: once <see cref="_maxDecals"/> exist, the oldest is reused.
///
/// Needs: the Decal Renderer Feature on your URP Renderer asset, and a material using the shader
/// "Shader Graphs/Decal". Put ONE of these in the scene (at scale 1).
/// </summary>
public class ImpactDecals : MonoBehaviour
{
    public static ImpactDecals Instance { get; private set; }

    [Tooltip("Material using the shader Shader Graphs/Decal, with the bullet hole texture as its Base Map.")]
    [SerializeField] private Material _decalMaterial;
    [Tooltip("Decals alive at once. When full, the oldest one is moved to the new hit.")]
    [SerializeField] private int _maxDecals = 64;

    [Header("Look")]
    [Tooltip("Width and height of a decal, in meters.")]
    [SerializeField] private float _size = 0.15f;
    [Tooltip("Random size variation")]
    [Range(0f, 0.5f)]
    [SerializeField] private float _sizeVariation = 0.2f;
    [Tooltip("How deep the projection box reaches into the surface. Larger values wrap further around curves and corners.")]
    [SerializeField] private float _projectionDepth = 0.3f;
    [Tooltip("Decals farther than this from the camera are not drawn.")]
    [SerializeField] private float _drawDistance = 64f;
    [Tooltip("How far in front of the surface the projection box starts.")]
    [SerializeField] private float _surfaceOffset = 0.05f;

    private DecalProjector[] _pool;
    private int _next;

    // ---------------------------------------------------------------- Public API

    /// <summary>Safe to call when no ImpactDecals exists in the scene (it just does nothing).</summary>
    public static void Spawn(Vector3 point, Vector3 normal)
    {
        if (Instance != null)
            Instance.Place(point, normal);
    }

    // ---------------------------------------------------------------- Unity messages

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("More than one ImpactDecals in the scene; ignoring this one.", this);
            enabled = false;
            return;
        }

        if (_decalMaterial == null)
        {
            Debug.LogError("ImpactDecals: no decal material assigned.", this);
            enabled = false;
            return;
        }

        if (!_decalMaterial.shader.name.Contains("Decal"))
            Debug.LogWarning($"ImpactDecals: the material uses the shader '{_decalMaterial.shader.name}'. " +
                             "It should be 'Shader Graphs/Decal', otherwise nothing will be projected.", this);

        Instance = this;
        _pool = new DecalProjector[Mathf.Max(_maxDecals, 1)];
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ---------------------------------------------------------------- Placing

    private void Place(Vector3 point, Vector3 normal)
    {
        if (normal.sqrMagnitude < 0.0001f)
            return;

        DecalProjector decal = GetNextProjector();
        float size = _size * (1f + Random.Range(-_sizeVariation, _sizeVariation));

        // A projector shines along its local +Z, so +Z must point INTO the surface.
        // The random roll around that axis stops every hole from looking identical.
        Quaternion rotation = Quaternion.LookRotation(-normal) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        decal.transform.SetPositionAndRotation(point + normal * _surfaceOffset, rotation);

        // With the pivot at half the depth, the box starts at the transform and extends into the surface.
        decal.size = new Vector3(size, size, _projectionDepth);
        decal.pivot = new Vector3(0f, 0f, _projectionDepth * 0.5f);
        decal.gameObject.SetActive(true);
    }

    private DecalProjector GetNextProjector()
    {
        DecalProjector decal = _pool[_next];

        // Create on first use (or if something destroyed it).
        if (decal == null)
        {
            GameObject go = new GameObject($"Decal {_next}");
            go.transform.SetParent(transform, false);
            go.SetActive(false);  // don't draw until it has been positioned

            decal = go.AddComponent<DecalProjector>();
            decal.material = _decalMaterial;
            decal.drawDistance = _drawDistance;
            _pool[_next] = decal;
        }

        _next = (_next + 1) % _pool.Length;
        return decal;
    }
}
