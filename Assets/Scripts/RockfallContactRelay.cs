using UnityEngine;

namespace DevilsHarvest
{
    /// <summary>Reusable with the team's eventual controller; CharacterController hits need their own relay.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class RockfallContactRelay : MonoBehaviour
    {
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            var rock=hit.collider.GetComponentInParent<RockfallBoulder>();
            if(rock!=null)rock.TryImpact(GetComponent<CharacterController>(),hit.point);
        }
    }
}
