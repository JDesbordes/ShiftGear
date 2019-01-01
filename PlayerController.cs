using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {
    public float hover_fwd_accel, rocket_fwd_accel;
    [Tooltip("Maximum vehicle forward velocity in m/s")]
    public float hover_fwd_max_speed, rocket_fwd_max_speed;
    public float hover_bwd_accel, rocket_bwd_accel;
    [Tooltip("Maximum vehicle Backward velocity in m/s")]
    public float hover_bwd_max_speed, rocket_bwd_max_speed;
    [Tooltip("Default slow affecting the vehicle when speed is untouched")]
    public float auto_brake_speed;
    public float hover_brake_speed, rocket_brake_speed;
    public float hover_turn_speed, rocket_turn_speed;

    public float hover_fall_angle, hover_fall_up_angle, hover_fall_down_angle;


    public Camera fps, tps;
    float hover_tilt = 45;
    float rocket_tilt = 120;

    /*Auto adjust to track surface parameters*/
    public float hover_height;     //Distance to keep from the ground

    public GameObject visuals, glove;

    //GameObject[] m_Animators;

    private float height_smooth;   //How fast the vehicle will readjust to "hover_height"
    private float pitch_smooth;    //How fast the vehicle will adjust its rotation to match track normal

    //
    private Vector3 prev_up;
    public float yaw;
    private float maxAngleChange = 55;
    private float smooth_y;

    //Character's rigidbody
    private Rigidbody m_rb;

    //Speed and acceleration variables
    public float current_speed;
    public float next_speed;
    private Vector3 lastPos;
    float smooth_accel;
    private Vector3 direction;
    private float drift=0;

    //Rotation speed
    float smooth_angle;
    float smooth_angle2;
    float hover_soap = 0.13f;
    float rocket_soap = 0.05f;
    float rotation_help = 0.013f;
    public float turn_variable;
    float turnAngle;

    //Current form true=hoverboard
    public bool form = true;
    float animTimer = 0;
    
    //Extrashit
    float extraFlightAngle;
    bool glide;
    float rotationforce;
    public float animRotation;
    Vector3 prev_forward;

    //Boosts
    public float startTime;
    public bool Boost = false;
    public float BoostDecrease = 2;

    public bool flying;
    private float flyingTimer;

    //Animation variables
    public Animator spikeAnimator, hoverAnimator;
    public bool landing;
    public int hitAnim;
    public float animAcceleration;

    TargetSystem targetingScript;

    Collider targetInteraction,potentialInteraction;

    [HideInInspector]
    public bool playerControlled=true;

    float HorizontalInput;
    float SpeedInput;

    //TempValues
    bool hascontrol = true;


    // Use this for initialization
    void Start()
    {
        targetingScript = GetComponent<TargetSystem>();
        m_rb = GetComponent<Rigidbody>();
        StartCoroutine(CheckTargets());
        StartCoroutine(ActivateInteraction());
    }

    // Update is called once per frame
    void Update()
    {
        if (playerControlled)
        {
            if (Input.GetButtonDown("ChangeForm") && Time.time - animTimer > 1)
            {
                animTimer = Time.time;
                smooth_accel = 0;
                smooth_angle = 0;
                form = !form;
                if (form)
                {
                    fps.enabled = false;
                    tps.enabled = true;
                    tps.GetComponent<TPSCameraController>().switchform = false;
                }
                else
                {
                    tps.GetComponent<TPSCameraController>().switchform = true;
                }
            }
            SpeedInput = Input.GetAxis("Forward");
            HorizontalInput = Input.GetAxis("Horizontal");
            if(hascontrol == false)
            {
                SpeedInput = (-transform.forward.y*0.8f);
            }
            if (SpeedInput > 0)
            {
                drift = 0;
                /*Increase our current speed only if it is not greater than fwd_max_speed*/
                if (form)
                {
                    if (current_speed >= hover_fwd_max_speed && Boost == true)
                    {
                        current_speed -= auto_brake_speed * Time.deltaTime * BoostDecrease;
                    }
                    else if (current_speed >= hover_fwd_max_speed)
                    {
                        current_speed -= auto_brake_speed * Time.deltaTime;
                    }
                    else
                    {
                        current_speed += hover_fwd_accel * Time.deltaTime;
                        Boost = false;
                    }
                    //current_speed += ((current_speed >= hover_fwd_max_speed) ? -auto_brake_speed : hover_fwd_accel) * Time.deltaTime;
                }
                else
                {
                    if (current_speed >= rocket_fwd_max_speed && Boost == true)
                    {
                        current_speed -= auto_brake_speed * Time.deltaTime * BoostDecrease;
                    }
                    else if (current_speed >= rocket_fwd_max_speed)
                    {
                        current_speed -= auto_brake_speed * Time.deltaTime;
                    }
                    else
                    {
                        current_speed += rocket_fwd_accel * Time.deltaTime;
                        Boost = false;
                    }
                    //current_speed += ((current_speed >= rocket_fwd_max_speed) ? -auto_brake_speed : rocket_fwd_accel) * Time.deltaTime;
                }
            }
            else if (SpeedInput < 0 && current_speed > -hover_bwd_max_speed)
            {
                if (current_speed > 1&&flying==false)
                {
                    drift = Mathf.Lerp(drift, 1, Time.deltaTime * (0.5f + (current_speed / (form?hover_fwd_max_speed:rocket_fwd_max_speed)) * 0.5f));
                }
                /*Decrease our current speed only if it is not smaller than bwd_max_speed*/
                if (form)
                {
                    //Test percentage increase
                    current_speed -= ((current_speed > 0) ? hover_brake_speed : hover_bwd_accel) * Time.deltaTime*(1.2f - Mathf.Abs(HorizontalInput));
                }
                else
                {;
                    current_speed -= ((current_speed > 0) ? rocket_brake_speed : rocket_bwd_accel) * Time.deltaTime*(1.2f - Mathf.Abs(HorizontalInput));
                }
            }
            else
            {
                if (form && current_speed > hover_fwd_max_speed)
                {
                    current_speed -= auto_brake_speed * Time.deltaTime * BoostDecrease;
                }
                else if (!form && current_speed > rocket_fwd_max_speed)
                {
                    current_speed -= auto_brake_speed * Time.deltaTime * BoostDecrease;
                }
                else if (current_speed > 0.1f)
                {
                    /*The ship will slow down by itself if we dont accelerate*/
                    Boost = false;
                    current_speed -= auto_brake_speed * Time.deltaTime;
                }
                else if (current_speed < -0.1f)
                {
                    Boost = false;
                    current_speed += auto_brake_speed * Time.deltaTime;
                }
                else
                {
                    current_speed = 0;
                }
            }

            if (Input.GetButton("Interaction") && potentialInteraction != null&&form)
            {
                targetInteraction = potentialInteraction;
            }

            float v = Input.GetAxis("Vertical") / 2;

            RaycastHit hit;
            RaycastHit fronthit;
            RaycastHit backhit;
            RaycastHit lefthit;
            RaycastHit righthit;


            bool front = Physics.Raycast(transform.position + transform.forward / 2, -prev_up, out fronthit, 3, LayerMask.GetMask("Path",  "GravityEnhancer" ));
            bool back = Physics.Raycast(transform.position - transform.forward / 2, -prev_up, out backhit, 3, LayerMask.GetMask("Path", "GravityEnhancer" ));
            //(form?true:transform.forward.y>0)
            if (Physics.Raycast(transform.position, -prev_up, out hit, 3, LayerMask.GetMask("Path","GravityEnhancer")) && (front || back))
            {
                if (!form && transform.up.y < -0.1f)
                {
                    flightManager(v);
                }
                else
                {
                    if (flying == true)
                    {
                        current_speed = next_speed;
                        flying = false;
                        if (Time.time - flyingTimer > 0.5f)
                        {

                            landing = true;
                        }
                    }
                    else if (landing == true)
                    {
                        landing = false;
                    }


                    height_smooth = 5 / hit.distance;
                    Vector3 normals = hit.normal;
                    if (Physics.Raycast(transform.position + transform.right * 0.5f, -prev_up, out righthit, 3, LayerMask.GetMask("Path", "GravityEnhancer")))
                    {
                        normals += righthit.normal;
                    }
                    if (Physics.Raycast(transform.position + transform.right * 0.5f, -prev_up, out lefthit, 3, LayerMask.GetMask("Path", "GravityEnhancer")))
                    {
                        normals += lefthit.normal;
                    }
                    if (front)
                    {
                        normals += fronthit.normal * current_speed / 5;
                    }

                    smooth_angle = 0;
                    if (smooth_accel > 0)
                    {
                        smooth_accel = 0;
                    }
                    else
                    {
                        smooth_accel -= Time.deltaTime;
                    }

                    pitch_smooth = 3;

                    //Add in later with better labeling of path and gravity enhancer
                    if (hit.collider.gameObject.layer == 8)
                    {
                        pitch_smooth /= 2.5f;
                    }

                    /*Here are the meat and potatoes: first we calculate the new up vector for the ship using lerp so that it is smoothed*/
                    Vector3 desired_up = Vector3.Lerp(prev_up, normals, Time.deltaTime * pitch_smooth / (hit.distance + 0.25f));

                    /*Then we get the angle that we have to rotate in quaternion format*/
                    Quaternion tilt = Quaternion.FromToRotation(transform.up, desired_up);
                    /*Now we apply it to the ship with the quaternion product property*/
                    transform.rotation = tilt * transform.rotation;
                    if (!form && hit.collider.gameObject.layer == 9)
                    {
                        hascontrol = false;
                        Vector3 desired_forward = Vector3.ProjectOnPlane(Vector3.down, normals);
                        desired_forward = Vector3.Lerp(transform.forward, desired_forward, Time.deltaTime * pitch_smooth);
                        Quaternion tilt2 = Quaternion.FromToRotation(transform.forward, desired_forward);
                        transform.rotation = tilt2 * transform.rotation;
                    }
                    else
                    {
                        hascontrol = true;
                    }
                    /*Smoothly adjust our height*/
                    smooth_y = Mathf.Lerp(hit.distance, hover_height, Time.deltaTime * height_smooth);
                    transform.localPosition = hit.point + prev_up * smooth_y;

                    //forward velocity

                    m_rb.velocity = (Mathf.Abs(current_speed) < 0.1f ? 0 : Mathf.Lerp(m_rb.velocity.magnitude, current_speed, -smooth_accel / 3)) * direction;

                    animAcceleration = Mathf.Lerp(animAcceleration, SpeedInput, Time.deltaTime);
                }
            }
            else
            {
                flightManager(v);
            }
        }
    }
    
    private void FixedUpdate()
    {
        if (playerControlled)
        {
            turnManager();
        }
    }
    void flightManager(float v)
    {
        if (flying == false)
        {
            flyingTimer = Time.time;
            flying = true;
            smooth_angle2 = v * 1;
            next_speed = current_speed;
            extraFlightAngle = current_speed / hover_fwd_max_speed * 0.55f;
        }
        if (form)
        {
            hascontrol = true;
            if (smooth_accel < 0)
            {
                smooth_accel = 0;
            }
            else
            {
                smooth_accel += Time.deltaTime / 2;
            }
            smooth_angle += Time.deltaTime / 10;

            //hover_fall_up_angle + extraFlightAngle = 0.45 Maximum up Angle

            if (extraFlightAngle > 0.5f)
            {
                glide = true;
            }
            else if (extraFlightAngle < 0.1f)
            {
                glide = false;
            }

            if (v > 0.1f)
            {
                smooth_angle2 = Mathf.Lerp(smooth_angle2, hover_fall_down_angle, Time.deltaTime / (0.8f - v));
            }
            else if (v < -0.1f)
            {
                smooth_angle2 = Mathf.Lerp(smooth_angle2, hover_fall_up_angle + (glide ? extraFlightAngle : 0), Time.deltaTime / (0.8f + v));
            }
            else
            {
                smooth_angle2 = Mathf.Lerp(smooth_angle2, hover_fall_angle, Time.deltaTime / 0.4f);
            }
            if (smooth_angle2 > hover_fall_up_angle && glide == true)
            {
                extraFlightAngle = Mathf.Lerp(extraFlightAngle, 0, (Time.deltaTime / (1 - smooth_angle2)));
            }
            else
            {
                extraFlightAngle = Mathf.Lerp(extraFlightAngle, 0.75f, (Time.deltaTime / (0.8f + smooth_angle2)));
            }

            next_speed = Mathf.Lerp(next_speed, (0.7f - smooth_angle2) * 35, Time.deltaTime);

            Vector3 editedForward = transform.forward;
            if (transform.forward.y > 0.8f)
            {

                editedForward += transform.up / 10;
            }
            else if (transform.forward.y < -0.8f)
            {

                editedForward += transform.up / 10;
            }

            float angle = Vector3.Angle(Vector3.ProjectOnPlane(transform.up, Vector3.ProjectOnPlane(transform.forward, Vector3.up)), Vector3.up);
            angle = Mathf.Clamp(Mathf.Abs(angle - 180) / 180, 0.2f, 1);

            Vector3 desired_up;
            desired_up = Vector3.Lerp(transform.up, Vector3.Normalize(Vector3.ProjectOnPlane(editedForward, Vector3.up)) * (Mathf.Sqrt(3) / 2) + Vector3.up * 1.5f + transform.TransformDirection(0, 0, -smooth_angle2), Time.deltaTime / angle);

            Vector3 desired_forward = editedForward;

            //reduce forward.y
            desired_forward.y = Mathf.Lerp(transform.forward.y, smooth_angle2, Time.deltaTime / 0.2f);
            //desired_forward=new Vector3(transform.forward.x, Mathf.Lerp(transform.forward.y,-0.50f-(smooth_angle2/0.45f)*0.24f,Time.deltaTime/0.2f),transform.forward.z);

            Quaternion rot = Quaternion.LookRotation(desired_forward, desired_up);
            transform.rotation = rot;

            //change

            //m_rb.velocity = transform.forward * next_speed;

        }
        else
        {
            hascontrol = false;
            if (smooth_accel < 0)
            {
                smooth_accel = 0;
            }
            else
            {
                smooth_accel += Time.deltaTime / 2;
            }

            smooth_angle += Time.deltaTime / 10;

            Vector3 forwardRocketFallVel = Vector3.Normalize(Vector3.ProjectOnPlane(transform.forward, Vector3.up)) * 0.25f;
            Vector3 desired_forward = Vector3.Lerp(transform.forward, Vector3.Normalize(Vector3.down + forwardRocketFallVel), smooth_angle);
            //Then we get the angle that we have to rotate in quaternion format
            Quaternion tilt = Quaternion.FromToRotation(transform.forward, desired_forward);
            //Now we apply it to the ship with the quaternion product property

            transform.rotation = tilt * transform.rotation;

            if (transform.up.y < 0)
            {
                transform.RotateAround(transform.position, transform.forward, -Mathf.Sign(transform.right.y) * Time.deltaTime / 0.002f);
            }
            else if (Mathf.Abs(transform.right.y) > 0.1f)
            {
                transform.RotateAround(transform.position, transform.forward, -Mathf.Sign(transform.right.y) * Time.deltaTime / 0.002f);
            }

            //if transform.forward.y<0
            //transform.RotateAround (transform.position, transform.forward, Time.deltaTime * 180);

            next_speed = Mathf.Lerp(current_speed, 65, smooth_accel);

            //m_rb.velocity = transform.forwa rd * next_speed;


            RaycastHit rocketHit;

            if (flying && !form && Physics.Raycast(transform.position, transform.forward, out rocketHit, m_rb.velocity.magnitude / 6.5f, LayerMask.GetMask("Path")))
            {
                
                Vector3 desired_up = Vector3.Lerp(prev_up, rocketHit.normal, Time.deltaTime / 0.1f*((5-rocketHit.distance)*2));
                /*Then we get the angle that we have to rotate in quaternion format*/
                Quaternion rocketTilt = Quaternion.FromToRotation(transform.up, desired_up);
                /*Now we apply it to the ship with the quaternion product property*/
                transform.rotation = rocketTilt * transform.rotation;
                //Slow down player as necessary
            }
        }
        m_rb.velocity = transform.forward * next_speed;
    }
    void turnManager()
    {
        //Physical aspect

        if (!hascontrol)
        {
            HorizontalInput = 0;
        }

        if (form)
        {
            if (flying)
            {
                yaw = Mathf.Lerp(yaw, hover_turn_speed * rotation_help * HorizontalInput, (Time.deltaTime / hover_soap) * (1 + turn_variable / 100) * 2);
            }
            else
            {
                yaw = Mathf.Lerp(yaw, hover_turn_speed * rotation_help * HorizontalInput, Time.deltaTime / hover_soap) * (1 + turn_variable / 100);
            }
        }
        else
        {
            if (flying)
            {
                yaw = 0;
            }
            else
            {
                yaw = Mathf.Lerp(yaw, rocket_turn_speed * rotation_help * HorizontalInput, Time.deltaTime / rocket_soap) * (1 + turn_variable / 100);
            }
        }
        turnAngle = yaw * 75;

        float yaw_Increase;

        if (Mathf.Sign(HorizontalInput) == Mathf.Sign(turnAngle))
        {
            yaw_Increase = yaw * (Mathf.Abs(turnAngle / maxAngleChange * 0.5f));
        }
        else
        {
            yaw_Increase = yaw/2;
        }

        rotationforce = yaw + yaw_Increase;
        if (Mathf.Abs(rotationforce) <= 0.1f)
        {
            rotationforce = 0;
        }

        //Rotation Calculation
        transform.RotateAround(transform.position, transform.up, Mathf.Sign(transform.InverseTransformDirection(m_rb.velocity).z) * (rotationforce + (rotationforce < 0 ? -1 : (rotationforce > 0 ? 1 : 0)) * (SpeedInput < 0 ? (current_speed / rocket_fwd_max_speed) / 2 : 0)) * Time.deltaTime / 0.01f);

        //Visual Rotation
        animRotation = Mathf.Lerp(animRotation, HorizontalInput, Time.deltaTime);

        visuals.transform.rotation = transform.rotation;
        visuals.transform.RotateAround(transform.position, transform.up, 180);
        if (form)
        {
            tps.GetComponent<TPSCameraController>().tilt = Mathf.Lerp(tps.GetComponent<TPSCameraController>().tilt, -yaw_Increase * hover_tilt, Time.deltaTime);
            visuals.transform.RotateAround(visuals.transform.position, visuals.transform.forward, -tps.GetComponent<TPSCameraController>().tilt * 3f);
        }
        if (!form)
        {
            fps.GetComponent<FPSCamera>().tilt = Mathf.Lerp(fps.GetComponent<FPSCamera>().tilt, -yaw_Increase * rocket_tilt, Time.deltaTime);
            visuals.transform.RotateAround(visuals.transform.position, visuals.transform.forward, -fps.GetComponent<FPSCamera>().tilt * 1.5f);
        }
        prev_up = transform.up;

        direction = Quaternion.AngleAxis(-rotationforce * 60*drift, transform.up) * transform.forward;
    }

    private void OnCollisionStay(Collision col)
    {
        if (Vector3.Distance(transform.position, lastPos) / Time.deltaTime < 10)
        {
            current_speed = Vector3.Magnitude(Vector3.Project(transform.position - lastPos, transform.forward)) / Time.deltaTime;
        }
        lastPos = transform.position;

        Vector3 normal = Vector3.zero;
        Vector3 hitpos = Vector3.zero;

        foreach (ContactPoint cp in col.contacts)
        {
            normal += cp.normal;
            hitpos += transform.InverseTransformPoint(cp.point);
        }
        if (Vector3.Dot(transform.forward, normal) < 0)
        {
            Vector3 desired_up = transform.up;
            Vector3 desired_forward = (hitpos.x < 0 ? 1 : -1) * Vector3.Cross(normal, transform.up);

            if (transform.InverseTransformVector(desired_forward).x < 0 ? HorizontalInput <= 0 : HorizontalInput >= 0)
            {
                transform.forward = Vector3.Lerp(transform.forward, desired_forward, Time.deltaTime / 0.2f);
                Quaternion rot = Quaternion.LookRotation(transform.forward, desired_up);
                transform.rotation = rot;
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        Vector3 hitpos = transform.InverseTransformPoint(collision.contacts[0].point);
        if (Mathf.Abs(hitpos.x) > Mathf.Abs(hitpos.z))
        {
            
            if (hitpos.x > 0)
            {
                hitAnim = 3;
            }
            else
            {
                hitAnim = 4;
            }
        }
        else
        {
            if (hitpos.z > 0)
            {
                hitAnim = 1;
            }
            else
            {
                hitAnim = 1;
            }
        }
    }

    IEnumerator ActivateInteraction()
    {
        for (; ; )
        {
            if (targetInteraction != null)
            {
                if (Vector3.Distance(transform.position, targetInteraction.transform.position) < 10)
                {
                    targetInteraction.gameObject.BroadcastMessage("Activate", this.gameObject);
                    targetInteraction = null;
                }
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    IEnumerator CheckTargets()
    {
        for(;;){
            potentialInteraction =targetingScript.GetValidTarget(this.gameObject);
            yield return new WaitForSeconds(0.2f);
        }
    }

    //VALUES
    public float SpeedPercentage()
    {
        float percentage;
        percentage = Mathf.Clamp(current_speed / rocket_fwd_max_speed,0,1);
        return percentage;
    }
}
