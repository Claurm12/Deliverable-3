using System.IO;
using UnityEngine;

namespace Delivery3
{
    public class NetPlayer : NetObject
    {
        public int health = 100;
        public string playerRole = "Chased";
        public bool isMoving = false;
        public bool isDead = false;
        public int shotCount = 0;

        private Animator m_animator;
        private int m_lastAppliedShotCount = 0;
        private bool m_initializedShotCount = false;

        void Awake()
        {
            m_animator = GetComponentInChildren<Animator>();
        }

        public void TriggerShootAnimation()
        {
            shotCount++;
            m_lastAppliedShotCount = shotCount;
            m_initializedShotCount = true;
            if (m_animator != null)
            {
                m_animator.ResetTrigger("Shoot");
                m_animator.SetTrigger("Shoot");
            }
        }

        public void ApplyAnimationState()
        {
            if (m_animator == null)
                m_animator = GetComponentInChildren<Animator>();
            if (m_animator == null) return;

            m_animator.SetBool("IsMoving", isMoving);
            m_animator.SetBool("IsDead", isDead);

            /*
            if (!m_initializedShotCount)
            {
                m_lastAppliedShotCount = shotCount;
                m_initializedShotCount = true;
                return;
            }

            if (shotCount > m_lastAppliedShotCount)
            {
                int diff = shotCount - m_lastAppliedShotCount;
                m_lastAppliedShotCount = shotCount;
                if (!isDead && diff > 0)
                {
                    m_animator.ResetTrigger("Shoot");
                    m_animator.SetTrigger("Shoot");
                }
            }
            else if (shotCount < m_lastAppliedShotCount)
            {
                m_lastAppliedShotCount = shotCount;
            }
            */
        }

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(health);
            writer.Write(playerRole ?? "");
            writer.Write(isMoving);
            writer.Write(isDead);
            writer.Write(shotCount);
        }

        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            health = reader.ReadInt32();
            playerRole = reader.ReadString();
            isMoving = reader.ReadBoolean();
            isDead = reader.ReadBoolean();
            shotCount = reader.ReadInt32();

            ApplyAnimationState();
        }
    }
}