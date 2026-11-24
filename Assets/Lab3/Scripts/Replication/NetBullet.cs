using System.IO;
using UnityEngine;

namespace Delivery3
{
    public class NetBullet : NetObject
    {
        public const byte TypeId = 200;

        public const float Speed = 25f;
        public const float Lifetime = 2f;
        public const float Cooldown = 2.0f;
        public const int MaxAlivePerPlayer = 3;

        const float Size = 0.18f;
        const string MaterialResource = "NetBulletMaterial";

        static readonly Color CoreColor = new Color(1f, 0.95f, 0.7f);
        static readonly Color GoldColor = new Color(1f, 0.72f, 0.15f);

        static Material s_material;

        [System.NonSerialized]
        public LinkingContext replicaLinking;

        float m_lastServerUpdate;
        Vector3 m_serverPos;
        bool m_hasServerPos;
        TrailRenderer[] m_trails;

        static Material BulletMaterial()
        {
            if (s_material != null)
                return s_material;

            s_material = Resources.Load<Material>(MaterialResource);

            if (s_material == null)
            {
                Debug.LogWarning("[NetBullet] Missing Resources/" + MaterialResource + ".mat, using a runtime material");
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                s_material = new Material(shader);
            }

            return s_material;
        }

        static void SetColor(Renderer renderer, Color color)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            renderer.SetPropertyBlock(block);
        }

        static TrailRenderer AddTrail(GameObject go, float width, float time, Color color)
        {
            TrailRenderer trail = go.AddComponent<TrailRenderer>();
            trail.sharedMaterial = BulletMaterial();
            trail.time = time;
            trail.minVertexDistance = 0.05f;
            trail.widthMultiplier = width;
            trail.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
            trail.numCapVertices = 4;
            trail.alignment = LineAlignment.View;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.startColor = color;
            trail.endColor = color;
            SetColor(trail, color);
            return trail;
        }

        public static NetBullet CreateVisual(Vector3 pos, float rotY, bool emitTrail = true)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "NetBullet";
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
            go.transform.localScale = Vector3.one * Size;

            Collider col = go.GetComponent<Collider>();
            if (col != null)
                Destroy(col);

            MeshRenderer body = go.GetComponent<MeshRenderer>();
            body.sharedMaterial = BulletMaterial();
            body.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            SetColor(body, CoreColor);

            GameObject core = new GameObject("Trail Core");
            core.transform.SetParent(go.transform, false);

            NetBullet bullet = go.AddComponent<NetBullet>();
            bullet.type = TypeId;
            bullet.m_serverPos = pos;
            bullet.m_trails = new[]
            {
                AddTrail(go, 0.16f, 0.18f, GoldColor),
                AddTrail(core, 0.06f, 0.10f, CoreColor)
            };
            bullet.SetTrailsEmitting(emitTrail);
            return bullet;
        }

        void SetTrailsEmitting(bool emitting)
        {
            foreach (TrailRenderer trail in m_trails)
            {
                trail.Clear();
                trail.emitting = emitting;
            }
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
                Vector3 predicted = m_serverPos + transform.forward * Speed * (Time.time - m_lastServerUpdate);
                transform.position = Vector3.Lerp(transform.position, predicted, Mathf.Clamp01(Time.deltaTime * 20f));
            }

            if (Time.time - m_lastServerUpdate > Lifetime + 2f)
            {
                replicaLinking.Unregister(id);
                Destroy(gameObject);
            }
        }

        public override void Read(BinaryReader reader)
        {
            Vector3 serverPos = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            float rotY = reader.ReadSingle();

            if (replicaLinking == null)
                return;

            transform.rotation = Quaternion.Euler(0f, rotY, 0f);
            m_lastServerUpdate = Time.time;
            m_serverPos = serverPos;

            if (!m_hasServerPos)
            {
                transform.position = serverPos;
                SetTrailsEmitting(true);
            }
            else if ((transform.position - serverPos).sqrMagnitude > 9f)
            {
                transform.position = serverPos;
            }

            m_hasServerPos = true;
        }
    }
}
