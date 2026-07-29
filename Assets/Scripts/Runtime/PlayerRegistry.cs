using System.Collections.Generic;
using UnityEngine;

namespace SimpleSummon.Runtime
{
    public static class PlayerRegistry
    {
        private static readonly List<PlayerController> players = new();

        public static void Register(PlayerController player)
        {
            if (!players.Contains(player))
            {
                players.Add(player);
            }
        }

        public static void Unregister(PlayerController player)
        {
            players.Remove(player);
        }

        public static PlayerController GetClosestLiving(Vector3 position)
        {
            PlayerController closest = null;
            float closestSqrDistance = float.PositiveInfinity;

            foreach (PlayerController player in players)
            {
                if (player == null || player.IsDead)
                {
                    continue;
                }

                float sqrDistance = (player.transform.position - position).sqrMagnitude;
                if (sqrDistance < closestSqrDistance)
                {
                    closest = player;
                    closestSqrDistance = sqrDistance;
                }
            }

            return closest;
        }
    }
}
