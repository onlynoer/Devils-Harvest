using UnityEngine;
using UnityEngine.InputSystem;

namespace DevilsHarvest
{
    /// <summary>Temporary map test rig. No hunting, inventory, or quota systems.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class WildernessTestPlayer : MonoBehaviour
    {
        public Terrain ground;
        public Transform spawn;
        public Camera playerCamera;
        public float seaLevel = 18f;
        [Min(.1f), InspectorName("Walk Speed")] public float speed = 5.5f;
        [Min(.1f)] public float sprintSpeed = 8.8f;
        public float jumpHeight = 1.25f;
        public float sensitivity = 0.085f;
        public bool showHelp = true;

        private CharacterController body;
        private InputAction move, look, jump, reset, sprint;
        public bool IsSprinting { get; private set; }
        public float CurrentMoveSpeed { get; private set; }
        private float vertical, pitch;
        private bool ready;
        private const float Gravity = 22f;

        private void Awake()
        {
            body = GetComponent<CharacterController>();
            move = new InputAction("Map Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            look = new InputAction("Map Look", InputActionType.Value, "<Mouse>/delta");
            jump = new InputAction("Map Jump", InputActionType.Button, "<Keyboard>/space");
            reset = new InputAction("Map Respawn", InputActionType.Button, "<Keyboard>/r");
            sprint = new InputAction("Map Sprint", InputActionType.Button, "<Keyboard>/leftShift");
        }

        private void OnEnable()
        {
            move.Enable(); look.Enable(); jump.Enable(); reset.Enable(); sprint.Enable();
        }

        private void Start()
        {
            var collider = ground != null ? ground.GetComponent<TerrainCollider>() : null;
            if (collider == null || !collider.enabled || collider.terrainData != ground.terrainData ||
                !ground.gameObject.activeInHierarchy || spawn == null || playerCamera == null ||
                Physics.GetIgnoreLayerCollision(gameObject.layer, ground.gameObject.layer))
            {
                Debug.LogError("Map test stopped: check assigned Terrain, enabled TerrainCollider with matching TerrainData, spawn, camera, and layer collisions. Enable built-in Terrain Physics in Package Manager.", this);
                enabled = false;
                return;
            }
            ready = Respawn();
            if (ready) Capture();
        }

        private void Update()
        {
            if (!ready) return;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.escapeKey.wasPressedThisFrame) Release();
                if (Keyboard.current.f1Key.wasPressedThisFrame) showHelp = !showHelp;
            }
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Application.isFocused) Capture();
            Vector3 local = transform.position - ground.transform.position;
            bool outside = local.x < 0f || local.z < 0f || local.x > ground.terrainData.size.x || local.z > ground.terrainData.size.z;
            float surface = ground.SampleHeight(transform.position) + ground.transform.position.y;
            if (reset.WasPressedThisFrame() || outside || transform.position.y < seaLevel - 0.7f ||
                (transform.position.y < surface - 1f && !OverTerrainHole(local)))
            {
                ready = Respawn();
                return;
            }

            bool captured = Cursor.lockState == CursorLockMode.Locked;
            if (captured)
            {
                Vector2 delta = look.ReadValue<Vector2>() * sensitivity;
                transform.Rotate(0f, delta.x, 0f);
                pitch = Mathf.Clamp(pitch - delta.y, -85f, 85f);
                playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
            Vector2 input = captured ? Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f) : Vector2.zero;
            if (body.isGrounded && vertical < 0f) vertical = -3f;
            if (captured && body.isGrounded && jump.WasPressedThisFrame())
                vertical = Mathf.Sqrt(2f * Gravity * jumpHeight);
            float dt = Mathf.Min(Time.deltaTime, 0.1f);
            vertical = Mathf.Max(vertical - Gravity * dt, -45f);
            IsSprinting = captured && input.sqrMagnitude > .001f && sprint.IsPressed();
            CurrentMoveSpeed = IsSprinting ? Mathf.Max(speed, sprintSpeed) : speed;
            Vector3 velocity = (transform.right * input.x + transform.forward * input.y) * CurrentMoveSpeed + Vector3.up * vertical;
            // Small movement steps protect collision during frame stalls and downhill travel.
            int steps = Mathf.Max(1, Mathf.CeilToInt(velocity.magnitude * dt / 0.25f));
            for (int i = 0; i < steps; i++)
            {
                CollisionFlags flags = body.Move(velocity * (dt / steps));
                if ((flags & CollisionFlags.Above) != 0 && vertical > 0f) vertical = 0f;
            }
        }

        public bool Respawn()
        {
            if (ground == null || spawn == null) return false;
            var collider = ground.GetComponent<TerrainCollider>();
            Vector3 target = spawn.position;
            Vector3 local = target - ground.transform.position;
            if (local.x < 0 || local.z < 0 || local.x > ground.terrainData.size.x || local.z > ground.terrainData.size.z)
                return Fail("Spawn marker is outside the assigned Terrain.");
            Physics.SyncTransforms();
            float surface = ground.SampleHeight(target) + ground.transform.position.y;
            Ray ray = new Ray(new Vector3(target.x, ground.transform.position.y + ground.terrainData.size.y + 20f, target.z), Vector3.down);
            if (collider == null || !collider.Raycast(ray, out RaycastHit hit, ground.terrainData.size.y + 40f) || Mathf.Abs(hit.point.y - surface) > 0.15f)
                return Fail("Terrain collider raycast failed or disagrees with the visible surface. Check Terrain Physics module and collider data.");
            // Root denotes capsule feet; sample current, edited terrain in world space each time.
            target.y = hit.point.y + 0.6f;
            body.enabled = false;
            Vector3 bottom = target + Vector3.up * body.radius;
            Vector3 top = target + Vector3.up * (body.height - body.radius);
            if (Physics.CheckCapsule(bottom, top, body.radius, ~0, QueryTriggerInteraction.Ignore))
            {
                body.enabled = true;
                return Fail("Spawn capsule is obstructed. Move the spawn marker to a clear part of the beach.");
            }
            transform.SetPositionAndRotation(target, Quaternion.Euler(0f, spawn.eulerAngles.y, 0f));
            body.enabled = true;
            Physics.SyncTransforms();
            vertical = 0f; pitch = 0f;
            playerCamera.transform.localRotation = Quaternion.identity;
            return true;
        }

        // Terrain.SampleHeight still returns a height over a hole. Actual openings must not look like tunneling.
        private bool OverTerrainHole(Vector3 local)
        {
            var data=ground.terrainData;
            int x=Mathf.Clamp(Mathf.FloorToInt(local.x/data.size.x*data.holesResolution),0,data.holesResolution-1);
            int z=Mathf.Clamp(Mathf.FloorToInt(local.z/data.size.z*data.holesResolution),0,data.holesResolution-1);
            return data.IsHole(x,z);
        }

        private bool Fail(string message)
        {
            Debug.LogError(message, this);
            ready = false;
            Release();
            return false;
        }

        private static void Capture() { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        private static void Release() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        private void OnApplicationFocus(bool focus) { if (!focus) Release(); }
        private void OnDisable()
        {
            move?.Disable(); look?.Disable(); jump?.Disable(); reset?.Disable(); sprint?.Disable(); IsSprinting = false; Release();
        }
        private void OnDestroy() { move?.Dispose(); look?.Dispose(); jump?.Dispose(); reset?.Dispose(); sprint?.Dispose(); }
        private void OnGUI()
        {
            if (!showHelp) return;
            GUI.Box(new Rect(12, 12, 510, 76), "Devil's Harvest | Wilderness map test\nWASD move | Hold Left Shift sprint | Mouse look | Space jump\nR beach respawn | Esc release | Click capture | F1 hide help\n" +
                (ready ? "Grounded: " + body.isGrounded + "   Feet Y: " + transform.position.y.ToString("F1") : "Map test stopped - check Console."));
        }
    }
}
