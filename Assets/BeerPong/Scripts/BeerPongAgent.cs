using System;
using System.Threading.Tasks.Sources;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;

public class BeerPongAgent : Agent
{
    [Tooltip("Whether it is gameplay mode or training mode")]
    public bool trainingMode;

    [Tooltip("Speed to pitch up or down")] public float pitchSpeed = 100f;

    [Tooltip("Speed to rotate around the up axis")]
    public float yawSpeed = 100f;

    [Tooltip("Speed to change the force of the throw")]
    public float forceSpeed = 100f;

    public float timeBetweenDecisionsAtInference;

    [Tooltip("Target beer cups")] [SerializeField]
    private BeerCups beerCups;

    [SerializeField] private Vector3 initialPosition;

    [SerializeField] private Quaternion initialRotation;

    // current beerCup the agent is aiming at
    private BeerCup _aimedBeerCup;

    // time since the last decision of the agent
    private float _timeSinceDecision;

    // keep track whether the ball has been thrown or not
    private bool _isBallThrown;

    // Rigidbody of the ball
    private Rigidbody _ballRigidbody;

    [SerializeField]
    // horizontal direction of the throw
    private float _pitch;

    [SerializeField]
    // vertical direction of the throw
    private float _yaw;

    [SerializeField]
    // force of the throw
    private float _throwForce;

    // maximum distance between the ball and the beer cup, used for normalizing the distance
    private const float MaxCupDistance = 3.0f;

    private const float MaxBallPitch = 20f;

    private const float MaxBallYaw = 20f;

    private const float MaxLaunchForce = 0.1f;

    public override void Initialize()
    {
        _ballRigidbody = GetComponent<Rigidbody>();

        //deactivate the rigidbody so it doesn't fall at start
        _ballRigidbody.isKinematic = true;

        // If not in training mode, play forever, no max step
        if (!trainingMode)
        {
            MaxStep = 0;
        }
    }

    private void Start()
    {
        UpdateAimedBeerCup();
        ResetBall();
    }

    public override void OnEpisodeBegin()
    {
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        var CupPosition = _aimedBeerCup.transform.position;

        // normalized distance to the aimed beer cup (1 observation)
        sensor.AddObservation(Vector3.Distance(CupPosition, transform.position) / MaxCupDistance);

        // direction to the aimed beer cup (3 observations)
        sensor.AddObservation(CupPosition - transform.position);

        // 4 total observation
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        Debug.Log("OnActionReceived");
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
    }

    private void UpdateAimedBeerCup()
    {
        if (beerCups.BeerCupsList.Count < 1)
        {
            return;
        }

        // get the next beer cup to aim at
        if (ReferenceEquals(_aimedBeerCup, null) || _aimedBeerCup.IsHit)
        {
            _aimedBeerCup = beerCups.BeerCupsList[UnityEngine.Random.Range(0, beerCups.BeerCupsList.Count - 1)];
        }
    }

    private void ThrowBall(Vector3 direction, float force)
    {
        // throw the ball
        _ballRigidbody.isKinematic = false;
        _ballRigidbody.AddForce(direction * force, ForceMode.Impulse);

        _isBallThrown = true;
    }

    private void ResetBall()
    {
        // reset throw variables
        //_throwDirection = Vector3.zero;
        //_throwForce = 0;
        _isBallThrown = false;

        // reset rigidbody
        _ballRigidbody.velocity = Vector3.zero;
        _ballRigidbody.angularVelocity = Vector3.zero;
        _ballRigidbody.isKinematic = true;

        // reset position
        transform.position = initialPosition;
        transform.rotation = initialRotation;
    }

    private void OnCollisionEnter(Collision other)
    {
        if (!other.gameObject.CompareTag("beer"))
        {
            ResetBall();
        }
    }

    private void Update()
    {
        if (trainingMode)
        {
            WaitBeforeThrowing();
        }
        else
        {
            if (!_isBallThrown)
            {
                Quaternion rotation = Quaternion.Euler(0, _yaw, _pitch);
                Vector3 throwDirection = rotation * transform.forward;

                //DrawTrajectory
                // Calculate the trajectory points using the TrajectoryCalculator class
                //Vector3[] trajectoryPoints = DrawTrajectory.Instance.CalculateTrajectoryPoints(transform.position, throwDirection, _throwForce);

                // Update the line renderer
                //UpdateLineRenderer(trajectoryPoints);
                
                if (Input.GetButtonDown("Jump"))
                {
                    Debug.Log("throwing ball");
                    
                    //RequestDecision();
                    ThrowBall(throwDirection, _throwForce);
                }

                if (Input.GetButtonDown("Debug Reset"))
                {
                    Debug.Log("resetting ball");
                    ResetBall();
                }
            }
        }
    }
    
    private void UpdateLineRenderer(Vector3[] trajectoryPoints)
    {
        // Set the number of points for the line renderer
        DrawTrajectory.Instance.lineRenderer.positionCount = trajectoryPoints.Length;

        // Set the positions of the line renderer to the trajectory points
        for (int i = 0; i < trajectoryPoints.Length; i++)
        {
            DrawTrajectory.Instance.lineRenderer.SetPosition(i, trajectoryPoints[i]);
        }
    }


    private void WaitBeforeThrowing()
    {
        if (_isBallThrown)
        {
            return;
        }

        if (_timeSinceDecision >= timeBetweenDecisionsAtInference)
        {
            _timeSinceDecision = 0f;
            //RequestDecision();
            Quaternion rotation = Quaternion.Euler(0, _yaw, _pitch);
            Vector3 throwDirection = rotation * Vector3.forward;
            ThrowBall(throwDirection, _throwForce);
        }
        else
        {
            _timeSinceDecision += Time.fixedDeltaTime;
        }
    }
    
    /// <summary>
    /// called every 0.02 seconds
    /// </summary>
    private void FixedUpdate()
    {
        //avoids scenario where the nearest flower nectar is taken by another agent and not updated
        if(!ReferenceEquals(_aimedBeerCup, null))
        {
            UpdateAimedBeerCup();
        }
    }

    public void UpdatePitch(float pitch)
    {
        _pitch = pitch * MaxBallPitch;
    }

    public void UpdateYaw(float yaw)
    {
        _yaw = yaw * MaxBallYaw;
    }

    public void UpdateForce(float force)
    {
        _throwForce = force * MaxLaunchForce;
    }

    private void OnDrawGizmos()
    {
        // Assuming you have a reference to the ball's transform
        Quaternion rotation = Quaternion.Euler(0, _yaw, _pitch);
        Vector3 throwDirection = rotation * transform.forward;
        float rayLength = 1f; // Length of the debug ray

        // Draw the debug ray
        Debug.DrawRay(transform.position, throwDirection * rayLength, Color.red);
    }
}