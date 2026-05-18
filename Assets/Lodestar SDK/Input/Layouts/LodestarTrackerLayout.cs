using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;

namespace LodestarInput
{

    public static class LodestarTrackerLayout
    {
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod] // Runs in Edit-mode
#endif
        [RuntimeInitializeOnLoadMethod] // Runs in Player
        private static void Register()
        {
            InputSystem.RegisterProcessor<EqualToProcessor>(EqualToProcessor.NAME);

            const string json = @"
        {
          ""name""       : ""LodestarTracker"",
          ""displayName"": ""Lodestar Tracker"",
          ""extend""     : ""TrackedDevice"",
          ""format""     : ""HID"",

          ""controls"" :
          [
            {
                ""name"": ""deviceIdentifier"",
                ""layout"": ""Integer"",
                ""format"": ""BYTE"",
                ""offset"": 1,
                ""sizeInBits"": 8,
                ""reportId"": 0
            },

            {
                ""name"": ""trackingState"",
                ""layout"": ""Integer"",
                ""format"": ""BYTE"",
                ""offset"": 2,
                ""sizeInBits"": 8,
                ""reportId"": 0
            },

            {
                ""name"": ""isTracked"",
                ""layout"": ""Button"",
                ""useStateFrom"": ""trackingState"",
                ""processors"": ""scale(factor=255);equalTo(compareValue=5)"",
                ""reportId"": 0
            },

            {
                ""name"": ""deviceBattery"",
                ""layout"": ""Integer"",
                ""format"": ""BYTE"",
                ""offset"": 3,
                ""sizeInBits"": 8,
                ""reportId"": 0
            },

            {
                ""name"": ""devicePosition"",
                ""layout"": ""Vector3"",
                ""offset"": 5,
                ""reportId"": 0
            },

            {
                ""name"": ""deviceRotation"",
                ""layout"": ""Quaternion"",
                ""offset"": 17,
                ""reportId"": 0
            },

            {
                ""name"": ""deviceLinearVelocity"",
                ""layout"": ""Vector3"",
                ""offset"": 33,
                ""reportId"": 0
            },

            {
                ""name"": ""deviceAngularVelocity"",
                ""layout"": ""Vector3"",
                ""offset"": 45,
                ""reportId"": 0
            }
          ]
        }";

            InputSystem.RegisterLayout(
                json,
                name: "Lodestar Tracker",
                matches: new InputDeviceMatcher()
                    .WithInterface("HID")
                    .WithCapability("vendorId", 0x1915)
                    .WithCapability("productId", 0x1101)
                    .WithCapability("usagePage", 0xFF01));
        }
    }

}