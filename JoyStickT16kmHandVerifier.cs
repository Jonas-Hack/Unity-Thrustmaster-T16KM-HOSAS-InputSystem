/*
*  Written by Jonas H.
*
*  Assigns the Thrustmaster T16000m a hand to allow for HOSAS use.
*  
*  Reads the switch on the underside of the stick
*  Its value is stored in bit 26 of the HID desciptor
*  
*  This script is builds on previous work on the Unity Forums
*/


// https://forum.unity.com/threads/two-identical-joysticks.639691/
// https://forum.unity.com/threads/t-16000m-read-left-hand-right-hand-switch.873124/

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
[InitializeOnLoad]
#endif
public static class JoyStickT16kmHandVerifier
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void OnEnable()
    {
        // Register Handedness after Devices have had a chance to setup
        //StartCoroutine(delayedRegistration());

        // Make sure to also check any Sticks, which have been added too late
        InputSystem.onDeviceChange += (device, change) =>
        {
            switch (change)
            {
                case InputDeviceChange.Added:
                case InputDeviceChange.Reconnected:
                case InputDeviceChange.Enabled:
                    Joystick stick = device as Joystick;
                    if (stick != null)
                        registerStick(device);
                    break;

                default:
                    break;
            }
        };


        // Update Sticks, if someone changes their mind
        // Listen for any device with "leftRightSwitch" both press/release and do not perform disambiguation
        InputAction changeHandAction = new InputAction(binding: "*/leftRightSwitch", type: InputActionType.PassThrough);
        changeHandAction.performed += (_ => registerAllSticks());
        changeHandAction.canceled += (_ => registerAllSticks());
        changeHandAction.Enable();
    }

    static IEnumerator delayedRegistration()
    {
        yield return new WaitForEndOfFrame();
        registerAllSticks();
        yield return null;
    }

    static void registerAllSticks()
    {
        // Look at all devices
        foreach (InputDevice device in InputSystem.devices)
        {
            Joystick stick = device as Joystick;
            if (stick != null)
                registerStick(device);
        }
    }

    static unsafe void registerStick(InputDevice device)
    {
        // Only care about my specific joystick type
        if (device.description.product == "T.16000M")
        {
            // Getting data from my stick is a bit tricky
            // https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/api/UnityEngine.InputSystem.InputControlExtensions.html
            // https://github.com/Unity-Technologies/InputSystem/blob/585047ccb5138dff1e2662cd0be453193a585f6f/Packages/com.unity.inputsystem/InputSystem/Controls/InputControlExtensions.cs#L403

            // Need bit 29 of the state of the t19km stick
            byte[] stateBuffer = new byte[4];
            //https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/unsafe-code
            fixed (void* bufferPointer = stateBuffer)
            {
                // Just read the bare data from memory
                InputControlExtensions.CopyState(device, bufferPointer, 4);


                // get the correct bit
                // https://stackoverflow.com/questions/4854207/get-a-specific-bit-from-byte
                bool isRight = (stateBuffer[3] & 0b00100000) != 0;

                /*
                // Debug read data
                string hexBuffer = System.BitConverter.ToString(stateBuffer);
                string lastInBinary = (System.Convert.ToString(stateBuffer[3], 2).PadLeft(8, '0'));
                Debug.Log(device.displayName + ": " + hexBuffer + ", " + lastInBinary + ", " + isRight);
                */

                // Now tell the input system
                setStickStatus(device, isRight);
            }
        }
    }

    static void setStickStatus(InputDevice device, bool isRight)
    {
        InputSystem.SetDeviceUsage(device, (isRight ? CommonUsages.RightHand : CommonUsages.LeftHand));
        //TODO: left-right switch mirrors the layout of all buttons
    }
}