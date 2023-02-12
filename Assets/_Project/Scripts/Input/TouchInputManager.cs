using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VehicleBehaviour;

public class TouchInputManager : MonoBehaviour
{

    List<TouchInputButton> currentTouchInput = new List<TouchInputButton>();

    public void RegisterInput(TouchInputButton touchInputButton)
    {
        if (!currentTouchInput.Contains(touchInputButton))
        {
            currentTouchInput.Add(touchInputButton);
        }
    }

    public void UnregisterInput(TouchInputButton touchInputButton)
    {
        if (currentTouchInput.Contains(touchInputButton))
        {
            currentTouchInput.Remove(touchInputButton);
        }
    }

    public float GetAxis(string axisName)
    {
        float result = 0;
        foreach (TouchInputButton button in currentTouchInput)
        {
            if (button.AxisName == axisName)
            {
                result += button.Value;
            }
        }
        return result;
    }

}
