using UnityEngine;

namespace SimpleSummon.Runtime
{
    public class DebugController : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        public void Awake()
        {
            gameObject.SetActive(false);
        }
    }
}
