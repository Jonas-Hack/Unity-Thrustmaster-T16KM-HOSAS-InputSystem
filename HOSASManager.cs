/*
 * Written by Jonas H.
 * 
 * Manages flight sticks
 * handling state change, such as reconnection
 * 
 * Just place anywhere in your project
 * 
 * You can add support for other flight sticks by 
 * 1. Creating an Input Layout Override
 * 2. Adding a binding using the SidedStickInitialize Attribute
 * 3. Creating a registerStick method
 * 4. Adding the method using the SidedStickRegistrate Attribute
 * 
 * Take a look at SidedTM16KM.cs to see, how it's done
 * You do NOT need to change anything in this file
 */

using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class HOSASManager
{
    // ---------------------------------------------------------------------------------------------------------------------------
    //                                    This stores the info & actions of supported flight sticks
    //                      You can add more using the SidedStickInitialize & SidedStickRegistrate Attributes
    // ---------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Add support for your own stick here, by crating an implementation of registerStick and adding it here
    /// </summary>
    public static Dictionary<string, System.Action<InputDevice>> supportedSticks = new Dictionary<string, System.Action<InputDevice>>();

    /// <summary> 
    /// Change the settings of sticks, if their switch is flipped
    /// Listen to Input Events
    /// <summary>
    public static InputAction changeHandAction = new InputAction(type: InputActionType.PassThrough);

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
        // Add generic leftHand / rightHand layout overrides for flightSticks
        SidedStick.InitializeGenericStickSides();
    
        // Load all the supported stick info using reflection

        // First find all SidedStickInitializeAttributed methods
        foreach (MethodInfo initializeMethod in System.AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(assembly => assembly.GetTypes())
                        .SelectMany(type => type.GetMethods())
                        .Where(method => method.GetCustomAttributes(typeof(SidedStickInitializeAttribute), false).Length > 0))
        {
            // Call initilisation
            initializeMethod.Invoke(null, null);

            // Add input binding to changeHandAction
            string binding = initializeMethod.GetCustomAttribute<SidedStickInitializeAttribute>().binding;
            if(binding != null) changeHandAction.AddBinding(binding);
        }

        // Now do the same for the registration methods
        foreach (MethodInfo initializeMethod in System.AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(assembly => assembly.GetTypes())
                        .SelectMany(type => type.GetMethods())
                        .Where(method => method.GetCustomAttributes(typeof(SidedStickRegistrateAttribute), false).Length > 0))
        {
            // Get Info from Attribute
            System.Action<InputDevice> registrateStickMethod = (System.Action<InputDevice>) initializeMethod.CreateDelegate(typeof(System.Action<InputDevice>));
            string productName = initializeMethod.GetCustomAttribute<SidedStickRegistrateAttribute>().productName;

            // Add registerStickMethod to supportedSticks
            bool success = supportedSticks.TryAdd(productName, registrateStickMethod);
            if(!success) Debug.LogWarning("HOSAS - could not add " + productName + " to manager. Possibly, it already is supported. This is to be expected, while in the editor");
        }



        // Now register all the sticks
        registerAllSticks();

        // Make sure to check any Sticks, which get plugged in sometime later
        InputSystem.onDeviceChange += (device, change) =>
        {
            switch (change)
            {
                case InputDeviceChange.Enabled:
                case InputDeviceChange.Reconnected:
                case InputDeviceChange.HardReset:
                    Joystick stick = device as Joystick;
                    if (stick != null) registerStick(device);
                    break;

                default:
                    break;
            }
        };

        // Alsoupdated Sticks if there are changes
        changeHandAction.performed += registerStickFromAction;
        changeHandAction.canceled += registerStickFromAction;
        changeHandAction.Enable();
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
        // NOTE: the value could be read from the context, but other factors might be at play, so I don't
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
        if (!isSupported)
        {
#if UNITY_EDITOR
            Debug.LogWarning("HOSASManager - Unsupported Flight Stick, probably just because of wonky load order");
#else
            Debug.LogWarning("HOSASManager - Unsupported Flight Stick: " + device.description.product);
#endif
            return;
        }

        // Now lets do the work
        registerCall.Invoke(device);
    }
}