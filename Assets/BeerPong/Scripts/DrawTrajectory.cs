using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawTrajectory : MonoBehaviour
{
    
    [SerializeField]
    public LineRenderer lineRenderer;
    
    [SerializeField]
    [Range(6, 50)]
    private int lineSegmentCount = 20;
    
    [SerializeField]
    [Range(20, 100)]
    private int showPercentage = 50;
    
    private int _linePointsCount;
    
    private List<Vector3> _linePoints = new List<Vector3>();

    public bool IsVisible
    {
        get => lineRenderer.enabled;
        set => lineRenderer.enabled = value;
    } 

    #region Singleton
    public static DrawTrajectory Instance;
    void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
    }
    
    #endregion
    
    private void Start()
    {
        _linePointsCount = lineSegmentCount * showPercentage / 100;
        lineRenderer.positionCount = 0;
    }
    
    public void Draw(Vector3 throwDirection, float throwForce, Rigidbody rb, Vector3 initialPosition)
    {
        if (!IsVisible)
        {
            return;
        }
        
        _linePointsCount = lineSegmentCount * showPercentage / 100;
        Vector3 forceVector = throwDirection * throwForce;
        
        _linePoints.Clear();
        _linePoints.Add(initialPosition);
        float timeDelta = 1f / lineSegmentCount;
        for (int i = 1; i < _linePointsCount; i++)
        {
            float time = timeDelta * i;
            Vector3 velocity = (forceVector / rb.mass ) * time;
            //Position (at time) = Origin + Direction * Velocity * Time (i * step)

            Vector3 position = initialPosition + velocity * Time.fixedDeltaTime +
                               Physics.gravity * (time * time) / 2f;
            _linePoints.Add(position);
        }
        lineRenderer.positionCount = _linePointsCount;
        lineRenderer.SetPositions(_linePoints.ToArray());
    }

}
