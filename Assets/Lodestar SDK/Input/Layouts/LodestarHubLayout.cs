using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;


namespace LodestarInput
{

    public static class LodestarHubLayout
    {
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod] // Runs in Edit-mode
#endif
        [RuntimeInitializeOnLoadMethod] // Runs in Player
        private static void Register()
        {
            const string json = @"
        {
          ""name""       : ""LodestarHub"",
          ""displayName"": ""Lodestar Hub"",
          ""format""     : ""HID"",

           ""controls"" :
          [
            {
                ""name"": ""hubFirmware"",
                ""layout"": ""Integer"",
                ""format"": ""USHT"",
                ""offset"": 8,
                ""sitInBits"": 16,
                ""reportId"": 1
            },

            {
                ""name"": ""hubHardware"",
                ""layout"": ""Integer"",
                ""format"": ""USHT"",
                ""offset"": 10,
                ""sitInBits"": 16,
                ""reportId"": 1
            },

            {
                ""name"": ""hubStatus"",
                ""layout"": ""Integer"",
                ""format"": ""BYTE"",
                ""offset"": 12,
                ""sitInBits"": 8,
                ""reportId"": 1
            }
          ]
        }";

            InputSystem.RegisterLayout(
                json,
                name: "Lodestar Hub",
                matches: new InputDeviceMatcher()
                    .WithInterface("HID")
                    .WithCapability("vendorId", 0x1915)
                    .WithCapability("productId", 0x1101)
                    .WithCapability("usagePage", 0xFF00));
        }
    }

}