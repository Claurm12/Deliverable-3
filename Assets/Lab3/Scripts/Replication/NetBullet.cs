using System.IO;
using UnityEngine;

namespace Delivery3
{
    public class NetBullet : NetObject
    {
        public const byte TypeId = 200;

        public const float Speed = 12f;
        public const float Lifetime = 2f;
        public const float Cooldown = 2.0f;
        public const int MaxAlivePerPlayer = 3;

        [System.NonSerialized]
        public LinkingContext replicaLinking;

        float m_lastServerUpdate;
        Vector3 m_targetPos;
        bool m_hasServerPos;

        public static NetBullet CreateVisual(Vector3 pos, float rotY)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "NetBullet";
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
            go.transform.localScale = Vector3.one * 0.4f;

            Collider col = go.GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;

            NetBullet bullet = go.AddComponent<NetBullet>();
            bullet.type = TypeId;
            bullet.m_targetPos = pos;
            return bullet;
        }

        void Start()
        {
            m_lastServerUpdate = Time.time;
        }

        void Update()
        {
            if (replicaLinking == null)
                return;

            if (m_hasServerPos)
            {
                transform.position = Vector3.Lerp(
                    transform.position,
                    m_targetPos,
                    Mathf.Clamp01(Time.deltaTime * 20f));
            }

            if (Time.time - m_lastServerUpdate > Lifetime + 2f)
            {
                replicaLinking.Unregister(id);
                Destroy(gameObject);
            }
        }

        public override void Read(BinaryReader reader)
        {
            Vector3 serverPos = new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle());

            float rotY = reader.ReadSingle();

            transform.rotation = Quaternion.Euler(0f, rotY, 0f);
            m_lastServerUpdate = Time.time;

            if (!m_hasServerPos || (transform.position - serverPos).sqrMagnitude > 2.25f)
            {
                transform.position = serverPos;
            }

            m_targetPos = serverPos;
            m_hasServerPos = true;
        }
    }
}