using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeerCup : MonoBehaviour
{
    /// <summary>
    /// Trigger collider representing the beer
    /// </summary>
    [HideInInspector] public Collider beerCollider;
    
    public bool IsHit { get; private set; }
    
    /// <summary>
    /// The position of the beer cup
    /// </summary>
    public Vector3 CupPosition
    {
        get
        {
            return transform.position;
        }
    }
    
    // the initial position of the beer cup
    private Vector3 _initialPosition;

    
    /// <summary>
    /// Reset the beer cup to its initial position
    /// </summary>
    public void ResetBeerCup()
    {
        transform.position = _initialPosition;
        transform.rotation = Quaternion.identity;
        IsHit = false;
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Called when the cup wakes up
    /// </summary>
    private void Awake()
    {
        _initialPosition = transform.position;
        beerCollider = transform.Find("BeerCollider").GetComponent<Collider>();
    }

    public void GotHit()
    {
        Debug.Log("Got hit");
        IsHit = true;
        gameObject.SetActive(false);
    }
}
