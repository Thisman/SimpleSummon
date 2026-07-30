using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class InstructionInteraction : MonoBehaviour, IInteractable
    {
        [SerializeField] private Vector3 cameraPosition;
        [SerializeField] private Vector3 cameraEulerAngles;
        [SerializeField] private Vector3 cameraScale = Vector3.one;

        public Vector3 CameraPosition => cameraPosition;
        public Quaternion CameraRotation => Quaternion.Euler(cameraEulerAngles);
        public Vector3 CameraScale => cameraScale;

        public void Interact(GameObject interactor)
        {
            if (interactor.TryGetComponent(out InstructionModeController instructionMode))
            {
                instructionMode.Enter(this);
            }
        }
    }
}
