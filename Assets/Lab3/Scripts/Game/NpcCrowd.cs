using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Delivery3
{
    public class NpcCrowd : MonoBehaviour
    {
        [Min(1)] public int count = 25;
        [Min(1f)] public float spawnRadius = 35f;
        [Min(1f)] public float wanderRadius = 10f;
        [Min(0.1f)] public float speed = 6f;
        public float modelHeightOffset = 0.5f;
        [Min(0.1f)] public float minJumpDelay = 8f;
        [Min(0.1f)] public float maxJumpDelay = 20f;

        const float JumpForce = 8f;
        const float Gravity = -20f;

        class Npc
        {
            public NetPlayer player;
            public Vector3 groundPosition;
            public Vector3[] corners;
            public int corner;
            public float changeAt;
            public float nextJumpAt;
            public float jumpHeight;
            public float verticalVelocity;
            public bool jumping;
        }

        readonly List<Npc> m_npcs = new List<Npc>();
        LobbyServer m_server;
        bool m_frozen;

        public void Spawn(
            LobbyServer server,
            LinkingContext linking,
            GameObject[] prefabs,
            ReplicationManagerServer replication)
        {
            m_server = server;

            if (prefabs == null || prefabs.Length < 2)
            {
                Debug.LogError(
                    "[NPC] Hey, add a chased prefab at index 1 or higher!");
                return;
            }

            List<int> types = new List<int>();

            for (int i = 1; i < prefabs.Length && i < NetBullet.TypeId; i++)
            {
                if (prefabs[i] != null &&
                    prefabs[i].GetComponent<NetPlayer>() != null)
                {
                    types.Add(i);
                }
            }

            if (types.Count == 0)
            {
                Debug.LogError(
                    "[NPC] Couldn't find a chased prefab with NetPlayer.");
                return;
            }

            for (int i = 0; i < count; i++)
            {
                Vector3 position;

                if (!TrySample(transform.position, spawnRadius, out position))
                {
                    Debug.LogError(
                        "[NPC] Couldn't find a spawn spot. " +
                        "Check the NavMesh and crowd radius!");
                    break;
                }

                int type = types[Random.Range(0, types.Count)];

                GameObject go = Instantiate(
                    prefabs[type],
                    position + Vector3.up * modelHeightOffset,
                    Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));

                go.name = "NPC";

                PlayerController controller = go.GetComponent<PlayerController>();
                if (controller != null)
                    controller.enabled = false;

                Rigidbody body = go.GetComponent<Rigidbody>();
                if (body != null)
                    body.isKinematic = true;

                Animator animator = go.GetComponentInChildren<Animator>();
                if (animator != null)
                    animator.applyRootMotion = false;

                NetPlayer player = go.GetComponent<NetPlayer>();
                player.type = (byte)type;
                player.id = linking.Register(go);
                player.playerRole = "Chased";
                player.isMoving = false;
                player.ApplyAnimationState();

                m_npcs.Add(new Npc
                {
                    player = player,
                    groundPosition = position,
                    changeAt = Time.time + Random.Range(0.2f, 2f),
                    nextJumpAt = Time.time + JumpDelay()
                });

                replication.MarkCreate(player.id);
            }

            Debug.Log(
                "[NPC] Spawned " + m_npcs.Count + " out of " + count + ".");
        }

        public IEnumerable<NetPlayer> Players
        {
            get
            {
                foreach (Npc npc in m_npcs)
                {
                    if (npc.player != null)
                        yield return npc.player;
                }
            }
        }

        void Update()
        {
            if (m_server == null || !m_server.Started)
                return;

            if (m_server.MatchEnded)
            {
                if (!m_frozen)
                {
                    foreach (Npc npc in m_npcs)
                    {
                        if (npc.player == null)
                            continue;

                        npc.jumpHeight = 0f;
                        npc.verticalVelocity = 0f;
                        npc.jumping = false;
                        npc.player.isMoving = false;

                        npc.player.transform.position =
                            npc.groundPosition +
                            Vector3.up * modelHeightOffset;

                        npc.player.ApplyAnimationState();
                    }

                    m_frozen = true;
                }

                return;
            }

            foreach (Npc npc in m_npcs)
            {
                if (npc.player == null)
                    continue;

                UpdateWalking(npc);
                UpdateJump(npc);

                npc.player.transform.position =
                    npc.groundPosition +
                    Vector3.up * (modelHeightOffset + npc.jumpHeight);
            }
        }

        void UpdateWalking(Npc npc)
        {
            if (Time.time >= npc.changeAt)
            {
                if (npc.player.isMoving)
                {
                    SetMoving(npc, false);
                    npc.changeAt = Time.time + Random.Range(0.8f, 3f);
                }
                else if (!StartWalking(npc))
                {
                    npc.changeAt = Time.time + Random.Range(0.8f, 3f);
                }
            }

            if (!npc.player.isMoving)
                return;

            Vector3 destination = npc.corners[npc.corner];
            Vector3 delta = destination - npc.groundPosition;
            Vector3 flat = new Vector3(delta.x, 0f, delta.z);

            if (flat.sqrMagnitude > 0.001f)
            {
                npc.player.transform.rotation = Quaternion.RotateTowards(
                    npc.player.transform.rotation,
                    Quaternion.LookRotation(flat),
                    360f * Time.deltaTime);
            }

            npc.groundPosition = Vector3.MoveTowards(
                npc.groundPosition,
                destination,
                speed * Time.deltaTime);

            if ((npc.groundPosition - destination).sqrMagnitude < 0.01f)
            {
                npc.corner++;

                if (npc.corner >= npc.corners.Length)
                {
                    SetMoving(npc, false);
                    npc.changeAt = Time.time + Random.Range(0.8f, 3f);
                }
            }
        }

        void UpdateJump(Npc npc)
        {
            if (!npc.jumping && Time.time >= npc.nextJumpAt)
            {
                npc.verticalVelocity = JumpForce;
                npc.jumping = true;
            }

            if (!npc.jumping)
                return;

            npc.verticalVelocity += Gravity * Time.deltaTime;
            npc.jumpHeight += npc.verticalVelocity * Time.deltaTime;

            if (npc.jumpHeight <= 0f)
            {
                npc.jumpHeight = 0f;
                npc.verticalVelocity = 0f;
                npc.jumping = false;
                npc.nextJumpAt = Time.time + JumpDelay();
            }
        }

        float JumpDelay()
        {
            float min = Mathf.Max(0.1f, minJumpDelay);
            return Random.Range(min, Mathf.Max(min, maxJumpDelay));
        }

        bool StartWalking(Npc npc)
        {
            NavMeshPath path = new NavMeshPath();

            for (int i = 0; i < 10; i++)
            {
                Vector3 destination;

                if (!TrySample(
                        npc.groundPosition,
                        wanderRadius,
                        out destination))
                {
                    continue;
                }

                Vector3 offset = destination - transform.position;
                offset.y = 0f;

                if (offset.sqrMagnitude > spawnRadius * spawnRadius)
                    continue;

                if (!NavMesh.CalculatePath(
                        npc.groundPosition,
                        destination,
                        NavMesh.AllAreas,
                        path) ||
                    path.status != NavMeshPathStatus.PathComplete ||
                    path.corners.Length < 2)
                {
                    continue;
                }

                npc.corners = path.corners;
                npc.corner = 1;
                npc.changeAt = Time.time + Random.Range(2f, 5f);
                SetMoving(npc, true);
                return true;
            }

            return false;
        }

        static bool TrySample(
            Vector3 center,
            float radius,
            out Vector3 position)
        {
            for (int attempt = 0; attempt < 40; attempt++)
            {
                Vector2 offset = Random.insideUnitCircle * radius;
                Vector3 candidate =
                    center + new Vector3(offset.x, 0f, offset.y);

                NavMeshHit hit;

                if (NavMesh.SamplePosition(
                        candidate,
                        out hit,
                        2f,
                        NavMesh.AllAreas))
                {
                    position = hit.position;
                    return true;
                }
            }

            position = Vector3.zero;
            return false;
        }

        static void SetMoving(Npc npc, bool moving)
        {
            npc.player.isMoving = moving;
            npc.player.ApplyAnimationState();
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
        }
    }
}