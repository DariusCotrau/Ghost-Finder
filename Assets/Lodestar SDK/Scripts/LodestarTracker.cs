using UnityEngine;

namespace LodestarRuntime
{

    public class TrackedPose
    {
        public float confidence;

        public Vector3 position;
        public Quaternion rotation;

        public Vector3 linearVelocity;
        public Vector3 angularVelocity;
    }

    public class LodestarTracker : MonoBehaviour
    {
        [HideInInspector]
        public enum Status
        {
            Disconnected = 0,
            Off = 1,
            Pairing = 2,
            Ready = 3,
            Active = 4,
            Tracking = 5,
            Error = 6
        }

        [Header("Tracker Info")]
        [ReadOnly]
        public int id;

        [ReadOnly]
        public int battery;

        [ReadOnly]
        public Status status;

        [ReadOnly]
        public bool tracking;

        [HideInInspector]
        public TrackedPose pose = new TrackedPose();

        private bool renderMesh;
        private MeshRenderer mesh;

        private void Start()
        {
            mesh = GetComponent<MeshRenderer>();
        }


        private void Update()
        {
            if (mesh) mesh.enabled = renderMesh;

            if (status >= Status.Active)
            {
                transform.localPosition = pose.position;
                transform.localRotation = pose.rotation;
            }
            else
            {
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            }
        }

        public void setRenderTracker(bool render)
        {
            renderMesh = render;
        }
    }

}
