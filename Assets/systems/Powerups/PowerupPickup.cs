using UnityEngine;

/// <summary>
/// A powerup in the world. The player collects it by walking into it. Needs a Collider marked Is Trigger
/// (one is added if the object has none). Spins and bobs so it reads as a pickup.
/// </summary>
public sealed class PowerupPickup : MonoBehaviour
{
    [Tooltip("What this pickup gives.")]
    [SerializeField] private PowerupData powerup;
    [Tooltip("Seconds until it comes back after being collected. 0 = one use only.")]
    [Min(0f)] [SerializeField] private float respawnTime = 0f;

    [Header("Idle Animation")]
    [Tooltip("Spin speed in degrees per second. 0 = no spin.")]
    [SerializeField] private float spinSpeed = 90f;
    [Tooltip("How far it floats up and down, in meters. 0 = no bob.")]
    [Min(0f)] [SerializeField] private float bobHeight = .15f;
    [Min(0f)] [SerializeField] private float bobSpeed = 2f;

    private Vector3 basePosition;
    private Renderer[] renderers;
    private Collider[] colliders;
    private float respawnAt = -1f;

    public PowerupData Powerup => powerup;
    [Tooltip("Stable save key for a one-use exploration reward.")]
    public string cacheId;
    [Min(0)] public int cachedMeat;

    private void Start()
    {
        var progress = SaveGameService.ActiveSave?.progress;
        if (!string.IsNullOrEmpty(cacheId) && progress?.exploredCaches != null && progress.exploredCaches.Contains(cacheId))
            gameObject.SetActive(false);
    }

    private void Awake()
    {
        basePosition = transform.localPosition;
        colliders = GetComponentsInChildren<Collider>(true);
        if (colliders.Length == 0)
        {
            var sphere = gameObject.AddComponent<SphereCollider>();
            sphere.radius = .5f;
            colliders = new Collider[] { sphere };
        }
        foreach (Collider collider in colliders) collider.isTrigger = true;
        renderers = GetComponentsInChildren<Renderer>(true);
        if (!string.IsNullOrEmpty(cacheId) && powerup != null)
        {
            var tint = new MaterialPropertyBlock();
            foreach (var surface in renderers)
            {
                surface.GetPropertyBlock(tint);
                tint.SetColor("_BaseColor", powerup.color);
                tint.SetColor("_Color", powerup.color);
                surface.SetPropertyBlock(tint);
            }
        }
    }

    private void Update()
    {
        if (respawnAt >= 0f && Time.time >= respawnAt) SetAvailable(true);

        if (spinSpeed != 0f) transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        if (bobHeight > 0f)
            transform.localPosition = basePosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (powerup == null || respawnAt >= 0f) return;
        Player player = other.GetComponentInParent<Player>();
        if (player == null || player.IsDead) return;

        var progress = SaveGameService.ActiveSave?.progress;
        if (!string.IsNullOrEmpty(cacheId) && progress != null)
        {
            if (progress.exploredCaches == null) progress.exploredCaches = new System.Collections.Generic.List<string>();
            if (progress.exploredCaches.Contains(cacheId)) return;
            progress.exploredCaches.Add(cacheId);
            progress.meat += cachedMeat;
        }
        PlayerPowerups.For(player.gameObject).Apply(powerup);
        if (!string.IsNullOrEmpty(cacheId))
        {
            gameObject.SetActive(false);
            SaveGameService.SaveActive();
            return;
        }

        if (respawnTime > 0f)
        {
            SetAvailable(false);
            respawnAt = Time.time + respawnTime;
        }
        else gameObject.SetActive(false);
    }

    private void SetAvailable(bool available)
    {
        if (available) respawnAt = -1f;
        foreach (Renderer renderer in renderers) if (renderer != null) renderer.enabled = available;
        foreach (Collider collider in colliders) if (collider != null) collider.enabled = available;
    }
}
