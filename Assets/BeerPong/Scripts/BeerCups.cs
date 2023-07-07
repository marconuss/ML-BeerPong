using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Manages the collection of beer cups
/// </summary>
public class BeerCups : MonoBehaviour
{
    /// <summary>
    /// The list of the opponents beer cups
    /// </summary>
    public List<BeerCup> BeerCupsList { get; private set;}

    /// <summary>
    /// Reset all the beer cups
    /// </summary>
    public void ResetAllBeerCups()
    {
        foreach (var beerCup in BeerCupsList)
        {
            beerCup.ResetBeerCup();
        }
    }
    
    private void Awake()
    {
        // initialize beer cups list
        BeerCupsList = new List<BeerCup>();
    }

    private void Start()
    {
       FindChildBeerCups(); 
    }
    
    private void FindChildBeerCups()
    {
        for (int i = 0; i <transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.CompareTag("beerCup"))
            {
                // found a beer cup, add it to the list
                BeerCup beerCup = child.GetComponent<BeerCup>();
                BeerCupsList.Add(beerCup);
            }
            else
            {
                Debug.LogWarning($"No beer cup component found for child {child.name}");
            }
        }
    }

    public BeerCup GetBeerCup(Collider other)
    {
        return other.GetComponentInParent<BeerCup>();
    }
}
