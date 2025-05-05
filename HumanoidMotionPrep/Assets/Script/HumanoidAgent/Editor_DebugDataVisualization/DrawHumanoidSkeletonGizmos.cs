using UnityEngine;

#if UNITY_EDITOR
namespace gianmarcolelli.AnimVisualization
{
    public class DrawHumanoidSkeletonGizmos : MonoBehaviour
    {
        private SkeletonMapper mapper;

        void Awake()
        {
            // Controllo del componente SkeletonMapper
            mapper = GetComponent<SkeletonMapper>();
            if (mapper == null)
            {
                Debug.LogWarning($"[{gameObject.name}] Missing reference to 'SkeletonMapper' component in {nameof(DrawHumanoidSkeletonGizmos)}.");
            }
        }

        private void OnDrawGizmos()
        {
            if (mapper == null) { return; }

            Gizmos.color = Color.red;

            if (mapper.GetType() == typeof(SkeletonMapper))
            {
                DrawOriginalStructure();
            }
            else
            {
                Debug.LogWarning("mapper è di un tipo non previsto.");
            }
        }

        private void DrawOriginalStructure()
        {
            // Spine Structure
            DrawSegment(mapper.pelvis.position, mapper.spine.position);
            DrawSegment(mapper.spine.position, mapper.spine1.position);
            DrawSegment(mapper.spine1.position, mapper.spine2.position);
            DrawSegment(mapper.spine2.position, mapper.neck.position);
            DrawSegment(mapper.neck.position, mapper.head.position);

            // Left Leg
            DrawSegment(mapper.pelvis.position, mapper.l_hip.position);
            DrawSegment(mapper.l_hip.position, mapper.l_knee.position);
            DrawSegment(mapper.l_knee.position, mapper.l_ankle.position);
            DrawSegment(mapper.l_ankle.position, mapper.l_foot.position);

            // Right Leg
            DrawSegment(mapper.pelvis.position, mapper.r_hip.position);
            DrawSegment(mapper.r_hip.position, mapper.r_knee.position);
            DrawSegment(mapper.r_knee.position, mapper.r_ankle.position);
            DrawSegment(mapper.r_ankle.position, mapper.r_foot.position);

            // Left Arm
            DrawSegment(mapper.spine2.position, mapper.l_collar.position);
            DrawSegment(mapper.l_collar.position, mapper.l_shoulder.position);
            DrawSegment(mapper.l_shoulder.position, mapper.l_elbow.position);
            DrawSegment(mapper.l_elbow.position, mapper.l_wrist.position);

            // Right Arm
            DrawSegment(mapper.spine2.position, mapper.r_collar.position);
            DrawSegment(mapper.r_collar.position, mapper.r_shoulder.position);
            DrawSegment(mapper.r_shoulder.position, mapper.r_elbow.position);
            DrawSegment(mapper.r_elbow.position, mapper.r_wrist.position);
        }

        private void DrawRagdollStructure()
        {
            // Spine Structure
            DrawSegment(mapper.pelvis.position, mapper.spine1.position);
            DrawSegment(mapper.spine1.position, mapper.head.position);

            // Left Leg
            DrawSegment(mapper.pelvis.position, mapper.l_hip.position);
            DrawSegment(mapper.l_hip.position, mapper.l_knee.position);
            DrawSegment(mapper.l_knee.position, mapper.l_ankle.position);

            // Right Leg
            DrawSegment(mapper.pelvis.position, mapper.r_hip.position);
            DrawSegment(mapper.r_hip.position, mapper.r_knee.position);
            DrawSegment(mapper.r_knee.position, mapper.r_ankle.position);

            // Left Arm
            DrawSegment(mapper.spine1.position, mapper.l_shoulder.position);
            DrawSegment(mapper.l_shoulder.position, mapper.l_elbow.position);
            DrawSegment(mapper.l_elbow.position, mapper.l_wrist.position);

            // Right Arm
            DrawSegment(mapper.spine1.position, mapper.r_shoulder.position);
            DrawSegment(mapper.r_shoulder.position, mapper.r_elbow.position);
            DrawSegment(mapper.r_elbow.position, mapper.r_wrist.position);
        }

        private void DrawSegment(Vector3 parentPos, Vector3 childPos)
        {
            Gizmos.DrawLine(parentPos, childPos);
            Gizmos.DrawSphere(childPos, 0.03f);
        }
    }
}
#endif