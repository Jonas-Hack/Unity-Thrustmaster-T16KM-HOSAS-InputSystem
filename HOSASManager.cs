/*
 * Written by Jonas H.
 * 
 * Manages flight sticks
 * handling state change, such as reconnection
 * 
 * Just place anywhere in your project
 * 
 * You can add support for other sticks by creating a new registerStick method
 * and adding it to the supportedSticks dictionary here
 */

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class HOSASManager
{
    // ---------------------------------------------------------------------------------------------------------------------------
    //                                     Your own stick can be added HERE
    // ---------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Add support for your own stick here, by crating an implementation of registerStick and adding it here
    /// </summary>
    readonly static Dictionary<string, System.Action<InputDevice>> supportedSticks = new Dictionary<string, System.Action<InputDevice>>()
    {
        { "T.16000M", SidedTM16KM.registerStick}
        // { "Your stick product name", YourStickRegisterMethod }
    };

    // ---------------------------------------------------------------------------------------------------------------------------



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

        // Now initialize all the sticks
        registerAllSticks();
    }

    /// <summary>
    /// Looks through all devices and then decides on the side each joystick should use.
    /// </summary>
    static void registerAllSticks()
    {
        // Look at all devices
        foreach (Joystick stick in Joystick.all)
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
        // Find the correct handler for this type of stick
        System.Action<InputDevice> registerCall = null;
        bool isSupported = supportedSticks.TryGetValue(device.description.product, out registerCall);

        // I do not know this stick
        if (!isSupported) return;

        // Now lets do the work
        registerCall.Invoke(device);
    }
}