using UnityEngine;

namespace Delivery3
{
    public class PlayerController : MonoBehaviour
    {
        public int objectId;

        private Rigidbody m_rb;
        private float m_sendTimer;
        private float m_nextShotTime;   
        private float m_verticalVelocity = 0f;
        private const float m_gravity = -20f;
        private const float m_jumpForce = 8f;

        void Start()
        {
            m_rb = GetComponent<Rigidbody>();
        }

        void Update()
        {
            if (LobbyGUI.NeedsSession()) return;
            LobbySession session = LobbySession.Instance;
            LobbyClient client = session.Client;

            m_sendTimer += Time.deltaTime;
            if (m_sendTimer >= 0.05f)
            {
                m_sendTimer = 0f;
                byte[] packet = Messages.Transform(objectId, transform.position, transform.eulerAngles.y);

                if (session.IsHost)
                {
                    session.Server.BroadcastCustomBytes(packet);
                }
                else
                {
                    client.SendCustomBytes(packet);
                }
            }

            if (!GameUI.IsTypingInChat && GameUI.ShootRequested)
            {
                GameUI.ShootRequested = false;

                string myName = client.userName;
                string currentRole = "Chased";
                PlayerDTO2 data;
                if (client.Teams.TryGetValue(myName, out data) && !string.IsNullOrEmpty(data.playerRole))
                {
                    currentRole = data.playerRole;
                }

                if (currentRole == "Hunter" && Time.time < m_nextShotTime)
                {
                    // bala en cooldown
                }
                else if (currentRole == "Hunter")
                {
                    m_nextShotTime = Time.time + NetBullet.Cooldown;

                    Vector3 spawnPos = transform.position + transform.forward * 0.8f;
                    Vector3 shootDir = transform.forward;

                    GameManager.Instance.SpawnAndSendBullet(session, client, objectId, spawnPos, shootDir);
                }
                else
                {
                    Debug.Log("No puedes disparar porque eres el que escapa.");
                }
            }
        }

        void FixedUpdate()
        {
            if (LobbyGUI.NeedsSession() || GameUI.IsTypingInChat) return;

            float moveSpeed = 6f;
            float h = 0f;
            float v = 0f;

            if (GameUI.MoveW) v += 1f;
            if (GameUI.MoveS) v -= 1f;
            if (GameUI.MoveA) h -= 1f;
            if (GameUI.MoveD) h += 1f;

            Vector3 direction = new Vector3(h, 0, v);
            Vector3 currentPos = (m_rb != null) ? m_rb.position : transform.position;

            // Gestión del salto
            if (GameUI.JumpRequested && currentPos.y <= 0.51f)
            {
                m_verticalVelocity = m_jumpForce;
                GameUI.JumpRequested = false;
            }

            if (currentPos.y > 0.5f || m_verticalVelocity != 0f)
            {
                m_verticalVelocity += m_gravity * Time.fixedDeltaTime;
                currentPos.y += m_verticalVelocity * Time.fixedDeltaTime;

                if (currentPos.y <= 0.5f)
                {
                    currentPos.y = 0.5f;
                    m_verticalVelocity = 0f;
                }
            }

            if (direction.magnitude > 0.1f)
            {
                Vector3 moveDir = direction.normalized;
                currentPos += moveDir * moveSpeed * Time.fixedDeltaTime;

                // Girar suavemente hacia la dirección en la que nos movemos
                Quaternion targetRotation = Quaternion.LookRotation(moveDir);
                if (m_rb != null)
                {
                    m_rb.MoveRotation(Quaternion.Slerp(m_rb.rotation, targetRotation, 15f * Time.fixedDeltaTime));
                }
                else
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 15f * Time.fixedDeltaTime);
                }
            }

            // Aplicar movimiento físico y frenar rebotes residuales
            if (m_rb != null)
            {
                m_rb.linearVelocity = Vector3.zero;
                m_rb.angularVelocity = Vector3.zero;
                m_rb.MovePosition(currentPos);
            }
            else
            {
                transform.position = currentPos;
            }
        }
    }
}