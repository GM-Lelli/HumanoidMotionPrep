using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
namespace gianmarcolelli.AnimVisualization
{
    public class SkeletonMapper : MonoBehaviour
    {
        [Header("Body Parts")]
        public Transform pelvis;
        public Transform l_hip;
        public Transform l_knee;
        public Transform l_ankle;
        public Transform l_foot;
        public Transform r_hip;
        public Transform r_knee;
        public Transform r_ankle;
        public Transform r_foot;
        public Transform spine;
        public Transform spine1;
        public Transform spine2;
        public Transform l_collar;
        public Transform l_shoulder;
        public Transform l_elbow;
        public Transform l_wrist;
        public Transform neck;
        public Transform head;
        public Transform r_collar;
        public Transform r_shoulder;
        public Transform r_elbow;
        public Transform r_wrist;

        [HideInInspector]
        public List<Transform> bodyPart;

        protected virtual void Start()
        {
            bodyPart = new List<Transform>() {
                pelvis,
                l_hip, l_knee, l_ankle, l_foot,
                r_hip, r_knee, r_ankle, r_foot,
                spine, spine1, spine2,
                neck, head,
                l_collar, l_shoulder, l_elbow, l_wrist,
                r_collar, r_shoulder, r_elbow, r_wrist,
            };
        }
    }
}
#endif