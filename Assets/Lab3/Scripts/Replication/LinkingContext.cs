using System.Collections.Generic;
using UnityEngine;

namespace Delivery3
{
    public class LinkingContext : MonoBehaviour
    {
        private readonly Dictionary<int, GameObject> m_registry = new Dictionary<int, GameObject>();

        public void Register(GameObject obj, int id)
        {
            m_registry[id] = obj;
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