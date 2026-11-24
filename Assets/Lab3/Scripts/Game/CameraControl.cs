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

        [Header("Soft Zoom Avoidance")]
        public float minDistance = 4.5f;
        public float zoomSmoothing = 6f;
        public float castRadius = 0.3f;
        public float skinOffset = 0.4f;
        public LayerMask obstacleMask = ~0;

        public float Yaw { get; private set; }

        float m_pitch = 25f;
        float m_currentDistance;
        Vector3 m_pivot;
        Transform m_lastTarget;

        void Awake()
        {
            Instance = this;
            m_currentDistance = distance;
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
                m_currentDistance = distance;
            }

            Vector2 drag = GameUI.ConsumeCameraDrag();
            Yaw += drag.x * mouseSensitivity;
            m_pitch = Mathf.Clamp(m_pitch + drag.y * mouseSensitivity, minPitch, maxPitch);

            m_pivot = Vector3.Lerp(m_pivot, desiredPivot, Mathf.Clamp01(followSmoothing * Time.deltaTime));

            Quaternion orbit = Quaternion.Euler(m_pitch, Yaw, 0f);
            Vector3 desiredDir = -(orbit * Vector3.forward);
            float targetDistance = distance;

            if (Physics.SphereCast(
                m_pivot,
                castRadius,
                desiredDir,
                out RaycastHit hit,
                distance,
                obstacleMask,
                QueryTriggerInteraction.Ignore))
            {
                float safeMin = Mathf.Min(minDistance, distance);
                targetDistance = Mathf.Clamp(hit.distance - skinOffset, safeMin, distance);
            }

            m_currentDistance = Mathf.Lerp(
                m_currentDistance,
                targetDistance,
                Mathf.Clamp01(zoomSmoothing * Time.deltaTime));

            transform.position = m_pivot + desiredDir * m_currentDistance;
            transform.rotation = orbit;
        }
    }
}