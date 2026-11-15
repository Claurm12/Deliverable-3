using System.Collections.Generic;
using UnityEngine;

namespace Delivery3
{
    public class LinkingContext : MonoBehaviour
    {
        private readonly Dictionary<int, GameObject> m_registry = new Dictionary<int, GameObject>();
        private int m_nextId = 1;

        public IEnumerable<int> Ids { get { return m_registry.Keys; } }
        public int Count { get { return m_registry.Count; } }

        public void Register(GameObject obj, int id)
        {
            m_registry[id] = obj;
        }

        public int Register(GameObject obj)
        {
            int id = m_nextId++;
            m_registry[id] = obj;
            return id;
        }

        public void Unregister(int id)
        {
            m_registry.Remove(id);
        }

        public GameObject Find(int id)
        {
            m_registry.TryGetValue(id, out GameObject obj);
            return obj;
        }
    }
}