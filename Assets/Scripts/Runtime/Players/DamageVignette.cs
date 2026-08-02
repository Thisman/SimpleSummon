using UnityEngine;

namespace SimpleSummon.Runtime
{
    internal sealed class DamageVignette
    {
        private readonly float duration;
        private readonly float opacity;
        private readonly Texture2D texture;
        private float remainingTime;

        public DamageVignette(float duration, float opacity)
        {
            this.duration = duration;
            this.opacity = opacity;
            texture = CreateTexture();
        }

        public void Play() => remainingTime = duration;

        public void Tick(float deltaTime)
        {
            remainingTime = Mathf.Max(0f, remainingTime - deltaTime);
        }

        public void Draw()
        {
            if (remainingTime <= 0f || duration <= 0f)
            {
                return;
            }

            float normalizedTime = remainingTime / duration;
            float pulse = Mathf.Sin(normalizedTime * Mathf.PI * 0.5f);
            GUI.color = new Color(1f, 1f, 1f, pulse * opacity);
            GUI.DrawTexture(
                new Rect(0f, 0f, Screen.width, Screen.height),
                texture,
                ScaleMode.StretchToFill);
            GUI.color = Color.white;
        }

        public void Dispose()
        {
            Object.Destroy(texture);
        }

        private static Texture2D CreateTexture()
        {
            const int textureSize = 128;
            Texture2D result = new Texture2D(
                textureSize,
                textureSize,
                TextureFormat.RGBA32,
                false);
            Color[] pixels = new Color[textureSize * textureSize];

            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    float normalizedX = (x + 0.5f) / textureSize * 2f - 1f;
                    float normalizedY = (y + 0.5f) / textureSize * 2f - 1f;
                    float distance = Mathf.Max(Mathf.Abs(normalizedX), Mathf.Abs(normalizedY));
                    float alpha = Mathf.SmoothStep(
                        0f,
                        1f,
                        Mathf.InverseLerp(0.82f, 1f, distance));
                    pixels[y * textureSize + x] = new Color(0.75f, 0f, 0f, alpha);
                }
            }

            result.SetPixels(pixels);
            result.Apply(false, true);
            return result;
        }
    }
}
