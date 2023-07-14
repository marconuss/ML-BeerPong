using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ThrowBall : MonoBehaviour
{
    private Vector3 initialPosition;

    private Rigidbody _rigidbody;

    private bool _isBallThrown = false;
    
    private const float MaxBallPitch = 30f;

    private const float MaxBallYaw = 20f;

    private const float MaxThrowForce = 3f;
    private const float BaseThrowForce = 1f;

    private float _pitch;
    private float _yaw;
    private float _throwForce;

    public BeerCup aimedBeerCup;

    // Start is called before the first frame update
    void Start()
    {
        _pitch = 0f;
        _yaw = 0f;
        _throwForce = BaseThrowForce;
        initialPosition = transform.position;
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.isKinematic = true;
    }

    public void ResetBall()
    {
        _isBallThrown = false;
        _rigidbody.isKinematic = true;
        DrawTrajectory.Instance.IsVisible = true;
        transform.position = initialPosition;
    }

    public void ThrowBallAtTarget()
    {
        _isBallThrown = true;
        _rigidbody.isKinematic = false;
        DrawTrajectory.Instance.IsVisible = false;
        _rigidbody.AddForce(_throwForce * transform.forward);
        //_rigidbody.AddTorque(_pitch * transform.right);
        //_rigidbody.AddTorque(_yaw * transform.up);
    }

    private void Update()
    {
        if (!_isBallThrown)
        {
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0);
            Vector3 throwDirection = rotation * transform.forward;
            transform.rotation = rotation;

            DrawTrajectory.Instance.Draw(transform.forward, _throwForce, _rigidbody, initialPosition);
        }

        if (Input.GetButtonDown("Debug Reset"))
        {
            ResetBall();
            aimedBeerCup.ResetBeerCup();
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("boundary"))
        {
            Debug.Log("ball hit something else");
            //ResetBall();
        }
        else if (other.gameObject.CompareTag("beerCup"))
        {
            Debug.Log("ball hit cup");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<BeerCup>() == aimedBeerCup)
        {
            aimedBeerCup.gameObject.SetActive(false);
        }
        else
        {
            Debug.Log("ball hit wrong beer");
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

    public void UpdateThrowForce(float throwForce)
    {
        _throwForce = throwForce * MaxThrowForce + BaseThrowForce;
    }
}