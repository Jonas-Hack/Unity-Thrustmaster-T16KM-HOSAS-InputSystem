/*
 * Written by Jonas H.
 * 
 * Reads the swtich on a Thrustmaster T16000M flight stick, to determine if it is left or right handed
 * Based on https://forum.unity.com/threads/t-16000m-read-left-hand-right-hand-switch.873124/
 * Just place this script somewhere in your project.
 * 
 * You can add support for other flight sticks by 
 * 1. Creating an Input Layout Override
 * 2. Adding a binding using the SidedStickInitialize Attribute
 * 3. Creating a registerStick method
 * 4. Adding the method using the SidedStickRegistrate Attribute
 */

using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

public static class SidedTM16KM
{
    /// <summary>
    /// Modifies the Input Device Layout of the Joysticks
    /// Adds additional information on left/right handedness
    /// Adds left/right-switch
    /// Registers with the HOSAS Manager
    /// </summary>
    [SidedStickInitialize("*/leftRightSwitch")]
    public static void Initialize()
    {
        // Add switch to input Layout
        // Read Thrustmaster T16000m "Handedness"-Switch 
        // https://forum.unity.com/threads/t-16000m-read-left-hand-right-hand-switch.873124/
        InputSystem.RegisterLayoutOverride(@"
            {
                ""name"" : ""T16000MWithLeftRightSwitch"",
                ""extend"" : ""HID::Thrustmaster T.16000M"",
                ""beforeRender"" : ""Update"",
                ""controls"" : [
                    { ""name"" : ""leftRightSwitch"", ""layout"" : ""Button"", ""offset"" : 0, ""bit"" : 29, ""sizeInBits"" : 1}
                ]
            }
        ");      
    }


    /// <summary>
    /// Attempts to read the side side the stick is supposed to be on & then assigns it accordingly
    /// </summary>
    /// <param name="device">The joystick Input Device</param>
    [SidedStickRegistrate("T.16000M")]
    public static void registerStick(InputDevice device)
    {
        // Reads the switch on Thrustmaster T16000M joysticks and then assigns them the appropiate side.
        // Getting data from my stick is a bit tricky
        // https://forum.unity.com/threads/t-16000m-read-left-hand-right-hand-switch.873124/
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
        bool isAssinged = SidedStick.getStickStatus(device, out currentRight);

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
        if (hatInUse)
        {
            // Except when stick is unitialized
            // Set to left, because right hand sticks will be re-assinged as soon as hat is being let go
            if (isAssinged) return;
            else SidedStick.setStickStatus(device, false);
        }
        else
        {
            // Default behaviour
            // Set side, if it has changed
            if (currentRight != isRight)
                SidedStick.setStickStatus(device, isRight);
        }
    }

    
}


// Used to store t16kmdevice State
// https://docs.unity3d.com/Packages/com.unity.inputsystem@1.7/manual/Devices.html#device-state
// This could also be used to define the layout, but I prefer to do it as seen above
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