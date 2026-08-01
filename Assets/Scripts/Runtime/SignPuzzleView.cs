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
        [SerializeField] private Text progressHint;
        [SerializeField] private Color selectedColor = new(1f, 0.85f, 0.35f, 1f);
        private readonly Sprite[] sprites = new Sprite[SignPuzzleState.FragmentCount];
        private NetworkSignPuzzle puzzle;
        private Texture2D[] textures;
        private int activeVariant = -1;
        private byte selectedFragment = SignPuzzleState.Empty;

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

        public void SetSelectedFragment(byte fragment)
        {
            selectedFragment = fragment;
            Refresh();
        }

        private void Refresh()
        {
            if (puzzle == null || cells.Length != SignPuzzleState.SlotCount) return;
            EnsureVariant(puzzle.SignVariant);
            int fragmentCount = 0;
            for (int i = 0; i < cells.Length; i++)
            {
                byte id = puzzle.GetSlot(i);
                bool visible = id < SignPuzzleState.FragmentCount;
                if (visible) fragmentCount++;
                cells[i].enabled = visible;
                cells[i].color = id == selectedFragment ? selectedColor : Color.white;
                cells[i].sprite = visible ? sprites[id] : null;
            }
            if (progressHint != null)
                progressHint.text = GameLocalization.GetSignPuzzleProgress(fragmentCount, SignPuzzleState.FragmentCount);
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
