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
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

using System.Runtime.InteropServices;


public static class JoyStickT16kmHandVerifier
{
    /// <summary>
    /// Inital Setup of the Sticks. Makes sure to listen to any changes
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
#if UNITY_EDITOR
    [InitializeOnLoadMethod]
#endif
    static void OnEnable()
    {
        // Make sure to also check any Sticks, which get plugged in sometime later
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


        // Change the settings of sticks, if their switch is flipped
        // Listen for any device with "leftRightSwitch" both press/release and do not perform disambiguation
        InputAction changeHandAction = new InputAction(binding: "*/leftRightSwitch", type: InputActionType.PassThrough);
        changeHandAction.performed += (_ => registerAllSticks());
        changeHandAction.canceled += (_ => registerAllSticks());
        changeHandAction.Enable();
    }

    /// <summary>
    /// Looks throuigh all devices and then decides on the side each joystick should use.
    /// </summary>
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

    /// <summary>
    /// Reads the switch on Thrustmaster T16000M joysticks and then assigns them the appropiate side.
    /// </summary>
    /// <param name="device">The joystick Input Device</param>
    static void registerStick(InputDevice device)
    {
        // Only care about my specific joystick type
        if (device.description.product == "T.16000M")
        {
            // Getting data from my stick is a bit tricky
            // https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/api/UnityEngine.InputSystem.InputControlExtensions.html
            // https://github.com/Unity-Technologies/InputSystem/blob/585047ccb5138dff1e2662cd0be453193a585f6f/Packages/com.unity.inputsystem/InputSystem/Controls/InputControlExtensions.cs#L403

            // To prevent unsafe code, I switched to the variation of the CopyState method, whithout pointers
            // This is potentially slower, but doens't force the AllowUnsafeBlocks compile option


            // Need the 29th bit of the state of the t19km stick
            // Read the date from the device memory
            T16KMState state;
            device.CopyState(out state);

            // Get the correct bit
            bool isRight = (state.leftRightSwitch & 0b00100000) != 0;
            setStickStatus(device, isRight);
            
        }
    }

    /// <summary>
    /// Sets the side (left/right) of the device.
    /// </summary>
    /// <param name="device">InputDevice - hopefully a T16000M</param>
    /// <param name="isRight">Is the stick left or right handed?</param>
    static void setStickStatus(InputDevice device, bool isRight)
    {
        InputSystem.SetDeviceUsage(device, (isRight ? CommonUsages.RightHand : CommonUsages.LeftHand));
        //TODO: left-right switch mirrors the layout of all buttons
    }



    // Used to store t16kmdevice State
    // https://docs.unity3d.com/Packages/com.unity.inputsystem@1.7/manual/Devices.html#device-state
    // This could also be used to define the layout, but I prefer to do it in InputJoyStickHandTypes.cs
    // It might be cleaner to switch to this way entirely, instead of going half way with both
    [StructLayout(LayoutKind.Explicit, Size = 4)] // 34 bit ~ 4.25 byte, but only need 29th bit, so 4 byte is ok
    internal struct T16KMState : IInputStateTypeInfo
    {
        // ID needs to match stick format in State viewer
        // Typically you'd want something unique here, but this Layout does not get officially registered. I just use it as if it were
        public FourCC format => new FourCC('H', 'I', 'D');


        // The all important switch
        [UnityEngine.InputSystem.Layouts.InputControl(name = "leftRightSwitch", layout = "Button", offset = 0, bit = 29, sizeInBits = 1)]
        [FieldOffset(3)] public byte leftRightSwitch;


        // Not adding any of the other controls, because this not a "global" definition of the device and thus doesn't need to be complete
    }
}