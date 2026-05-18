using UnityEngine;
using UnityEngine.InputSystem;

namespace LodestarInput
{
    // This processor takes the raw status byte (as a float) 
    // and returns 1.0 if it equals compareValue, else 0.0

    public class EqualToProcessor : InputProcessor<float>
    {
        // Name by which this processor is referenced in JSON
        public const string NAME = "equalTo";
        public float compareValue;

        public override float Process(float value, InputControl control)
        {
            return Mathf.Approximately(value, compareValue) ? 1f : 0f;
        }
    }
}