using System;
using SimpleSummon.Application;
using SimpleSummon.Domain;
using SimpleSummon.Network;
using UnityEngine;

namespace SimpleSummon.Runtime
{
    internal sealed class PlayerVitalController
    {
        private readonly UnitModel model;
        private readonly NetworkPlayer networkPlayer;
        private readonly PlayerLocomotion locomotion;
        private readonly PlayerPresentation presentation;
        private readonly LocalPlayerPresentation localPresentation;
        private readonly Transform spawnPoint;
        private readonly Vector3 fallbackPosition;
        private readonly Quaternion fallbackRotation;
        private bool replicatedDead;
        private bool replicatedStateInitialized;

        public PlayerVitalController(
            UnitModel model,
            NetworkPlayer networkPlayer,
            PlayerLocomotion locomotion,
            PlayerPresentation presentation,
            LocalPlayerPresentation localPresentation,
            Transform spawnPoint,
            Vector3 fallbackPosition,
            Quaternion fallbackRotation)
        {
            this.model = model;
            this.networkPlayer = networkPlayer;
            this.locomotion = locomotion;
            this.presentation = presentation;
            this.localPresentation = localPresentation;
            this.spawnPoint = spawnPoint;
            this.fallbackPosition = fallbackPosition;
            this.fallbackRotation = fallbackRotation;
        }

        public event Action Respawned;
        public event Action<float, float> Changed;

        public void NotifyInitialState() =>
            Changed?.Invoke(model.CurrentHealth, model.MaximumHealth);

        public void Publish()
        {
            networkPlayer?.PublishVitalState(model.CurrentHealth, model.IsDead);
        }

        public void TakeDamage(float damage)
        {
            if (model.IsDead)
            {
                return;
            }

            PlayerVitalService.TakeDamage(model, damage);
            Changed?.Invoke(model.CurrentHealth, model.MaximumHealth);
            networkPlayer?.PublishDamage();
            Publish();
            presentation.PlayDamage();
            localPresentation.PlayDamage();
            if (!model.IsDead)
            {
                return;
            }

            locomotion.Stop();
            presentation.StopMovement();
            presentation.PlayDeath();
        }

        public void CompleteDeathAnimation()
        {
            if (!model.IsDead || networkPlayer != null && !networkPlayer.CanRunSimulation)
            {
                return;
            }

            if (spawnPoint != null)
            {
                locomotion.Teleport(spawnPoint.position, spawnPoint.rotation);
            }
            else
            {
                locomotion.Teleport(fallbackPosition, fallbackRotation);
            }

            presentation.StopMovement();
            PlayerVitalService.Restore(model);
            Changed?.Invoke(model.CurrentHealth, model.MaximumHealth);
            Publish();
            presentation.PlayRespawn();
            Respawned?.Invoke();
        }

        public void ApplyReplicatedState(float currentHealth, bool isDead)
        {
            if (networkPlayer == null || networkPlayer.CanRunSimulation)
            {
                return;
            }

            PlayerVitalService.ApplyReplicatedHealth(model, currentHealth);
            Changed?.Invoke(model.CurrentHealth, model.MaximumHealth);
            if (!replicatedStateInitialized || replicatedDead != isDead)
            {
                if (isDead)
                {
                    presentation.PlayDeath();
                }
                else if (replicatedStateInitialized)
                {
                    presentation.PlayRespawn();
                }
            }

            replicatedDead = isDead;
            replicatedStateInitialized = true;
        }

        public void ApplyReplicatedDamage()
        {
            if (networkPlayer == null || networkPlayer.CanRunSimulation)
            {
                return;
            }

            presentation.PlayDamage();
            if (networkPlayer.CanReadLocalInput)
            {
                localPresentation.PlayDamage();
            }
        }
    }
}
