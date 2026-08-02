using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class RitualSignPlateController : MonoBehaviour
    {
        [SerializeField, Range(0, 8)] private int plateIndex;
        [SerializeField] private BoxCollider activationVolume;
        [SerializeField] private Transform visualTransform;
        [SerializeField, Min(0f)] private float standingHeightTolerance = 0.35f;
        [SerializeField, Min(0f)] private float pressedOffset = 0.7f;
        [SerializeField, Min(0.01f)] private float movementDuration = 0.15f;

        private Vector3 raisedLocalPosition;
        private bool occupied;

        public int PlateIndex => plateIndex;

        private void Awake()
        {
            raisedLocalPosition = visualTransform.localPosition;
        }

        private void Update()
        {
            Vector3 target = raisedLocalPosition;
            if (occupied)
            {
                target.y -= pressedOffset;
            }

            float speed = pressedOffset / movementDuration;
            visualTransform.localPosition = Vector3.MoveTowards(
                visualTransform.localPosition,
                target,
                speed * Time.deltaTime);
        }

        public bool ContainsStandingPoint(Vector3 worldPosition)
        {
            Bounds bounds = activationVolume.bounds;
            return worldPosition.x >= bounds.min.x && worldPosition.x <= bounds.max.x &&
                   worldPosition.z >= bounds.min.z && worldPosition.z <= bounds.max.z &&
                   Mathf.Abs(worldPosition.y - transform.position.y) <=
                   standingHeightTolerance;
        }

        public float GetSqrDistance(Vector3 worldPosition)
        {
            Vector3 center = activationVolume.bounds.center;
            center.y = worldPosition.y;
            return (center - worldPosition).sqrMagnitude;
        }

        public void SetOccupied(bool value)
        {
            occupied = value;
        }

        private void OnValidate()
        {
            if (activationVolume == null)
            {
                activationVolume = GetComponent<BoxCollider>();
            }
        }
    }
}
