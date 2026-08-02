using System;
using SimpleSummon.Domain;

namespace SimpleSummon.Application
{
    public sealed class TorchService
    {
        private readonly TorchModel model;

        public TorchService(TorchModel model)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public float Strength => model.Strength;
        public bool IsExtinguished => model.IsExtinguished;
        public bool IsAvailable => model.IsAvailable;

        public bool TryTake(ulong holderId) => model.TryTake(holderId);
        public bool IsHeldBy(ulong holderId) => model.IsHeldBy(holderId);

        public void Update(bool isMoving, float deltaTime) =>
            model.Tick(isMoving, deltaTime);

        public void Release() => model.Release();
    }
}
