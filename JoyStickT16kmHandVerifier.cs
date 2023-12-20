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
                case InputDeviceChange.SoftReset:
                case InputDeviceChange.HardReset:
                    Joystick stick = device as Joystick;
                    if (stick != null) registerStick(device);
                    break;

                default:
                    break;
            }
        };


        // Change the settings of sticks, if their switch is flipped
        // Listen for any device with "leftRightSwitch" both press/release and do not perform disambiguation
        InputAction changeHandAction = new InputAction(binding: "*/leftRightSwitch", type: InputActionType.PassThrough);
        changeHandAction.performed += registerStickFromAction;
        changeHandAction.canceled += registerStickFromAction;
        changeHandAction.Enable();


        // Now initialize all the sticks
        registerAllSticks();
    }


    /// <summary>
    /// Looks through all devices and then decides on the side each joystick should use.
    /// </summary>
    static void registerAllSticks()
    {
        // Look at all devices
        foreach(Joystick stick in Joystick.all)
        {
            registerStick(stick);
        }
    }

    /// <summary>
    /// Automatically detect the device that triggered the action and then re-register it
    /// </summary>
    /// <param name="context"></param>
    public static void registerStickFromAction(InputAction.CallbackContext context)
    {
        InputDevice device = context.control.device;
        registerStick(device);
    }

    /// <summary>
    /// Attempts to read the side side the stick is supposed to be on & then assigns it accordingly
    /// </summary>
    /// <param name="device">The joystick Input Device</param>
    static void registerStick(InputDevice device)
    {
        switch(device.description.product)
        {
            // Reads the switch on Thrustmaster T16000M joysticks and then assigns them the appropiate side.
            case "T.16000M":
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

                // Get the current device setting
                // Needed for edge cases
                bool currentRight = false;
                bool isAssinged = getStickStatus(device, out currentRight);

                // Issue: The Hat (dpad) writes to the same byte and therefore flips my bit erroniously
                // Actual binary input
                // Notice how the hat sets the 3rd bit to 0

                // Left Hand Stick:           Right Hand Stick:

                // 0x 1F = idle               0x 3F = idle
                // 0b 0001 1111               0b 0011 1111
                // 31                         63

                // 0x 04 = hat backward       0x 04 = hat backward
                // 0b 0000 0100               0b 0000 0100
                // 4                          4

                // 0x 00 = hat forward        0x 00 = hat forward
                // 0b 0000 0000               0b 0000 0000
                // 0                          0

                // 0x 06 = hat left           0x 06 = hat left
                // 0b 0000 0110               0b 0000 0110
                // 6                          6

                // 0x 02 = hat right          0x 02 = hat right
                // 0b 0000 0010               0b 0000 0010
                // 2                          2

                // 0x 08 in editor on init for both sticks
                // 0b 0000 1000         ????
                // 8


                // The third bit can only tell us something in idle / when the hat is not being used
                // When the hat is used, the entire first byte is 0
                // So lets add an extra check in there                


                // This rejection of junk values leads to another problem
                // The value is not correctly assinged, if the first input is illegal
                // Because the only assingment is rejected, no side is set
                // Specifically for the left hand stick, as when the stick is let go, the bit stays at 0
                // whereas the bit goes to 1 for right hand sticks
                // As a remendy, I set unassinged sticks as left

                // Check if hat is being used
                bool hatInUse = (state.leftRightSwitch & 0b11110000) == 0;

                // Discard illegal values, which accur if hat is being used
                if(hatInUse)
                {
                    // Except when stick is unitialized
                    // Set to left, because right hand sticks will be re-assinged as soon as hat is being let go
                    if (isAssinged) break;
                    else setStickStatus(device, false);
                }
                else
                {
                    // Default behaviour
                    // Set side, if it has changed
                    if (currentRight != isRight)
                        setStickStatus(device, isRight);
                }

                break;

            /*
             * // This is where you could add other types of flight stick
             * case "Your Stick Product Name
             *      // Code analogous to the TM16000M
             *      // So define a struct for the layout of data
             *      // according the State of your device in the Input Debug
             *      // Then read the correct bit of your switch
             *      // asuming your stick has a switch.
             *      // otherwise you could come up with some other metric
             *      // such as holding the stick to its respective side on startup
             *      break;
            */

            default:
                // Cannot assign anything to an unknown swtich
                break;
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

    /// <summary>
    /// Checks if device is assigned to a hand and if so which one
    /// </summary>
    /// <param name="device">Device to be checked</param>
    /// <param name="value">Place to store which hand - true: right, false: left</param>
    /// <returns>Is the device assinged to a side at all?</returns>
    static bool getStickStatus(InputDevice device, out bool value)
    {
        bool rightHanded = device.usages.Contains(CommonUsages.RightHand);
        bool leftHanded = device.usages.Contains(CommonUsages.LeftHand);

        if (rightHanded) value = true;
        else value = false;

        return (rightHanded || leftHanded);
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