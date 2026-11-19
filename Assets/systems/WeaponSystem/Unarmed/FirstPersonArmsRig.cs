using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// First-person arms built from the character model. At startup a copy of the model is placed under the camera,
/// everything but its arms is hidden, and each frame its arms are posed to reach the FistsController pivots.
/// Arms bend at the elbow when the fist is close and stretch (rubber-arm style) when it is farther than the arm.
///
/// The real body stays on the player for the shadow (its renderers are set to Shadows Only), so the camera
/// never sees the body and the arms can't clip into it when looking around or jumping.
/// </summary>
public sealed class FirstPersonArmsRig : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FistsController fists;
    [Tooltip("Character model the arms are copied from (the player's model asset).")]
    [SerializeField] private GameObject modelPrefab;
    [Tooltip("Renderer in the model that contains the arms. Every other renderer in the copy is hidden.")]
    [SerializeField] private string armsRendererName = "Body";
    [Tooltip("The player's character in the scene. The copy takes its materials from the renderers with the same " +
             "names here, so it matches the player even when the scene overrides the model's materials.")]
    [SerializeField] private Transform materialSource;

    [Header("Placement (camera space)")]
    [Tooltip("Size of the copied model. Match the player's scale so the arms look the same.")]
    [Min(.01f)] [SerializeField] private float modelScale = 1.4f;
    [Tooltip("Where the point between the shoulders sits relative to the camera, in meters. " +
             "Below and slightly behind the camera keeps the torso out of view.")]
    [SerializeField] private Vector3 shoulderCenter = new Vector3(0f, -.35f, -.15f);

    [Header("Arms")]
    [Tooltip("Which way the elbows bend, in camera space (X is mirrored for the left arm).")]
    [SerializeField] private Vector3 elbowDirection = new Vector3(.6f, -1f, -.2f);
    [Tooltip("Let arms stretch longer than their real length to reach the fists.")]
    [SerializeField] private bool allowStretch = true;
    [Tooltip("Longest an arm can stretch, as a multiple of its real length.")]
    [Min(1f)] [SerializeField] private float maxStretch = 4f;
    [Tooltip("Wrist roll in degrees (mirrored for the left hand). Turn to face the knuckles forward.")]
    [Range(-180f, 180f)] [SerializeField] private float handRoll = 0f;

    [Header("Bone Names")]
    [SerializeField] private string leftUpperArm = "upper_arm.L";
    [SerializeField] private string leftForearm = "forearm.L";
    [SerializeField] private string leftHand = "hand.L";
    [SerializeField] private string rightUpperArm = "upper_arm.R";
    [SerializeField] private string rightForearm = "forearm.R";
    [SerializeField] private string rightHand = "hand.R";
    [Tooltip("Bones shrunk to nothing in the copy (neck/head, legs) so only the arms remain.")]
    [SerializeField] private string[] hiddenBones = { "spine.004", "thigh.L", "thigh.R" };

    private sealed class Arm
    {
        public Transform upper, fore, hand;
        public float upperLength, foreLength;
        public Quaternion upperOffset, foreOffset, handOffset;
        public Vector3 upperRestScale, foreRestScale, handRestScale;
        public int upperAxis, handAxis;
        public float side;
    }

    private GameObject copy;
    private Transform copyRoot;
    private Vector3 placedShoulderCenter;
    private Arm left, right;

    private void Awake()
    {
        if (modelPrefab == null || fists == null)
        {
            Debug.LogWarning("FirstPersonArmsRig needs a Model Prefab and Fists reference.", this);
            enabled = false;
            return;
        }

        copy = Instantiate(modelPrefab, transform);
        copy.name = "First-Person Arms (runtime)";
        copyRoot = copy.transform;
        copyRoot.localRotation = Quaternion.identity;
        copyRoot.localPosition = Vector3.zero;
        copyRoot.localScale = Vector3.one * modelScale;

        foreach (Animator animator in copy.GetComponentsInChildren<Animator>(true)) Destroy(animator);
        foreach (Collider collider in copy.GetComponentsInChildren<Collider>(true)) Destroy(collider);
        foreach (Renderer renderer in copy.GetComponentsInChildren<Renderer>(true))
        {
            bool isArms = renderer.name == armsRendererName;
            renderer.enabled = isArms;
            Transform source = materialSource != null ? FindDeep(materialSource, renderer.name) : null;
            Renderer sourceRenderer = source != null ? source.GetComponent<Renderer>() : null;
            if (sourceRenderer != null) renderer.sharedMaterials = sourceRenderer.sharedMaterials;
            renderer.shadowCastingMode = ShadowCastingMode.Off; // the real body already casts the shadow
            if (renderer is SkinnedMeshRenderer skinned) skinned.updateWhenOffscreen = true; // stretched arms leave the bounds
        }
        foreach (string boneName in hiddenBones)
        {
            Transform bone = FindDeep(copyRoot, boneName);
            if (bone != null) bone.localScale = Vector3.one * .0001f;
        }

        left = BuildArm(leftUpperArm, leftForearm, leftHand, -1f);
        right = BuildArm(rightUpperArm, rightForearm, rightHand, 1f);
        if (left == null || right == null)
        {
            Debug.LogWarning("FirstPersonArmsRig couldn't find the arm bones; check the Bone Names.", this);
            Destroy(copy);
            enabled = false;
            return;
        }
        PlaceShoulders();
    }

    private void LateUpdate()
    {
        bool visible = fists.isActiveAndEnabled && fists.FistsVisible;
        if (copy.activeSelf != visible) copy.SetActive(visible);
        if (!visible) return;

        if (placedShoulderCenter != shoulderCenter) PlaceShoulders(); // live tuning in Play Mode
        Solve(left, fists.LeftFist);
        Solve(right, fists.RightFist);
    }

    private void OnDestroy()
    {
        if (copy != null) Destroy(copy);
    }

    /// <summary>Moves the copy so the midpoint of its shoulders sits at <see cref="shoulderCenter"/>.</summary>
    private void PlaceShoulders()
    {
        Vector3 mid = (left.upper.position + right.upper.position) * .5f;
        copyRoot.localPosition += shoulderCenter - transform.InverseTransformPoint(mid);
        placedShoulderCenter = shoulderCenter;
    }

    private Arm BuildArm(string upperName, string foreName, string handName, float side)
    {
        Transform upper = FindDeep(copyRoot, upperName);
        Transform fore = FindDeep(copyRoot, foreName);
        Transform hand = FindDeep(copyRoot, handName);
        if (upper == null || fore == null || hand == null) return null;

        Vector3 up = transform.up;
        Vector3 upperDir = (fore.position - upper.position).normalized;
        Vector3 foreDir = (hand.position - fore.position).normalized;
        return new Arm
        {
            upper = upper, fore = fore, hand = hand, side = side,
            upperLength = Vector3.Distance(upper.position, fore.position),
            foreLength = Vector3.Distance(fore.position, hand.position),
            // Offsets map "a rotation that looks along the bone" back to the bone's own rotation,
            // so posing works whatever axis the rig uses for its bones.
            upperOffset = Quaternion.Inverse(Quaternion.LookRotation(upperDir, up)) * upper.rotation,
            foreOffset = Quaternion.Inverse(Quaternion.LookRotation(foreDir, up)) * fore.rotation,
            handOffset = Quaternion.Inverse(Quaternion.LookRotation(foreDir, up)) * hand.rotation,
            upperRestScale = upper.localScale, foreRestScale = fore.localScale, handRestScale = hand.localScale,
            upperAxis = DominantAxis(Quaternion.Inverse(upper.rotation) * upperDir),
            handAxis = DominantAxis(Quaternion.Inverse(hand.rotation) * foreDir),
        };
    }

    private void Solve(Arm arm, Transform target)
    {
        if (target == null) return;
        arm.upper.localScale = arm.upperRestScale;
        arm.fore.localScale = arm.foreRestScale;
        arm.hand.localScale = arm.handRestScale;

        Vector3 shoulder = arm.upper.position;
        Vector3 toTarget = target.position - shoulder;
        float distance = toTarget.magnitude;
        if (distance < 1e-4f) return;
        Vector3 dir = toTarget / distance;
        Vector3 up = transform.up;
        float length = arm.upperLength + arm.foreLength;

        Vector3 upperDir, foreDir;
        float stretch = 1f;
        if (distance >= length)
        {
            // Fully extended; stretch the rest of the way.
            upperDir = foreDir = dir;
            if (allowStretch) stretch = Mathf.Min(distance / length, maxStretch);
        }
        else
        {
            // Two-bone bend, elbow toward elbowDirection.
            Vector3 hint = transform.TransformDirection(new Vector3(elbowDirection.x * arm.side, elbowDirection.y, elbowDirection.z));
            Vector3 bendAxis = Vector3.ProjectOnPlane(hint, dir);
            if (bendAxis.sqrMagnitude < 1e-6f) bendAxis = Vector3.ProjectOnPlane(-up, dir);
            bendAxis.Normalize();

            float a = arm.upperLength, b = arm.foreLength;
            float cos = Mathf.Clamp((a * a + distance * distance - b * b) / (2f * a * distance), -1f, 1f);
            float sin = Mathf.Sqrt(1f - cos * cos);
            upperDir = (dir * cos + bendAxis * sin).normalized;
            Vector3 elbow = shoulder + upperDir * a;
            foreDir = (target.position - elbow).normalized;
        }

        arm.upper.rotation = Quaternion.LookRotation(upperDir, up) * arm.upperOffset;
        arm.fore.rotation = Quaternion.LookRotation(foreDir, up) * arm.foreOffset;
        arm.hand.rotation = Quaternion.LookRotation(foreDir, up) * Quaternion.AngleAxis(handRoll * arm.side, Vector3.forward) * arm.handOffset;

        if (stretch > 1.0001f)
        {
            // Stretch the upper arm along its length (the forearm, in line with it, stretches with it),
            // and undo it on the hand so the fist keeps its size.
            arm.upper.localScale = ScaleAxis(arm.upperRestScale, arm.upperAxis, stretch);
            arm.hand.localScale = ScaleAxis(arm.handRestScale, arm.handAxis, 1f / stretch);
        }
    }

    private static Vector3 ScaleAxis(Vector3 scale, int axis, float factor)
    {
        scale[axis] *= factor;
        return scale;
    }

    private static int DominantAxis(Vector3 v)
    {
        v = new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        return v.x > v.y ? (v.x > v.z ? 0 : 2) : (v.y > v.z ? 1 : 2);
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}
