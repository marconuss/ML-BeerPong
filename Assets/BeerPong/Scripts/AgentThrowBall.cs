using System;
using System.Threading.Tasks.Sources;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.VisualScripting;
using UnityEngine;

public class AgentThrowBall : Agent
{
    [Tooltip("Whether it is gameplay mode or training mode")]
    public bool trainingMode;

    [Tooltip("Target beer cups")] [SerializeField]
    private BeerCups beerCups;

    [SerializeField] private float timeBetweenDecisionsAtInference;

    [SerializeField] private Vector3 initialPosition;

    [SerializeField] private Quaternion initialRotation;

    // current beerCup the agent is aiming at
    private BeerCup _aimedBeerCup;

    // time since the last decision of the agent
    private float _timeSinceDecision;

    // keep track whether the ball has been thrown or not
    private bool _isBallThrown = false;

    // Rigidbody of the ball
    private Rigidbody _ballRigidbody;

    // limit the pitch for the throw
    private const float MaxBallPitch = 30f;

    // limit the yaw for the throw
    private const float MaxBallYaw = 20f;

    // limit the force for the throw
    private const float MaxThrowForce = 3f;
    private const float BaseThrowForce = 1f;

    // throw variables
    private float _pitch;
    private float _yaw;
    private float _throwForce;

    // maximum distance between the ball and the beer cup, used for normalizing the distance
    private const float MaxCupDistance = 3.0f;

    public override void Initialize()
    {
        //deactivate the rigidbody so it doesn't fall at start
        _ballRigidbody = GetComponent<Rigidbody>();
        _ballRigidbody.isKinematic = true;

        // If not in training mode, play forever, no max step
        if (!trainingMode)
        {
            MaxStep = 0;
        }
    }

    void Start()
    {
        _pitch = 0f;
        _yaw = 0f;
        _throwForce = BaseThrowForce;
        _ballRigidbody = GetComponent<Rigidbody>();

        ResetBall();

        UpdateAimedBeerCup();
    }

    public override void OnEpisodeBegin()
    {
        ResetBall();
        UpdateAimedBeerCup();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        var cupPosition = _aimedBeerCup.transform.position;
        var ballPosition = transform.position;

        // normalized distance to the aimed beer cup (1 observation)
        sensor.AddObservation(Vector3.Distance(cupPosition, ballPosition) / MaxCupDistance);

        // rotation of the ball representing the aim (3 observations)
        sensor.AddObservation(transform.forward);

        // 4 total observation
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        Debug.Log("OnActionReceived");
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
    }

    public void ThrowBall()
    {
        _isBallThrown = true;
        _ballRigidbody.isKinematic = false;
        DrawTrajectory.Instance.IsVisible = false;
        _ballRigidbody.AddForce(_throwForce * transform.forward);
    }

    private void ResetBall()
    {
        // reset throw variables
        _isBallThrown = false;
        DrawTrajectory.Instance.IsVisible = true;

        // reset rigidbody
        _ballRigidbody.velocity = Vector3.zero;
        _ballRigidbody.angularVelocity = Vector3.zero;
        _ballRigidbody.isKinematic = true;

        // reset position
        transform.position = initialPosition;
        transform.rotation = initialRotation;
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

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("boundary"))
        {
            ResetBall();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("beer"))
        {
            _aimedBeerCup.GotHit();
            ResetBall();
        }
    }

    private void Update()
    {
        if (trainingMode)
        {
            WaitBeforeThrowing();
        }

        if (!_isBallThrown)
        {
            // Update the rotation of the ball
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0);
            transform.rotation = rotation;

            // Draw trajectory line
            DrawTrajectory.Instance.Draw(transform.forward, _throwForce, _ballRigidbody, initialPosition);
        }

        if (Input.GetButtonDown("Debug Reset"))
        {
            ResetBall();
            beerCups.ResetCups();
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
            //Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0);
            //transform.rotation = rotation;
            ThrowBall();
        }
        else
        {
            _timeSinceDecision += Time.fixedDeltaTime;
        }
    }

    private void FixedUpdate()
    {
        //avoids scenario where the nearest flower nectar is taken by another agent and not updated
        if (!ReferenceEquals(_aimedBeerCup, null))
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
        _throwForce = force * MaxThrowForce + BaseThrowForce;
    }
    
}