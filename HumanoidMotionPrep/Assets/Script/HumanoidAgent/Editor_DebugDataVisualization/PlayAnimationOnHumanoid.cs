using System.Collections.Generic;
using System.Text;
using UnityEngine;
using gianmarcolelli.AgentBehavior;

#if UNITY_EDITOR
namespace gianmarcolelli.AnimVisualization
{
    [RequireComponent(typeof(HumanoidCloningManager))]
    public class PlayAnimationOnHumanoid : MonoBehaviour
    {
        private SkeletonMapper mapper;
        private HumanoidCloningManager bc_manager;
        private Quaternion[] currentPose;
        private List<string> structureOrder;

        // Sincronizzazione dell'animazione
        private float animationFPS;
        [HideInInspector]
        public float frameTime;
        private float timeSinceLastFrame = 0f;

        // Debugging
        [SerializeField] bool debugAnimInfo = true;

        void Awake()
        {
            // Controllo del componente HumanoidCloningManager
            bc_manager = GetComponent<HumanoidCloningManager>();
            if (bc_manager == null)
            {
                Debug.LogWarning($"[{gameObject.name}] Missing reference to 'HumanoidCloningManager' component in {nameof(PlayAnimationOnHumanoid)}.");
            }

            // Controllo del componente SkeletonMapper
            mapper = GetComponent<SkeletonMapper>();
            if (mapper == null)
            {
                Debug.LogWarning($"[{gameObject.name}] Missing reference to 'SkeletonMapper' component in {nameof(PlayAnimationOnHumanoid)}.");
            }
        }

        void Start()
        {
            bc_manager.LoadAnimation();

            animationFPS = bc_manager.GetFrameRate();
            frameTime = 1.0f / animationFPS;

            structureOrder = bc_manager.humStructureOrder;
            if (debugAnimInfo) { DebugAnimationInfo(structureOrder, bc_manager.animationRot); }
            currentPose = new Quaternion[mapper.bodyPart.Count];
        }

        void Update()
        {
            timeSinceLastFrame += Time.deltaTime;
            if (timeSinceLastFrame >= frameTime)
            {
                timeSinceLastFrame -= frameTime;
                //Debug.Log("Frame corrente" + bc_manager.currentFrame);
                bc_manager.GetCurrentDataJointRotation(currentPose);
                for (int i = 0; i < currentPose.Length; i++)
                {
                    //Debug.Log($"{vs.bodyPart[i].name} = {structureOrder[i]}");
                    mapper.bodyPart[i].localRotation = currentPose[i];
                }
                bc_manager.currentFrame++;
                if (bc_manager.currentFrame >= bc_manager.GetAnimationLength()) { bc_manager.currentFrame = 0; }
            }
        }

        void DebugAnimationInfo(List<string> humStructureOrder, Quaternion[,] animationRot)
        {
            StringBuilder sb = new StringBuilder();

            // Print dimensions
            sb.AppendLine($"Humanoid Structure Count: {humStructureOrder.Count}");
            sb.AppendLine($"Animation Rotations: {animationRot.GetLength(0)} frames x {animationRot.GetLength(1)} joints");

            sb.AppendLine("\n==== HUMANOID STRUCTURE ORDER ====");
            for (int i = 0; i < humStructureOrder.Count; i++)
            {
                sb.AppendLine($"[{i}] {humStructureOrder[i]}");
            }

            sb.AppendLine("\n==== ANIMATION ROTATIONS ====");

            // Create table header
            sb.Append("Frame \\ Joint\t");
            for (int j = 0; j < animationRot.GetLength(1); j++)
            {
                sb.Append($"J{j}\t");
            }
            sb.AppendLine();

            // Print rotation values in a table-like format
            for (int i = 0; i < animationRot.GetLength(0); i++) // Frames
            {
                sb.Append($"Frame {i}\t");
                for (int j = 0; j < animationRot.GetLength(1); j++) // Joints
                {
                    Quaternion q = animationRot[i, j];
                    sb.Append($"({q.x:F2}, {q.y:F2}, {q.z:F2}, {q.w:F2})\t");
                }
                sb.AppendLine();
            }
            Debug.Log(sb.ToString());
        }
    }
}
#endif
