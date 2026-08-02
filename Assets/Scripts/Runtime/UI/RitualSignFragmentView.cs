using SimpleSummon.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSummon.Runtime
{
    public sealed class RitualSignFragmentView : MonoBehaviour
    {
        [SerializeField] private Texture2D signTexture;
        [SerializeField] private Image[] fragmentImages;

        private Sprite[] fragmentSprites;

        private void Awake()
        {
            CreateFragmentSprites();
            HideAll();
        }

        private void OnDestroy()
        {
            if (fragmentSprites == null)
            {
                return;
            }

            for (int i = 0; i < fragmentSprites.Length; i++)
            {
                Destroy(fragmentSprites[i]);
            }
        }

        public void ShowOpened(ushort occupiedMask)
        {
            EnsureInitialized();
            for (int i = 0; i < fragmentImages.Length; i++)
            {
                Image image = fragmentImages[i];
                image.sprite = fragmentSprites[i];
                image.enabled = (occupiedMask & (1 << i)) != 0;
            }
        }

        public void ShowScrambled(int[] fragmentIndices)
        {
            EnsureInitialized();
            for (int i = 0; i < fragmentImages.Length; i++)
            {
                Image image = fragmentImages[i];
                image.sprite = fragmentSprites[fragmentIndices[i]];
                image.enabled = true;
            }
        }

        private void HideAll()
        {
            for (int i = 0; i < fragmentImages.Length; i++)
            {
                fragmentImages[i].enabled = false;
            }
        }

        private void EnsureInitialized()
        {
            if (fragmentSprites == null)
            {
                CreateFragmentSprites();
            }
        }

        private void CreateFragmentSprites()
        {
            if (fragmentSprites != null || signTexture == null ||
                fragmentImages == null ||
                fragmentImages.Length != RitualSignPlateState.PlateCount)
            {
                return;
            }

            fragmentSprites = new Sprite[RitualSignPlateState.PlateCount];
            float cellWidth = signTexture.width / 3f;
            float cellHeight = signTexture.height / 3f;
            for (int i = 0; i < fragmentSprites.Length; i++)
            {
                int column = i % 3;
                int row = i / 3;
                Rect rect = new(
                    column * cellWidth,
                    signTexture.height - (row + 1) * cellHeight,
                    cellWidth,
                    cellHeight);
                fragmentSprites[i] = Sprite.Create(
                    signTexture,
                    rect,
                    new Vector2(0.5f, 0.5f),
                    100f,
                    0,
                    SpriteMeshType.FullRect);
            }

            for (int i = 0; i < fragmentImages.Length; i++)
            {
                fragmentImages[i].color = Color.white;
                fragmentImages[i].raycastTarget = false;
                fragmentImages[i].type = Image.Type.Simple;
                fragmentImages[i].preserveAspect = false;
            }
        }
    }
}
