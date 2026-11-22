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

        float m_spawnTime;
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
            return bullet;
        }

        void Start()
        {
            m_spawnTime = Time.time;
        }

        void Update()
        {
            if (replicaLinking == null)
                return;

            transform.position +=
                transform.forward * Speed * Time.deltaTime;

            if (Time.time - m_spawnTime > Lifetime + 2f)
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

            if (!m_hasServerPos ||
                (transform.position - serverPos).sqrMagnitude > 1.5f * 1.5f)
            {
                transform.position = serverPos;
            }

            m_hasServerPos = true;
        }
    }
}