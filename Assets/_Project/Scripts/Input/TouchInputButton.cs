using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TouchInputButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{

    [SerializeField] TouchInputManager touchInputManager;
    [SerializeField] string axisName;
    [SerializeField] float value;

    public string AxisName
    {
        get
        {
            return axisName;
        }
    }

    public float Value
    {
        get
        {
            return value;
        }
    }

    public void OnPointerDown(PointerEventData data)
    {
        Debug.Log("Register Button " + axisName + " " + value);
        touchInputManager.RegisterInput(this);
    }
    public void OnPointerUp(PointerEventData data)
    {
        Debug.Log("Unregister Button " + axisName + " " + value);
        touchInputManager.UnregisterInput(this);
    }

}
