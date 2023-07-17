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

    private int collisionCounter;

    public override void Initialize()
    {
        //deactivate the rigidbody so it doesn't fall at start
        _ballRigidbody = GetComponent<Rigidbody>();
        _ballRigidbody.isKinematic = true;
        _pitch = 0f;
        _yaw = 0f;
        _throwForce = BaseThrowForce;

        collisionCounter = 0;
        // If not in training mode, play forever, no max step
        if (!trainingMode)
        {
            MaxStep = 0;
        }
    }


    public override void OnEpisodeBegin()
    {
        if (trainingMode)
        {
            _pitch = 0f;
            _yaw = 0f;
            _throwForce = BaseThrowForce;
        }

        ResetBall();
        UpdateAimedBeerCup();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // if aimed cup is null, observe an empty array and return early
        if (_aimedBeerCup == null)
        {
            sensor.AddObservation(new float[4]);
            return;
        }

        var cupPosition = _aimedBeerCup.transform.position;
        var ballPosition = transform.position;

        Vector3 distanceVector = cupPosition - ballPosition;


        // normalized distance to the aimed beer cup (1 observation)
        sensor.AddObservation(distanceVector.magnitude / MaxCupDistance);

        // distance vector from the ball to the cup (3 observations)
        sensor.AddObservation(Vector3.Normalize(distanceVector));

        // 4 total observation
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // if the ball has been thrown, return early
        if (_isBallThrown) return;

        var action = actions.ContinuousActions;

        float pitchChange = action[0];
        float yawChange = action[1];
        float forceChange = action[2];

        UpdatePitch(pitchChange);
        UpdateYaw(yawChange);
        UpdateForce(forceChange);

        // Update the rotation of the ball
        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0);
        transform.rotation = rotation;

        Debug.Log($" Pitch: {_pitch} Yaw: {_yaw} Force: {_throwForce}");

        ThrowBall();
    }

    
    /// <summary>
    /// because the manual control is done with UI buttons,
    /// the Heuristic method just updates the actions with the current values
    /// </summary>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        Debug.Log("Heuristic!");
        var continuousActionsOut = actionsOut.ContinuousActions;

        continuousActionsOut[0] = _pitch;
        continuousActionsOut[1] = _yaw;
        continuousActionsOut[2] = _throwForce;
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
        collisionCounter = 0;
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

        // reset the ball if it hits the boundary or the ground
        if (other.gameObject.CompareTag("boundary"))
        {
            Debug.Log("Hit the boundary!");
            // increase the collision counter, if it is greater than 1, reset the ball
            // this allows the ball to bounce once before resetting
            collisionCounter++;
            if (collisionCounter > 1)
            {
                ResetBall();
                AddReward(-0.1f);
            }
        }

        // if the ball hits the beer cup, give a small reward, you deserve it 
        if (other.gameObject.CompareTag("beerCup"))
        {
            Debug.Log("Hit the beer cup!");
            AddReward(+0.1f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // if the ball hits the beer cup, give a large reward and reset the ball
        if (other.gameObject.CompareTag("beer"))
        {
            _aimedBeerCup.GotHit();
            // if the collision counter is 1, give a bigger reward
            if (collisionCounter == 1)
            {
                Debug.Log("Hit the cup with bounce!");
                AddReward(1f);
            }
            else
            {
                AddReward(0.8f);
            }
            ResetBall();
        }
    }

    private void Update()
    {
        if (!_isBallThrown)
        {
            // update ball rotation
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0);
            transform.rotation = rotation;
            
            // Draw trajectory line
            DrawTrajectory.Instance.Draw(transform.forward, _throwForce, _ballRigidbody, initialPosition);
        }
        
        // manual reset if something goes wrong
        if (Input.GetButtonDown("Debug Reset"))
        {
            ResetBall();
            beerCups.ResetCups();
        }

        if (Input.GetButtonDown("Jump"))
        {
            if (!trainingMode)
            {
                RequestDecision();
            }
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
            RequestDecision();
            //Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0);
            //transform.rotation = rotation;
            //ThrowBall();
        }
        else
        {
            _timeSinceDecision += Time.fixedDeltaTime;
        }
    }

    private void FixedUpdate()
    {
        if (trainingMode)
        {
            WaitBeforeThrowing();
        }

        //avoids scenario where the nearest flower nectar is taken by another agent and not updated
        if (!ReferenceEquals(_aimedBeerCup, null))
        {
            UpdateAimedBeerCup();
        }
    }

    public void UpdatePitch(float pitch)
    {
        pitch = Mathf.Clamp(pitch, -1, 1);
        _pitch = pitch * MaxBallPitch;
    }

    public void UpdateYaw(float yaw)
    {
        yaw = Mathf.Clamp(yaw, -1, 1);
        _yaw = yaw * MaxBallYaw;
    }

    public void UpdateForce(float force)
    {
        force = Mathf.Clamp(force, 0, 1);
        _throwForce = force * MaxThrowForce + BaseThrowForce;
    }
}