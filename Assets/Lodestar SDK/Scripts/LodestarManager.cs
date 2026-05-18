using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace LodestarRuntime
{

    /// <summary>
    /// Instantiates a tracker object in the scene for every conncted Lodestar tracker
    /// and keeps its transform in sync.
    /// - Tracker objects are created / destroyed on-the-fly as trackers appear / disappear.
    /// - Mapping is deterministic.
    /// </summary>
    /// 
    public class LodestarManager : MonoBehaviour
    {
        [Header("Tracker Settings")]
        [Tooltip("Object to spawn for each tracker. DO NOT CHANGE!")]
        public GameObject trackerObject;

        [Tooltip("Parent under which the trackers will spawn (optional)")]
        public Transform trackerOrigin;

        [Tooltip("Render the tracker mesh")]
        public bool renderTrackers = true;

        [HideInInspector]
        public Dictionary<int, GameObject> trackerRegistry = new();

        private const string LayoutName = "Lodestar Tracker"; // Must match LodestarTrackerLayout.cs

        private void Start()
        {
            if (trackerObject.GetComponent<LodestarTracker>() == null)
                Debug.LogWarning("Prefab is missing script 'LodestarTracker'");

            if (trackerOrigin == null)
                trackerOrigin = gameObject.transform;
        }

        void Update()
        {
            // Discover all currently enabled trackers this frame
            var trackers = InputSystem.devices
                .OfType<TrackedDevice>()
                .Where(d => d.enabled && d.layout == LayoutName)
                .ToList();

            // Build a set of the deviceIds we saw so we can cull stale ones later
            var activeIds = new HashSet<int>(trackers.Select(t => t.deviceId));

            // Ensure each tracker has a matching instance
            foreach (var tracker in trackers)
            {
                // Status
                LodestarTracker.Status status = (LodestarTracker.Status)tracker.trackingState.ReadValue();

                // ID
                var idCtrl = tracker.TryGetChildControl<IntegerControl>("deviceIdentifier");
                int id = 0;
                if (idCtrl != null) id = idCtrl.ReadValue();

                // Instantiate newly conncted trackers
                if (!trackerRegistry.TryGetValue(tracker.deviceId, out var go) || go == null)
                {
                    if (status <= LodestarTracker.Status.Disconnected)
                        continue;

                    // First time we see this tracker? Spawn an instance
                    go = Instantiate(trackerObject, trackerOrigin);
                    go.name = $"Lodestar Tracker [{id}]";
                    trackerRegistry[tracker.deviceId] = go;
                }

                if (status <= LodestarTracker.Status.Disconnected) // Disconnected - delete the tracker instance
                {
                    Destroy(go);
                    trackerRegistry.Remove(tracker.deviceId);
                    continue;
                }

                LodestarTracker t = go.GetComponent<LodestarTracker>();
                go.SetActive(status > LodestarTracker.Status.Off);

                // Tracked
                bool tracking = tracker.isTracked.ReadValue() > 0.5f;

                // Battery
                var batteryCtrl = tracker.TryGetChildControl<IntegerControl>("deviceBattery");
                int battery = 0;
                if (batteryCtrl != null) battery = batteryCtrl.ReadValue();

                // Position
                Vector3 pos = tracker.devicePosition.ReadValue();

                // Rotation (Convert to Unity's coordinate system)
                Quaternion rawRot = tracker.deviceRotation.ReadValue();
                Quaternion adjustedRot = new Quaternion(rawRot.x, rawRot.z, rawRot.y, -rawRot.w) *
                                         Quaternion.Euler(180, -90, 0);

                Quaternion swapAxes = Quaternion.AngleAxis(-90f, Vector3.up);
                adjustedRot = swapAxes * adjustedRot * Quaternion.Inverse(swapAxes);

                if (t != null)
                {
                    t.id = id;
                    t.status = (LodestarTracker.Status)status;
                    t.tracking = tracking;
                    t.battery = battery;

                    t.pose.position = pos;
                    t.pose.rotation = adjustedRot;

                    t.setRenderTracker(renderTrackers);
                }
            }

            // Remove trackers who disappeared this frame
            // (We iterate over a copy because we may modify the dictionary inside)
            foreach (var kvp in trackerRegistry.ToArray())
            {
                if (!activeIds.Contains(kvp.Key))
                {
                    if (kvp.Value != null) Destroy(kvp.Value);
                    trackerRegistry.Remove(kvp.Key);
                }
            }
        }

        // Clean up trackers when the host is disabled or destroyed
        void OnDisable() => Cleanup();
        void OnDestroy() => Cleanup();

        private void Cleanup()
        {
            foreach (var go in trackerRegistry.Values)
                if (go != null) Destroy(go);
            trackerRegistry.Clear();
        }
    }

}
