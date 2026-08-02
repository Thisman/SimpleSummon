using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class TorchLightFlicker : MonoBehaviour
    {
        [SerializeField] private Light torchLight;
        [SerializeField] private Vector2 intensityRange = new(0.7f, 1.15f);
        [SerializeField] private Vector2 transitionDurationRange = new(0.08f, 0.2f);
        [SerializeField] private Vector2 randomStartDelayRange = new(0f, 1f);

        private float startIntensity;
        private float targetIntensity;
        private float transitionDuration;
        private float transitionElapsed;
        private float startDelayRemaining;

        private void OnEnable()
        {
            startIntensity = torchLight.intensity;
            targetIntensity = Random.Range(intensityRange.x, intensityRange.y);
            transitionDuration = Random.Range(transitionDurationRange.x, transitionDurationRange.y);
            transitionElapsed = 0f;
            startDelayRemaining = Random.Range(randomStartDelayRange.x, randomStartDelayRange.y);
        }

        private void Update()
        {
            if (startDelayRemaining > 0f)
            {
                startDelayRemaining -= Time.deltaTime;
                return;
            }

            transitionElapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(transitionElapsed / transitionDuration);
            torchLight.intensity = Mathf.SmoothStep(startIntensity, targetIntensity, progress);

            if (progress < 1f)
            {
                return;
            }

            startIntensity = targetIntensity;
            targetIntensity = Random.Range(intensityRange.x, intensityRange.y);
            transitionDuration = Random.Range(transitionDurationRange.x, transitionDurationRange.y);
            transitionElapsed = 0f;
        }
    }
}
