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
    public float ballPitch;
    public float ballYaw;
    public float launchForce;
    
    [FormerlySerializedAs("agentTurn")] [HideInInspector]
    public bool isAgentTurn = false;
    
    [Tooltip("Whether it is gameplay mode or training mode")] [SerializeField]
    private bool trainingMode;

    [Tooltip("The initial position of the agent")] [SerializeField]
    private Vector3 initialPosition;
    
    [Tooltip("Target beer cup")] [SerializeField]
    private BeerCups beerCups;
    
    // Rigidbody of the ball
    private Rigidbody _rigidbody;

    // current beerCup the agent is aiming at
    private BeerCup _aimedBeerCup;
    
    private const float MaxBallPitch = 45f;
    private const float MaxBallYaw = 45f;
    private const float MaxLaunchForce = 100f;
    
    

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
        StartTurn();
        if (trainingMode)
        {
            beerCups.ResetCups();
        }
        isAgentTurn = true;
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
        sensor.AddObservation(ballPitch / MaxBallPitch);
        
        // Observe the agent' yaw (1 observation)
        sensor.AddObservation(ballYaw / MaxBallYaw);
        
        // Observe the agent's force (1 observation)
        sensor.AddObservation(launchForce / MaxLaunchForce);

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
        if(!isAgentTurn) return;

        var continuousActions = actions.ContinuousActions;
        
        float pitchChange = Mathf.Clamp(continuousActions[0], -1f, 1f);
        float yawChange = Mathf.Clamp(continuousActions[1], -1f, 1f);
        float forceChange = Mathf.Clamp(continuousActions[2], -1f, 1f);

        ballPitch += pitchChange * MaxBallPitch;
        ballYaw += yawChange * MaxBallYaw;
        launchForce += forceChange * MaxLaunchForce;
        
        // apply the rotation
        transform.rotation = Quaternion.Euler(ballPitch, ballYaw, 0f);
        _rigidbody.AddForce(transform.forward * launchForce * 100f, ForceMode.Impulse);
        
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
        

        // pitch
        continuousActionsOut[0] = Input.GetAxis("Vertical");
        // yaw
        continuousActionsOut[1] = Input.GetAxis("Horizontal");
        // force
        continuousActionsOut[2] = Input.GetAxis("Mouse ScrollWheel");
        
        if(Input.GetButtonDown("Jump"))
        {
            
        }
        else  
        {
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
        if (isAgentTurn)
        {
            isAgentTurn = false;
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
