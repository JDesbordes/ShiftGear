using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSCamera : MonoBehaviour {

    public Transform carCam;
    public Transform car;
    public Rigidbody carPhysics;

    public float tilt;

    void FixedUpdate()
    {
        Vector3 targetLook = transform.position + car.transform.forward;
        carCam.LookAt(targetLook, car.up);
        carCam.RotateAround(transform.position, transform.forward, tilt);
    }
}
