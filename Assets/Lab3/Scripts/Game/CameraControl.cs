using UnityEngine;

namespace Delivery3
{
    public class CameraControl : MonoBehaviour
    {
        public static CameraControl Instance { get; private set; }

        public Transform target;
        public float distance = 10.5f;
        public float pivotHeight = 1f;
        public float mouseSensitivity = 0.25f;
        public float minPitch = 5f;
        public float maxPitch = 70f;
        public float followSmoothing = 15f;

        public float Yaw { get; private set; }

        float m_pitch = 25f;
        Vector3 m_pivot;
        Transform m_lastTarget;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void LateUpdate()
        {
            if (target == null)
                return;

            Vector3 desiredPivot = target.position + Vector3.up * pivotHeight;

            if (target != m_lastTarget)
            {
                m_lastTarget = target;
                Yaw = target.eulerAngles.y;
                m_pivot = desiredPivot;
            }

            Vector2 drag = GameUI.ConsumeCameraDrag();
            Yaw += drag.x * mouseSensitivity;
            m_pitch = Mathf.Clamp(m_pitch + drag.y * mouseSensitivity, minPitch, maxPitch);

            m_pivot = Vector3.Lerp(m_pivot, desiredPivot, Mathf.Clamp01(followSmoothing * Time.deltaTime));

            Quaternion orbit = Quaternion.Euler(m_pitch, Yaw, 0f);
            transform.position = m_pivot - orbit * Vector3.forward * distance;
            transform.rotation = orbit;
        }
    }
}
