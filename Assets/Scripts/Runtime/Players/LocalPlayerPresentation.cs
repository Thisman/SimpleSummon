using UnityEngine;

namespace SimpleSummon.Runtime
{
    internal sealed class LocalPlayerPresentation
    {
        private readonly Transform cameraTransform;
        private readonly OrbitCameraController orbitCamera;
        private readonly DamageVignette vignette;

        public LocalPlayerPresentation(
            Transform cameraTransform,
            float vignetteDuration,
            float vignetteOpacity)
        {
            this.cameraTransform = cameraTransform;
            orbitCamera = cameraTransform.GetComponent<OrbitCameraController>();
            vignette = new DamageVignette(vignetteDuration, vignetteOpacity);
        }

        public void Tick(float deltaTime) => vignette.Tick(deltaTime);
        public void Draw() => vignette.Draw();
        public void SetCameraActive(bool active) => cameraTransform.gameObject.SetActive(active);

        public void PlayDamage()
        {
            orbitCamera.PlayDamageShake();
            vignette.Play();
        }

        public void Dispose() => vignette.Dispose();
    }
}
