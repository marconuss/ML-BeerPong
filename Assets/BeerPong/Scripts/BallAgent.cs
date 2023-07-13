using System;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Machine Learning Agent
/// </summary>

public class BallAgent : Agent
{
    /// <summary>
    /// Is it the agent's turn to throw the ball
    /// </summary>
    [HideInInspector]
    public bool agentTurn = false;
    
    //  ---------------------------------------
    //  Serialized Fields
    //  ---------------------------------------
    [Tooltip("Whether it is gameplay mode or training mode")] [SerializeField]
    private bool trainingMode;

    [Tooltip("The initial position of the agent")] [SerializeField]
    private Vector3 initialPosition;
    
    [Tooltip("Target beer cup")] [SerializeField]
    private BeerCups beerCups;

    //  ---------------------------------------
    // Private Variables
    //  ---------------------------------------

    // Rigidbody of the ball
    private Rigidbody _rigidbody;

    // current beerCup the agent is aiming at
    private BeerCup _aimedBeerCup;
    
    public float _ballPitch;
    public float _ballYaw;
    public float _launchForce;
    
    private const float _maxBallPitch = 45f;
    private const float _maxBallYaw = 45f;
    private const float _maxLaunchForce = 100f;
    
    // the nearest beer cup to the agent
    //private BeerCup _nearestBeerCup;

    //  ---------------------------------------
    //  Functions
    //  ---------------------------------------

    /// <summary>
    /// Number of beer cups hit
    /// </summary>
    public int BeerCupsHit { get; private set; }

    // ---------------------------------------
    // ML Agents Functions (Override)
    // ---------------------------------------

    /// <summary>
    /// Initialize the agent
    /// </summary>
    public override void Initialize()
    {
        _rigidbody = GetComponent<Rigidbody>();
        
        //deactivate the rigidbody so it doesn't fall over
        _rigidbody.isKinematic = true;

        // If not in training mode, play forever, no max step
        if (!trainingMode)
        {
            MaxStep = 0;
        }
    }

    /// <summary>
    /// Reset the agent when an episode begins
    /// </summary>
    public override void OnEpisodeBegin()
    {
        BeerCupsHit = 0;
        StartTurn();
        if (trainingMode)
        {
            beerCups.ResetCups();
        }
        agentTurn = true;
    }

    /// <summary>
    /// Collect the observations used by the agent to make decisions
    /// </summary>
    /// <param name="sensor"></param>

    public override void CollectObservations(VectorSensor sensor)
    {
        
        // if aimed beer cup is null, observe an empty array and return early
        if(_aimedBeerCup == null)
        {
            sensor.AddObservation(new float[4]);
            return;
        }
        
        //sensor.AddObservation(toBeerCup.normalized);

        // Observe the agent's pitch (1 observation)
        sensor.AddObservation(_ballPitch / _maxBallPitch);
        
        // Observe the agent' yaw (1 observation)
        sensor.AddObservation(_ballYaw / _maxBallYaw);
        
        // Observe the agent's force (1 observation)
        sensor.AddObservation(_launchForce / _launchForce);

        // Observe the agent's distance to the beer cup (1 observation)
        sensor.AddObservation(Vector3.Distance(this.transform.localPosition, _aimedBeerCup.CupPosition));

        // 4 observations total 
    }

    /// <summary>
    /// Called when an action is received from either the player input or the neural network
    ///
    /// vectorAction[i] represents:
    /// Index 0 : x direction (+1 = right, -1 = left)
    /// Index 1 : y direction (+1 = up, -1 = down)
    /// Index 2 : force of the throw
    /// 
    /// </summary>
    /// <param name="actions">The action to take</param>

    public override void OnActionReceived(ActionBuffers actions)
    {
        if(!agentTurn) return;

        var continuousActions = actions.ContinuousActions;

        _ballPitch = continuousActions[0];
        _ballYaw = continuousActions[1];
        _launchForce = continuousActions[2];
        

        Vector3 lanchDirection = new Vector3(continuousActions[0], continuousActions[1], 0);
        
        // launch the ball
        //_rigidbody.WakeUp();
        //if (!_rigidbody.isKinematic)
        //{
        //    _rigidbody.AddForce(lanchDirection * _launchForce * 100f);
        //}

        _ballPitch = Mathf.Clamp(_ballPitch, -_maxBallPitch, _maxBallPitch);
        _ballYaw = Mathf.Clamp(_ballYaw, -_maxBallYaw, _maxBallYaw);
        _launchForce = Mathf.Clamp(_launchForce, 0f, _maxLaunchForce);
        
        // apply the rotation
        transform.rotation = Quaternion.Euler(_ballPitch, _ballYaw, 0f);
        
        EndTurn();

    }

    /// <summary>
    /// When behaviour Type is set to "Heuristic Only" on the agent's Behaviour Parameters,
    /// this function will be called. Its return values will be fed into
    /// <see cref="OnActionReceived(ActionBuffers)"/> instead of using the neural network
    /// </summary>
    /// <param name="actionsOut">an output action array</param>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;

        // Get the user input
        if (Input.GetKey(KeyCode.UpArrow))
        {
            _ballPitch = 1f;
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            _ballPitch = -1f;
        }

        // Turn left/right
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            _ballYaw = -1f;
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            _ballYaw = 1f;
        }
        
        // Throw the ball
        if (Input.GetKey(KeyCode.Space))
        {
            Debug.Log("Heuristic staff");
            _launchForce = 1f;
            _rigidbody.isKinematic = false;
            RequestDecision();
            continuousActionsOut[0] = _ballPitch;
            continuousActionsOut[1] = _ballYaw;
            continuousActionsOut[2] = _launchForce;
        }
        else
        {
            
            continuousActionsOut[0] = 0f;
            continuousActionsOut[1] = 0f;
            continuousActionsOut[2] = 0f;
        }
    }

    /// <summary>
    /// Start the turn
    /// </summary>
    private void StartTurn()
    {
        _rigidbody.isKinematic = true;

        ResetBallPosition();
        UpdateBeerAim();
    }

    /// <summary>
    /// End the turn
    /// </summary>
    private void EndTurn()
    {
        if (agentTurn)
        {
            agentTurn = false;
        }
        StartTurn();
    }

    /// <summary>
    /// Resetting the ball position
    /// </summary>
    private void ResetBallPosition()
    {
        _rigidbody.velocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.isKinematic = true;
        transform.position = initialPosition;
    }
    
    /// <summary>
    /// Check if the ball hit a beer cup
    /// </summary>
    /// <param name="other"></param>

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("beer")) return;
        
        BeerCup beerCup = beerCups.GetBeerCup(other);
        
        if(beerCup != null)
        {
            beerCup.GotHit();
            beerCups.BeerCupsList.Remove(beerCup);
            if(beerCups.BeerCupsList.Count == 0)
            {
                Debug.Log("All cups hit");
                AddReward(1f);
                EndEpisode();
            }
            else
            {
                Debug.Log("Cup hit");
                UpdateBeerAim();
                AddReward(0.1f);
                EndTurn();
            }
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        Debug.Log("Wall hit");
        AddReward(-0.1f);
        EndTurn();
    }

    /// <summary>
    /// Update the beer cup to aim at
    /// </summary>
    private void UpdateBeerAim()
    {
        if (beerCups.BeerCupsList.Count < 1) return;
        // get the next beer cup to aim at
        if (ReferenceEquals(_aimedBeerCup, null) || _aimedBeerCup.IsHit)
        {
            _aimedBeerCup = beerCups.BeerCupsList[UnityEngine.Random.Range(0, beerCups.BeerCupsList.Count-1)];
        }
    }
    
    
    /// <summary>
    /// called every frame
    /// </summary>
    private void Update()
    {
        // draw a line from beak tip to the nearest flower
        if(!ReferenceEquals(_aimedBeerCup, null))
        {
            Debug.DrawLine(transform.position, _aimedBeerCup.CupPosition, Color.green);
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
            UpdateBeerAim();
        }
    }
    
    /// <summary>
    /// Debug drawing of the ray
    /// </summary>
    private void OnDrawGizmos()
    {
        // Assuming you have a reference to the ball's transform
        Vector3 ballDirection = transform.forward;
        float rayLength = 1f; // Length of the debug ray
        
        // Draw the debug ray
        Debug.DrawRay(transform.position, ballDirection * rayLength, Color.red);
    }
    
}
