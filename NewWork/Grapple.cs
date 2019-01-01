using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grapple : Interactions {

    public Transform endpoint;

    public Color color = Color.white;
    public float width = 0.2f;
    public int numberOfPoints = 50;
    LineRenderer lineRenderer;

    public float rotationMultiplier;

    bool activated = false;
    bool calculating = false;
    bool recalculate=false;
    GameObject activePlayer;

    Vector3[] positions;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.material = new Material(Shader.Find("Particles/Additive"));
    }

    public override void Effect(GameObject player)
    {
        activated = true;
        calculating = true;
        activePlayer = player;
    }
    private void Update()
    {
        if (activated)
        {
            if (calculating)
            {
                Vector3[] controlPoints = CalculateMainPoints(activePlayer);
                positions = new Vector3[(controlPoints.Length-1) * numberOfPoints];

                if (null == lineRenderer || controlPoints == null || controlPoints.Length < 2)
                {
                    return; // not enough points specified
                }

                // update line renderer
                lineRenderer.startColor = color;
                lineRenderer.endColor = color;
                lineRenderer.startWidth = width;
                lineRenderer.endWidth = width;
                if (numberOfPoints < 2)
                {
                    numberOfPoints = 2;
                }
                lineRenderer.positionCount = numberOfPoints * (controlPoints.Length - 1);

                // loop over segments of spline
                Vector3 p0, p1, m0, m1;
                for (int j = 0; j < controlPoints.Length - 1; j++)
                {
                    // check control points
                    if (controlPoints[j] == null || controlPoints[j + 1] == null ||
                        (j > 0 && controlPoints[j - 1] == null) ||
                        (j < controlPoints.Length - 2 && controlPoints[j + 2] == null))
                    {
                        Debug.Log("null");
                        return;
                    }
                    // determine control points of segment
                    p0 = controlPoints[j];
                    p1 = controlPoints[j + 1];

                    if (j > 0)
                    {
                        m0 = 0.5f * (controlPoints[j + 1] - controlPoints[j - 1]);
                    }
                    else
                    {
                        m0 = activePlayer.GetComponent<Rigidbody>().velocity.magnitude * activePlayer.transform.forward * 2;
                    }
                    if (j < controlPoints.Length - 2)
                    {
                        m1 = 0.5f * (controlPoints[j + 2] - controlPoints[j]);
                    }
                    else
                    {
                        m1 = controlPoints[j + 1] - controlPoints[j];
                    }

                    // set points of Hermite curve
                    Vector3 position;
                    float t;
                    float pointStep = 1.0f / numberOfPoints;

                    if (j == controlPoints.Length - 2)
                    {
                        pointStep = 1.0f / (numberOfPoints - 1.0f);
                        // last point of last segment should reach p1
                    }
                    for (int i = 0; i < numberOfPoints; i++)
                    {
                        t = i * pointStep;
                        position = (2.0f * t * t * t - 3.0f * t * t + 1.0f) * p0
                            + (t * t * t - 2.0f * t * t + t) * m0
                            + (-2.0f * t * t * t + 3.0f * t * t) * p1
                            + (t * t * t - t * t) * m1;


                        Vector3 newPosition = ProjectPositionOnSphere(position, activePlayer, i + j * numberOfPoints, controlPoints.Length - 1);
                        float a = (i + j * numberOfPoints);
                        float b = ((controlPoints.Length - 1) * numberOfPoints);


                        if (i > 1)
                        {
                            if (newPosition.y > transform.position.y )
                            {

                                Vector3 reflectCenter = endpoint.position;

                                Vector3 normal = reflectCenter - Vector3.Lerp(positions[0], transform.position, ((i + j * (float)numberOfPoints) / (((controlPoints.Length - 1) * (float)numberOfPoints)/2)));
                                normal = Vector3.Normalize(normal);
                                Vector3 inDirection = reflectCenter - newPosition;

                                Vector3 reflectAngle = Vector3.Normalize(Vector3.Reflect(inDirection, normal));
                               
                                newPosition = reflectCenter + reflectAngle * Vector3.Distance(newPosition, reflectCenter);
                                

                            }
                        }

                        lineRenderer.SetPosition(i + j * numberOfPoints, newPosition);
                        positions[i + j * numberOfPoints]=newPosition;

                    }
                }
                calculating = false;
                activePlayer.GetComponent<PlayerController>().playerControlled = false;
                StartCoroutine("Swing");
            }
        }
    }

    IEnumerator Swing()
    {
        int i= 1;
        float startSpeed;
        if (!activePlayer.GetComponent<PlayerController>().flying)
        {
            startSpeed = activePlayer.GetComponent<PlayerController>().current_speed;
        }
        else
        {
            startSpeed = activePlayer.GetComponent<PlayerController>().next_speed;
        }
        activePlayer.GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);

        while (activated)
        {
            bool playerfocused = true;
            
            /*float distance = (1 / (Vector3.Distance(activePlayer.transform.position, positions[i]) / activePlayer.GetComponent<PlayerController>().current_speed)) * Time.deltaTime;
            distance *=  Vector3.Distance(activePlayer.transform.position, positions[i]);*/

            float distance = activePlayer.GetComponent<PlayerController>().current_speed * Time.deltaTime; //Distance per frames in m

            while (distance > Vector3.Distance(playerfocused ? activePlayer.transform.position : positions[i - 1], positions[i]))
            {

                if (i < positions.Length - 1)
                {
                    distance -= Vector3.Distance(playerfocused ? activePlayer.transform.position : positions[i - 1], positions[i]);
                    i++;
                    playerfocused = false;
                }
                else
                {
                    activePlayer.GetComponent<PlayerController>().playerControlled = true;
                    activePlayer.transform.position = positions[i];
                    activePlayer.GetComponent<Rigidbody>().velocity = activePlayer.transform.forward * startSpeed;
                    
                    yield break;
                }
            }

            distance /= Vector3.Distance(positions[i - 1], positions[i]);



            Vector3 desiredPos= Vector3.Lerp(playerfocused? activePlayer.transform.position:positions[i-1], positions[i],distance);

            int layerMask =~ LayerMask.GetMask("Player");

            if (Physics.CheckSphere(desiredPos,1,layerMask)) {
                
                calculating = true;
                activePlayer.GetComponent<PlayerController>().playerControlled = true;
                activePlayer.GetComponent<Rigidbody>().velocity = activePlayer.transform.forward * startSpeed;
                yield break;
            }

            activePlayer.transform.position = desiredPos;
            if (desiredPos.y < transform.position.y && recalculate)
            {
                calculating = true;
                yield break;
            }
            if (i < positions.Length - 1)
            {
               
                Vector3 direction = positions[i + 1] - activePlayer.transform.position;
                Vector3 up = transform.position - activePlayer.transform.position;
                Quaternion toRotation = Quaternion.LookRotation(direction,up);
                activePlayer.transform.rotation = Quaternion.Lerp(activePlayer.transform.rotation, toRotation, Time.deltaTime*rotationMultiplier);
            }

            yield return null;
        }
    }

    private Vector3 ProjectPositionOnSphere(Vector3 position,GameObject player,float currentpoint,float mainpoints)
    {
        Vector3 CenterPos;
        CenterPos = transform.position;
        /*if (position.y - transform.position.y > 0)
        {
            CenterPos.y = position.y;
        }*/
        Vector3 p = new Vector3(position.x - CenterPos.x, position.y - CenterPos.y, position.z - CenterPos.z);
        float pLength = Mathf.Sqrt(p.x * p.x + p.y * p.y + p.z * p.z);
        float targetRadius = Mathf.Lerp(Vector3.Distance(CenterPos, player.transform.position), Vector3.Distance(CenterPos, endpoint.position),(currentpoint)/(numberOfPoints* mainpoints));
        Vector3 targetScale = (targetRadius / pLength) * p;
        Vector3 newp = targetScale + CenterPos;
        return newp;
    }

    private Vector3[] CalculateMainPoints(GameObject player)
    {
        Vector3[] p;
       
            p = new Vector3[2];
            p[0] = player.transform.position;
            p[1] = endpoint.position;

        /*if (Vector3.Angle(player.transform.position - transform.position, new Vector3(endpoint.position.x, transform.position.y, endpoint.position.z) - transform.position) < 90)
        {
            p = new Vector3[4];
            p[0] = player.transform.position;
            p[3] = endpoint.position;

            Vector3 firstSymmetry = 2 * (new Vector3(transform.position.x, p[0].y, transform.position.z) - p[0]);
            p[1] = p[0] + firstSymmetry;
            p[2] = p[3] + Vector3.Normalize(new Vector3(transform.position.x, (p[0].y + p[3].y) / 2, transform.position.z) - p[3]) * firstSymmetry.magnitude;
        }
        else
        {
        }*/
        return p;
    }

    public override void ChronoEffect()
    {
        Debug.Log("NoChrono");
    }
}
