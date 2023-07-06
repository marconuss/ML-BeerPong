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
    
    public void ResetBeerCup()
    {
        beerCollider.gameObject.SetActive(true);
    }

    /// <summary>
    /// Called when the cup wakes up
    /// </summary>
    private void Awake()
    {
        beerCollider = transform.Find("BeerCollider").GetComponent<Collider>();
    }
}
