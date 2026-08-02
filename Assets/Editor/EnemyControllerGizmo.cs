using SimpleSummon.Runtime;
using UnityEditor;
using UnityEngine;

namespace SimpleSummon.Editor
{
    public static class EnemyControllerGizmo
    {
        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        private static void Draw(EnemyController controller, GizmoType _)
        {
            EnemySettings settings = controller.GetComponent<EnemySettings>();
            if (settings == null)
            {
                return;
            }

            Vector3 origin = controller.transform.position;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origin, settings.DetectionRadius);
            Gizmos.color = Color.red;
            float scale = Mathf.Max(
                controller.transform.lossyScale.x,
                controller.transform.lossyScale.z);
            Gizmos.DrawWireSphere(origin, settings.AttackRadius * scale);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(origin, settings.ReturnRadius);
        }
    }
}
