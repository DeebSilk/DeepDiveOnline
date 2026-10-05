using UnityEngine;
using UnityEngine.InputSystem;

public static class CombatInput
{
    public static float GetAxis(string axisName)
    {
        if (Keyboard.current != null)
        {
            switch (axisName)
            {
                case "Horizontal":
                    return (Keyboard.current.dKey.isPressed ? 1f : 0f) + (Keyboard.current.aKey.isPressed ? -1f : 0f);
                case "Vertical":
                    return (Keyboard.current.wKey.isPressed ? 1f : 0f) + (Keyboard.current.sKey.isPressed ? -1f : 0f);
            }
        }

        return Input.GetAxis(axisName);
    }

    public static bool GetButtonDown(string actionName)
    {
        if (Keyboard.current != null)
        {
            switch (actionName)
            {
                case "Attack":
                    return Keyboard.current.qKey.wasPressedThisFrame ||
                           Mouse.current?.leftButton.wasPressedThisFrame == true;
                case "Dash":
                    return Keyboard.current.leftShiftKey.wasPressedThisFrame;
                case "Jump":
                    return Keyboard.current.spaceKey.wasPressedThisFrame;
                case "Interact":
                    return Keyboard.current.eKey.wasPressedThisFrame;
            }
        }

        return Input.GetButtonDown(actionName);
    }

    public static bool GetButton(string actionName)
    {
        if (Keyboard.current != null)
        {
            switch (actionName)
            {
                case "Dash":
                    return Keyboard.current.leftShiftKey.isPressed;
            }
        }

        return Input.GetButton(actionName);
    }
}
