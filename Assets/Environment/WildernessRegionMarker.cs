using UnityEngine;

namespace DevilsHarvest
{
    /// <summary>Editor placement guidance only; deliberately contains no gameplay logic.</summary>
    public sealed class WildernessRegionMarker : MonoBehaviour
    {
        [TextArea] public string purpose;
        public float radius = 10f;
        public Color color = new Color(0.85f, 0.65f, 0.22f, 0.8f);
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = color;
            Vector3 previous = transform.position + Vector3.right * radius;
            for (int i = 1; i <= 48; i++)
            {
                float a = i * Mathf.PI * 2f / 48f;
                Vector3 point = transform.position + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius;
                Gizmos.DrawLine(previous, point); previous = point;
            }
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 3f);
        }
    }
}
