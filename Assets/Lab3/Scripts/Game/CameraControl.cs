using UnityEngine;

namespace Delivery3
{
    public class CameraControl : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 5f, -10f);
        public float smoothSpeed = 10f;

        void Start()
        {
            transform.eulerAngles = new Vector3(25f, 0f, 0f);
        }

        void LateUpdate()
        {
            if (target != null)
            {
                Vector3 targetPosition = target.position + offset;
                transform.position = Vector3.Lerp(
                    transform.position,
                    targetPosition,
                    smoothSpeed * Time.deltaTime
                );
            }
        }
    }
}