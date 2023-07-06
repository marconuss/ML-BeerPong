using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Machine Learning Agent
/// </summary>

public class BallAgent : Agent
{
    //  ---------------------------------------
    //  Serialized Fields
    //  ---------------------------------------
    [Tooltip("Whether it is gameplay mode or training mode")]
    [SerializeField]
    private bool trainingMode;
    
    [Tooltip("The initial position of the agent")]
    [SerializeField]
    private Vector3 initialPosition;
    
    //  ---------------------------------------
    // Private Variables
    //  ---------------------------------------
    
    // Rigidbody of the ball
    private Rigidbody _rigidbody;
    
    
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
    }
    
    /// <summary>
    /// Collect the observations used by the agent to make decisions
    /// </summary>
    /// <param name="sensor"></param>
    
    public override void CollectObservations(VectorSensor sensor)
    {
        // get the vector form the beak tip to the nearest flower
        //Vector3 beerVector = _nearestBeerCup.localPosition - this.transform.position;
        
        // Observe the agent's pointing direction (3 observations)
        sensor.AddObservation(this.transform.forward);
        
        // Observe the agent's force (1 observation)
        sensor.AddObservation(_rigidbody.velocity);
        
        // Observe the agent's distance to the beer cup (1 observation)
        //sensor.AddObservation(Vector3.Distance(this.transform.localPosition, _nearestBeerCup.localPosition));
        
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
}
