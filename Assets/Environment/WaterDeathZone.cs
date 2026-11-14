using UnityEngine;

/// <summary>Kills player characters that enter the volume below the ocean surface.</summary>
[RequireComponent(typeof(BoxCollider))]
public sealed class WaterDeathZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other == null)
        {
            return;
        }

        var player = other.GetComponentInParent<Player>();
        if (player == null || player.IsDead)
        {
            return;
        }

        player.Kill();
        Debug.Log("Player drowned.", player);
    }
}
