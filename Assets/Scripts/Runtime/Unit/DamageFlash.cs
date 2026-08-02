using System.Collections;
using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class DamageFlash : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private Renderer[] renderers;
        [SerializeField, Min(0f)] private float duration = 0.12f;

        private MaterialPropertyBlock[] originalPropertyBlocks;
        private Coroutine flashRoutine;

        public Renderer[] Renderers => renderers;

        private void Awake()
        {
            originalPropertyBlocks = new MaterialPropertyBlock[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderers[i].GetPropertyBlock(block);
                originalPropertyBlocks[i] = block;
            }
        }

        private void OnDisable()
        {
            if (originalPropertyBlocks != null)
            {
                RestoreColors();
            }
        }

        public void Play()
        {
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
            }

            flashRoutine = StartCoroutine(Flash());
        }

        private IEnumerator Flash()
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderers[i].GetPropertyBlock(block);
                block.SetColor(BaseColorId, Color.red);
                block.SetColor(ColorId, Color.red);
                renderers[i].SetPropertyBlock(block);
            }

            yield return new WaitForSeconds(duration);

            RestoreColors();
            flashRoutine = null;
        }

        private void RestoreColors()
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].SetPropertyBlock(originalPropertyBlocks[i]);
            }
        }
    }
}
