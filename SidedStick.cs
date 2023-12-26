/*
 * Written by Jonas H.
 * 
 * Provides utility methods for generic Flight Sticks
 * Aswell as a definition for left / right handedness
 * 
 * Based on https://forum.unity.com/threads/two-identical-joysticks.639691/
 * 
 * Just place anywhere in your project
 */

using UnityEngine;

using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public static class SidedStick
{
    /// <summary>
    /// Modifies the Input Device Layout of the Joysticks
    /// Adds additional information on left/right handedness
    /// </summary>
    public  static void InitializeGenericStickSides()
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
    }

    /// <summary>
    /// Sets the side (left/right) of the device.
    /// </summary>
    /// <param name="device">InputDevice - hopefully a flight stick</param>
    /// <param name="isRight">Is the stick left or right handed?</param>
    public static void setStickStatus(InputDevice device, bool isRight)
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
    public static bool getStickStatus(InputDevice device, out bool value)
    {
        bool rightHanded = device.usages.Contains(CommonUsages.RightHand);
        bool leftHanded = device.usages.Contains(CommonUsages.LeftHand);

        if (rightHanded) value = true;
        else value = false;

        return (rightHanded || leftHanded);
    }
}


/// <summary>
/// Used to mark all Stick Initialisation methods.
/// </summary>
public class SidedStickInitializeAttribute : System.Attribute
{
    public string binding { get; }

    /// <summary>
    /// Used to mark all Stick Initialisation methods.
    /// </summary>
    /// <param name="binding">The input binding path of your "handedness" button (defined in your input layout)</param>
    public SidedStickInitializeAttribute(string binding = null)
    {
        this.binding = binding;
    }
}

/// <summary>
/// Used to mark all Stick Registration methods.
/// </summary>
public class SidedStickRegistrateAttribute : System.Attribute
{
    public string productName { get; }

    /// <summary>
    /// Used to mark all Stick registration methods.
    /// </summary>
    /// <param name="registerAction">Your registerStick method. It handles "handedness" detection and assignment</param>
    /// <param name="productName">The ID of your flight stick = device.description.product</param>
    public SidedStickRegistrateAttribute(string productName)
    {
        this.productName = productName;
    }
}
