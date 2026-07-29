using UnityEngine;
using UnityEngine.Rendering;

namespace SimpleSummon.Runtime
{
    public sealed class RetroVhsEffect : MonoBehaviour
    {
        [SerializeField] private Volume effectVolume;
        [SerializeField] private bool effectEnabled = true;

        public bool EffectEnabled => effectEnabled;

        private void Awake()
        {
            Apply();
        }

        private void OnValidate()
        {
            Apply();
        }

        public void SetEnabled(bool value)
        {
            effectEnabled = value;
            Apply();
        }

        private void Apply()
        {
            if (effectVolume != null)
            {
                effectVolume.enabled = effectEnabled;
            }
        }
    }
}
