/*
*  Written by Jonas H.
*
*  Provides left/right hand flags to joysticks
*  To be used in the Input bindings in the UI for the input mapping
*  
*  Just Place this Script somewhere in your project
*  
*  This script is essentially copy pasted from the Unity Forums
*/

// https://forum.unity.com/threads/two-identical-joysticks.639691/
// https://forum.unity.com/threads/t-16000m-read-left-hand-right-hand-switch.873124/


using UnityEngine.InputSystem;
using UnityEngine;


#if UNITY_EDITOR
using UnityEditor;
[InitializeOnLoad]
#endif
public static class InputManagerJoyStickHandTypes
{
    /// <summary>
    /// Modifies the Input Device Layout of the Joysticks
    /// Adds additional information on left/right handedness
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        // Give the Option to choose between hands in UI for any Joystick
        // https://forum.unity.com/threads/two-identical-joysticks.639691/
        InputSystem.RegisterLayoutOverride(@"
              {
                  ""name"" : ""JoystickConfigurationUsageTags"",
                  ""extend"" : ""Joystick"",
                  ""commonUsages"" : [
                      ""leftHand"", ""rightHand""
                  ]
              }
        ");

        // Read Thrustmaster T16000m "Handedness"-Switch 
        // https://forum.unity.com/threads/t-16000m-read-left-hand-right-hand-switch.873124/
        InputSystem.RegisterLayoutOverride(@"
            {
                ""name"" : ""T16000MWithLeftRightSwitch"",
                ""extend"" : ""HID::Thrustmaster T.16000M"",
                ""controls"" : [
                    { ""name"" : ""leftRightSwitch"", ""layout"" : ""Button"", ""offset"" : 0, ""bit"" : 29, ""sizeInBits"" : 1 }
                ]
            }
        ");
    }
}
