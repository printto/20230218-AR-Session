using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using VehicleBehaviour;

public class CarResetManager : MonoBehaviour
{

    public void ResetCar()
    {
        WheelVehicle[] vehicles = FindObjectsOfType<WheelVehicle>();
        foreach(WheelVehicle vehicle in vehicles)
        {
            vehicle.ResetPos();
            vehicle.transform.localPosition = Vector3.zero;
        }
    }

}
