using UnityEngine;

/// <summary>
/// Moves the player body's hands toward the FistsController pivots with Humanoid IK, so the player's shadow
/// punches along with the first-person arms (the body itself is Shadows Only; the arms you see come from
/// FirstPersonArmsRig). Reaches up to normal arm length; it can't stretch.
///
/// Requirements: put this on the object with the Animator (Humanoid avatar), and tick "IK Pass" on the
/// Animator Controller's Base Layer (Animator window > layer gear icon). Without IK Pass, OnAnimatorIK never runs.
/// </summary>
[RequireComponent(typeof(Animator))]
public sealed class FirstPersonArmsIK : MonoBehaviour
{
    [SerializeField] private FistsController fists;

    [Header("Blend")]
    [Tooltip("How strongly the hands stick to the fist pivots (0 = normal body animation, 1 = fully on the pivots).")]
    [Range(0f, 1f)] [SerializeField] private float positionWeight = 1f;
    [Tooltip("How strongly the hands match the pivots' rotation. Use the offsets below to turn them into fists facing forward.")]
    [Range(0f, 1f)] [SerializeField] private float rotationWeight = 1f;
    [Tooltip("Seconds to raise or lower the arms when switching between fists and a weapon.")]
    [Min(0f)] [SerializeField] private float blendTime = .15f;

    [Header("Hand Rotation (degrees, relative to each fist pivot)")]
    [SerializeField] private Vector3 leftHandRotationOffset = new Vector3(0f, 0f, 90f);
    [SerializeField] private Vector3 rightHandRotationOffset = new Vector3(0f, 0f, -90f);

    [Header("Elbows")]
    [Tooltip("Pushes the elbows out and down so the arms bend naturally (in the camera's local space, meters).")]
    [SerializeField] private Vector3 elbowHintOffset = new Vector3(.35f, -.35f, -.1f);
    [Range(0f, 1f)] [SerializeField] private float elbowHintWeight = .6f;

    private Animator animator;
    private float blend;

    private void Awake() => animator = GetComponent<Animator>();

    private void Update()
    {
        bool active = fists != null && fists.isActiveAndEnabled && fists.FistsVisible;
        float step = blendTime > 0f ? Time.deltaTime / blendTime : 1f;
        blend = Mathf.MoveTowards(blend, active ? 1f : 0f, step);
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (fists == null) return;
        ApplyHand(AvatarIKGoal.LeftHand, AvatarIKHint.LeftElbow, fists.LeftFist, leftHandRotationOffset, -1f);
        ApplyHand(AvatarIKGoal.RightHand, AvatarIKHint.RightElbow, fists.RightFist, rightHandRotationOffset, 1f);
    }

    private void ApplyHand(AvatarIKGoal goal, AvatarIKHint hint, Transform target, Vector3 rotationOffset, float side)
    {
        float weight = target != null ? blend : 0f;
        animator.SetIKPositionWeight(goal, weight * positionWeight);
        animator.SetIKRotationWeight(goal, weight * rotationWeight);
        animator.SetIKHintPositionWeight(hint, weight * elbowHintWeight);
        if (weight <= 0f) return;

        animator.SetIKPosition(goal, target.position);
        animator.SetIKRotation(goal, target.rotation * Quaternion.Euler(rotationOffset));

        Transform space = target.parent != null ? target.parent : target;
        Vector3 hintLocal = new Vector3(elbowHintOffset.x * side, elbowHintOffset.y, elbowHintOffset.z);
        animator.SetIKHintPosition(hint, target.position + space.TransformVector(hintLocal));
    }
}
