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
    public bool isTurn = false;
    
    //  ---------------------------------------
    //  Serialized Fields
    //  ---------------------------------------
    [Tooltip("Whether it is gameplay mode or training mode")] [SerializeField]
    private bool trainingMode;

    [Tooltip("The initial position of the agent")] [SerializeField]
    private Vector3 initialPosition;

    //  ---------------------------------------
    // Private Variables
    //  ---------------------------------------

    // Rigidbody of the ball
    private Rigidbody _rigidbody;
    
    [SerializeField]
    private BeerCups beerCups;
    
    // current beerCup the agent is aiming at
    private BeerCup _aimedBeerCup;

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

        _rigidbody.velocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;

        this.transform.localPosition = initialPosition;

        UpdateBeerAim();
    }

    /// <summary>
    /// Collect the observations used by the agent to make decisions
    /// </summary>
    /// <param name="sensor"></param>

    public override void CollectObservations(VectorSensor sensor)
    {
        // get the vector from the agent to the aimed beer cup
        Vector3 toBeerCup = _aimedBeerCup.CupPosition - this.transform.position;

        // Observe the agent's pointing direction (3 observations)
        sensor.AddObservation(toBeerCup.normalized);

        // Observe the agent's force (1 observation)
        sensor.AddObservation(_rigidbody.velocity);

        // Observe the agent's distance to the beer cup (1 observation)
        sensor.AddObservation(Vector3.Distance(this.transform.localPosition, _aimedBeerCup.CupPosition));

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
        // Actions, size = 2
        // index 0 = pointing direction
        // index 1 = throwing force

        var vectorAction = actions.ContinuousActions;

        Vector3 pointingDirection = new Vector3(vectorAction[0], vectorAction[1], 0);

        // Set the force of the ball
        float throwingForce = actions.ContinuousActions[2];

        // Apply the force to the ball
        _rigidbody.AddForce(pointingDirection * throwingForce);

        // Penalty given each step to encourage agent to finish task quickly
        // AddReward(-1f / MaxStep);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;

        float force = 0f;
        float pitch = 0f;
        float yaw = 0f;

        // Get the user input
        if (Input.GetKey(KeyCode.UpArrow))
        {
            pitch = 1f;
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            pitch = -1f;
        }

        // Turn left/right
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            yaw = -1f;
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            yaw = 1f;
        }
        
        // Throw the ball
        if (Input.GetKey(KeyCode.Space))
        {
            Debug.Log("Heuristic staff");
            force = 1f;
        }

        continuousActionsOut[0] = pitch;
        continuousActionsOut[1] = yaw;
        continuousActionsOut[2] = force;
    }

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
            }
        }
    }


    private void UpdateBeerAim()
    {
        if (beerCups.BeerCupsList.Count < 1) return;
        // get the next beer cup to aim at
        if (ReferenceEquals(_aimedBeerCup, null) || _aimedBeerCup.IsHit)
        {
            _aimedBeerCup = beerCups.BeerCupsList[UnityEngine.Random.Range(0, beerCups.BeerCupsList.Count-1)];
        }
    
    }
    
    
    private void OnDrawGizmos()
    {
        // Assuming you have a reference to the ball's transform
        Vector3 ballDirection = transform.forward;
        float rayLength = 1f; // Length of the debug ray

        // Draw the debug ray
        Debug.DrawRay(transform.position, ballDirection * rayLength, Color.red);
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
}
