using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TPSCameraController : MonoBehaviour {

    // Use this for initialization
    public Transform carCam;
    public Transform fpsCam;
    public Transform car;
    public Rigidbody carPhysics;

    [Tooltip("camera desired position behind the car.")]
    public Vector3 MinoffsetFromCar;
    public Vector3 MaxoffsetFromCar;

    Vector3 baseMinoffsetFromCar, baseMaxoffsetFromCar;
    public float minY, maxY, minZ, maxZ;

    public float max_strafe_amount, max_front_amount;

    public Vector3 MinlookOffset = new Vector3(0, 1.5f, 0);
    public Vector3 MaxlookOffset = new Vector3(0, 2, 0);

    Vector3 targetpos,targetLook;
    Vector3 look = Vector3.zero;

    public Vector3 midChangepos;

    public float BaseFOV;
    public float ChangeFOV;

    public float tilt;
    public float verticaltilt;

    float percentage;
    float smoothFly;

    public bool switchform = false;

    bool midposreached = false;

    void Start()
    {
        baseMinoffsetFromCar = MinoffsetFromCar;
        baseMaxoffsetFromCar = MaxoffsetFromCar;
    }

    void FixedUpdate()
    {

        //displaced car position (generates an output camera position separate from car's)


        //Moves the camera to match the car's position.

        percentage = Mathf.Lerp(percentage, car.gameObject.GetComponent<PlayerController>().SpeedPercentage(), Time.fixedDeltaTime);

        float h = Input.GetAxis("Horizontal");
        MinlookOffset.z = Mathf.Lerp(MinlookOffset.z, Mathf.Abs(h), Time.fixedDeltaTime);
        MaxlookOffset.z = Mathf.Lerp(MaxlookOffset.z, Mathf.Abs(h) * max_front_amount, Time.fixedDeltaTime);
        MinoffsetFromCar.x = Mathf.Lerp(MinoffsetFromCar.x, -h, Time.fixedDeltaTime);
        MaxoffsetFromCar.x = Mathf.Lerp(MaxoffsetFromCar.x, -h * max_strafe_amount, Time.fixedDeltaTime);

        float v = Input.GetAxis("Vertical")/2;
        if (car.gameObject.GetComponent<PlayerController>().flying)
        {
            smoothFly += Time.fixedDeltaTime * (Mathf.Abs(v) > 0.1f ? v : (0.4f - smoothFly));
            smoothFly = smoothFly >= 0.75f ? 0.75f : smoothFly <= 0 ? 0 : smoothFly;
        }
        else
        {
            smoothFly = Mathf.Lerp(smoothFly, 0, Time.deltaTime);
        }

        Vector3 lookOffset = Vector3.Lerp(MinlookOffset, MaxlookOffset, Mathf.Pow(percentage, 3)); 
        RaycastHit hit2;

        if (Physics.Raycast(car.transform.position, car.transform.forward, out hit2, 7,LayerMask.GetMask("Path","GravityEnhancer")) && car.transform.forward.y < -0.2f&& !car.GetComponent<PlayerController>().flying)
        {
            targetLook = Vector3.Lerp(targetLook,hit2.point + Vector3.Reflect(car.transform.forward, hit2.normal) * (Mathf.Clamp(7 - hit2.distance,0,4)),Time.deltaTime/0.1f);

            MinoffsetFromCar.y = Mathf.Lerp(baseMinoffsetFromCar.y, minY, 1 - hit2.distance / 7);
            MaxoffsetFromCar.y = Mathf.Lerp(baseMaxoffsetFromCar.y, maxY, 1 - hit2.distance / 7);
            MinoffsetFromCar.z = Mathf.Lerp(baseMinoffsetFromCar.z, minZ, 1 - hit2.distance / 7);
            MaxoffsetFromCar.z = Mathf.Lerp(baseMaxoffsetFromCar.z, maxZ, 1 - hit2.distance / 7);
        }
        else
        {
            targetLook = car.position + car.transform.TransformVector(lookOffset);
            MinoffsetFromCar.y = Mathf.Lerp(MinoffsetFromCar.y,baseMinoffsetFromCar.y,Time.deltaTime/0.5f);
            MaxoffsetFromCar.y = Mathf.Lerp(MaxoffsetFromCar.y, baseMaxoffsetFromCar.y, Time.deltaTime / 0.5f);
            MinoffsetFromCar.z = Mathf.Lerp(MinoffsetFromCar.z, baseMinoffsetFromCar.z, Time.deltaTime / 0.5f);
            MaxoffsetFromCar.z = Mathf.Lerp(MaxoffsetFromCar.z, baseMaxoffsetFromCar.z, Time.deltaTime / 0.5f);
        }

        look = targetLook;

        carCam.LookAt(look, car.up);
        if (Mathf.Abs(Input.GetAxis("RightHorizontal"))>0.2f|| Mathf.Abs(Input.GetAxis("RightVertical"))>0.2f) {
            carCam.RotateAround(car.transform.position,car.transform.up, Input.GetAxis("RightHorizontal")*90);
        }
        else {
            carCam.RotateAround(transform.position, transform.forward, tilt);
        }
        

        RaycastHit hit;

        if (Physics.Raycast(targetLook, (transform.position - targetLook).normalized, out hit))
        {

            if (Vector3.Distance(transform.position, new Vector3(car.position.x, car.position.y, car.position.z)) + 0.25f > Vector3.Distance(new Vector3(car.position.x, car.position.y, car.position.z), hit.point) && hit.collider.tag != "Player")
            {
                carCam.position = hit.point;
            }
        }

        Vector3 targetpos = Vector3.Lerp(car.position - car.transform.TransformVector(MinoffsetFromCar), car.position - car.transform.TransformVector(MaxoffsetFromCar), percentage);
        GetComponent<Camera>().fieldOfView = Mathf.Lerp(BaseFOV, ChangeFOV, percentage);
        if (switchform)
        {
            carCam.LookAt(car.transform.position + car.transform.forward * 100, Vector3.up);

            if (midposreached == false)
            {
                carCam.position = Vector3.Lerp(carCam.position, car.transform.position + car.transform.TransformVector(midChangepos), Time.deltaTime / 0.25f);
                midposreached = Vector3.Distance(carCam.position, car.transform.position + car.transform.TransformVector(midChangepos)) < 0.2f;
            }
            else
            {
                carCam.position = Vector3.Lerp(carCam.position, fpsCam.position, Time.deltaTime / 0.15f);
            }
            if (Vector3.Distance(carCam.position, fpsCam.position) < 0.2f)
            {
                fpsCam.GetComponent<Camera>().enabled = true;
                carCam.GetComponent<Camera>().enabled = false;
                //HUDCamera.enabled = true;
            }
        }
        else
        {
            midposreached = false;
            carCam.position = Vector3.Lerp(carCam.position, targetpos, Time.deltaTime / Mathf.Clamp(Vector3.Distance(targetpos, carCam.position), 0, 0.25f));
        }
    }
}
