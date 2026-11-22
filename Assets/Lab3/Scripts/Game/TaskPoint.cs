using UnityEngine;

namespace Delivery3
{
    public class TaskPoint : MonoBehaviour
    {
        [Min(0.5f)] public float radius = 2f;

        void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}