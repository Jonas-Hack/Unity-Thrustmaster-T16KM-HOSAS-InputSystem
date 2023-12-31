# Unity-Thrustmaster-T16KM-HOSAS-InputSystem
 Automatically Differentiate Between Left- & Right Stick Inputs for HOSAS Use

## Purpose
By default Unity does not recognize the difference between left- and right handed fight sticks.
There is however a suitable system for *XR-Controllers* in the form of `Configuration Usage Tags`.

These scripts automatically assign a side to *Thrustmaster T16000M* sticks by reading the value of the little switch on the bottom.

Additionally the sides get updated automatically at runtime, should the switch be flipped again - or another device be plugged in.

# Usage

Just place the scripts somewhere in your project.

## Usage in Input Action UI

In the *Input Actions* Window, for each binding - the side can be chosen:

```<HID::Thrustmaster T.16000M>{leftHand}/stick```


```<HID::Thrustmaster T.16000M>{rightHand}/hat```


## Usage in Code

Additionally the value of ```leftRightSwitch```can be read directly:
````
        // https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/api/UnityEngine.InputSystem.InputAction.html
        InputAction changeHandAction = new InputAction(binding: "<HID::Thrustmaster T.16000M>/leftRightSwitch");
        changeHandAction.performed += (_ => foo()); // Right Side
        changeHandAction.canceled += (_ => bar());  // Left Side
        changeHandAction.Enable();
````
Though I see no reason to do so.

**NOTE:** To get notified of *ANY* change in stick side, use `InputActionType.PassThrough`.

## Support for other Joysticks

For now this *ONLY* works for *Thrustmaster T16000M* joysticks. However, support for other sticks can be added:

Analogous to ``SidedTM16KM.cs``:  
1. Create an Input **Layout Override**
2. Add a **binding** using the `SidedStickInitialize` Attribute
3. Create a **registerStick** method
4. Add the method using the `SidedStickRegistrate` Attribute

If you do so, please create a pull request, so we can all benefit.

## Acknowledgements

This was originally based on two Unity Forum posts: [1](https://forum.unity.com/threads/two-identical-joysticks.639691/), [2](https://forum.unity.com/threads/t-16000m-read-left-hand-right-hand-switch.873124/), though some quality of life features, as well as some bug-fixes, are entirely my own.