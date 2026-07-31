using System;
using SimpleSummon.Domain;
using SimpleSummon.Network;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSummon.Runtime
{
    public sealed class SignPuzzleView : MonoBehaviour
    {
        [SerializeField] private Image[] cells;
        private readonly Sprite[] sprites = new Sprite[SignPuzzleState.FragmentCount];
        private NetworkSignPuzzle puzzle;
        private Texture2D[] textures;
        private int activeVariant = -1;

        private void Awake()
        {
            EnsureTexturesLoaded();
            SetVisible(false);
        }

        private void OnDestroy() { SetPuzzle(null); DestroySprites(); }

        public void SetPuzzle(NetworkSignPuzzle value)
        {
            if (puzzle != null) puzzle.BoardChanged -= Refresh;
            puzzle = value;
            if (puzzle != null) { puzzle.BoardChanged += Refresh; Refresh(); }
            else SetVisible(false);
        }

        public bool TryGetSlot(Vector2 screenPoint, out int slot)
        {
            Camera camera = GetEventCamera();
            for (int i = 0; i < cells.Length; i++)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(cells[i].rectTransform, screenPoint, camera))
                { slot = i; return true; }
            }
            slot = -1;
            return false;
        }

        private void Refresh()
        {
            if (puzzle == null || cells.Length != SignPuzzleState.SlotCount) return;
            EnsureVariant(puzzle.SignVariant);
            for (int i = 0; i < cells.Length; i++)
            {
                byte id = puzzle.GetSlot(i);
                bool visible = id < SignPuzzleState.FragmentCount;
                cells[i].enabled = visible;
                cells[i].color = Color.white;
                cells[i].sprite = visible ? sprites[id] : null;
            }
        }

        private void EnsureVariant(int variant)
        {
            EnsureTexturesLoaded();
            if (activeVariant == variant || textures.Length == 0) return;
            activeVariant = variant;
            DestroySprites();
            Texture2D texture = textures[variant % textures.Length];
            float width = texture.width / 3f;
            float height = texture.height / 3f;
            for (int id = 0; id < sprites.Length; id++)
            {
                int column = id % 3;
                int row = id / 3;
                sprites[id] = Sprite.Create(texture, new Rect(column * width, (2 - row) * height, width, height),
                    new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            }
        }

        private void EnsureTexturesLoaded()
        {
            if (textures != null) return;
            textures = Resources.LoadAll<Texture2D>("SignPuzzles");
            Array.Sort(textures, (a, b) => string.CompareOrdinal(a.name, b.name));
        }

        private void DestroySprites()
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] == null) continue;
                Destroy(sprites[i]);
                sprites[i] = null;
            }
        }

        private void SetVisible(bool visible)
        {
            if (cells == null) return;
            for (int i = 0; i < cells.Length; i++) cells[i].enabled = visible;
        }

        private Camera GetEventCamera()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            return canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }
    }
}
