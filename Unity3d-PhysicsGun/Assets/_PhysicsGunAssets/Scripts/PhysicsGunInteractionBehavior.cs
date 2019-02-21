using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

/* Original script "Gravity Gun": https://pastebin.com/w1G8m3dH
 * Original author: Jake Perry, reddit.com/user/nandos13
 * 
 * February 2019, above script was used as the starting point of this current script.
 * This improved script can be found here: https://github.com/Laumania/Unity3d-PhysicsGun
 * Repository created and script enhanced by Mads Laumann, http://laumania.net
 */
public class PhysicsGunInteractionBehavior : MonoBehaviour
{
    /// <summary>For easy enable/disable mouse look when rotating objects, we store this reference</summary>
    private FirstPersonController _firstPersonController;
    /// <summary>The rigidbody we are currently holding</summary>
    private Rigidbody _grabbedRigidbody;
    /// <summary>The offset vector from the object's position to hit point, in local space</summary>
    private Vector3 _hitOffsetLocal;
    /// <summary>The distance we are holding the object at</summary>
    private float _currentGrabDistance;
    /// <summary>The interpolation state when first grabbed</summary>
    private RigidbodyInterpolation _initialInterpolationSetting;
    /// <summary>The difference between player & object rotation, updated when picked up or when rotated by the player</summary>
    private Quaternion _rotationDifference;
    /// <summary>Tracks player input to rotate current object. Used and reset every fixedupdate call</summary>
    private Vector2 _rotationInput;
    /// <summary>The maximum distance at which a new object can be picked up</summary>
    private const float _maxGrabDistance = 50;

    /// <summary>Transform to parent the object we have pickuped up.  This could be a child of a gun so the grabbed object moves as the gun sways or just the character</summary>
    [SerializeField]
    private Transform _objectAttachPoint;
    private Transform _objectOriginalParent;

    //Velocity Estimation courtsey of Valve from the SteamVR Interations examples

    [Tooltip("How many frames to average over for computing velocity")]
    public int velocityAverageFrames = 5;
    [Tooltip("How many frames to average over for computing angular velocity")]
    public int angularVelocityAverageFrames = 11;

    private Coroutine routine;
    private int sampleCount;
    private Vector3[] velocitySamples;
    private Vector3[] angularVelocitySamples;

    void Awake()
    {
        velocitySamples = new Vector3[velocityAverageFrames];
        angularVelocitySamples = new Vector3[angularVelocityAverageFrames];
    }

    void Start()
    {
        _firstPersonController = GetComponent<FirstPersonController>();

        if (_firstPersonController == null)
            Debug.LogError($"{nameof(_firstPersonController)} is null and the gravity gun won't work properly!", this);
    }

    void Update()
    {
        _firstPersonController.enabled = !Input.GetKey(KeyCode.R);

        if (!Input.GetMouseButton(0))
        {
            // We are not holding the mouse button. Release the object and return before checking for a new one
            if (_grabbedRigidbody != null)
            {
                //Dettach Object
                _grabbedRigidbody.transform.SetParent(_objectOriginalParent);
                _objectOriginalParent = null;

                // Reset the rigidbody to how it was before we grabbed it
                _grabbedRigidbody.interpolation = _initialInterpolationSetting;
                _grabbedRigidbody.freezeRotation = false;

                //Estimate Velocity based on previous object positions
                FinishEstimatingVelocity();

                var velocity = GetVelocityEstimate();
                var angularVelocity = GetAngularVelocityEstimate();

                //Apply velocity
                _grabbedRigidbody.velocity = velocity;
                _grabbedRigidbody.angularVelocity = angularVelocity;

                _grabbedRigidbody = null;
            }
            return;
        }

        if (_grabbedRigidbody == null)
        {
            // We are not holding an object, look for one to pick up

            Ray ray = CenterRay();
            RaycastHit hit;

            Debug.DrawRay(ray.origin, ray.direction * _maxGrabDistance, Color.blue, 0.01f);

            if (Physics.Raycast(ray, out hit, _maxGrabDistance))
            {
                // Don't pick up kinematic rigidbodies (they can't move)
                if (hit.rigidbody != null && !hit.rigidbody.isKinematic)
                {
                    // Track rigidbody's initial information
                    _grabbedRigidbody = hit.rigidbody;
                    _grabbedRigidbody.freezeRotation = true;
                    _initialInterpolationSetting = _grabbedRigidbody.interpolation;
                    _rotationDifference = Quaternion.Inverse(transform.rotation) * hit.rigidbody.rotation;
                    _hitOffsetLocal = hit.transform.InverseTransformVector(hit.point - hit.transform.position);
                    _currentGrabDistance = Vector3.Distance(ray.origin, hit.point);

                    // Set rigidbody's interpolation for proper collision detection when being moved by the player
                    _grabbedRigidbody.interpolation = RigidbodyInterpolation.Interpolate;

                    // Attach object
                    if (_grabbedRigidbody.transform.parent)
                        _objectOriginalParent = _grabbedRigidbody.transform.parent;

                    _grabbedRigidbody.transform.SetParent(_objectAttachPoint);

                    //Start Coroutine to monitor movement
                    BeginEstimatingVelocity();
                }
            }
        }
        else
        {
            // We are already holding an object, listen for rotation input
            if (Input.GetKey(KeyCode.R))
            {
                _rotationInput += new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
            }
        }
    }

    void FixedUpdate()
    {
        if (_grabbedRigidbody)
        {
            // We are holding an object, time to rotate & move it

            Ray ray = CenterRay();

            // Apply any intentional rotation input made by the player & clear tracked input
            var intentionalRotation = Quaternion.AngleAxis(_rotationInput.y, transform.right) * Quaternion.AngleAxis(-_rotationInput.x, transform.up) * _grabbedRigidbody.rotation;
            var relativeToPlayerRotation = _rotationDifference * transform.rotation;

            // Rotate the object to remain consistent with any changes in player's rotation
            _grabbedRigidbody.MoveRotation(intentionalRotation);
            //_grabbedRigidbody.MoveRotation(relativeToPlayerRotation);


            // Remove all torque, reset rotation input & store the rotation difference for next FixedUpdate call
            _grabbedRigidbody.angularVelocity = Vector3.zero;
            _rotationInput = Vector2.zero;
            _rotationDifference = Quaternion.Inverse(transform.rotation) * _grabbedRigidbody.rotation;

            // Calculate object's center position based on the offset we stored
            // NOTE: We need to convert the local-space point back to world coordinates
            // Get the destination point for the point on the object we grabbed
            Vector3 holdPoint = ray.GetPoint(_currentGrabDistance);
            Vector3 centerDestination = holdPoint - _grabbedRigidbody.transform.TransformVector(_hitOffsetLocal);
            Debug.DrawLine(ray.origin, holdPoint, Color.blue, Time.fixedDeltaTime);

            // Find vector from current position to destination
            Vector3 toDestination = centerDestination - _grabbedRigidbody.transform.position;

            // Calculate force
            Vector3 force = toDestination / Time.fixedDeltaTime * 0.8f;

            // Remove any existing velocity and add force to move to final position
            _grabbedRigidbody.velocity = Vector3.zero;
            _grabbedRigidbody.AddForce(force, ForceMode.VelocityChange);
        }
    }

    /// <returns>Ray from center of the main camera's viewport forward</returns>
    private Ray CenterRay()
    {
        return Camera.main.ViewportPointToRay(Vector3.one * 0.5f);
    }

    #region Velocity Estimation

    //======= Copyright (c) Valve Corporation, All rights reserved. ===============
    //
    // Purpose: Estimates the velocity of an object based on change in position
    //
    //=============================================================================

    public void BeginEstimatingVelocity()
    {
        //Stop coroutine if it is running for some reason
        FinishEstimatingVelocity();

        routine = StartCoroutine(EstimateVelocityCoroutine());
    }

    public void FinishEstimatingVelocity()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }

    public Vector3 GetVelocityEstimate()
    {
        // Compute average velocity
        Vector3 velocity = Vector3.zero;
        int velocitySampleCount = Mathf.Min(sampleCount, velocitySamples.Length);
        if (velocitySampleCount != 0)
        {
            for (int i = 0; i < velocitySampleCount; i++)
            {
                velocity += velocitySamples[i];
            }
            velocity *= (1.0f / velocitySampleCount);
        }

        return velocity;
    }

    public Vector3 GetAngularVelocityEstimate()
    {
        // Compute average angular velocity
        Vector3 angularVelocity = Vector3.zero;
        int angularVelocitySampleCount = Mathf.Min(sampleCount, angularVelocitySamples.Length);
        if (angularVelocitySampleCount != 0)
        {
            for (int i = 0; i < angularVelocitySampleCount; i++)
            {
                angularVelocity += angularVelocitySamples[i];
            }
            angularVelocity *= (1.0f / angularVelocitySampleCount);
        }

        return angularVelocity;
    }

    private IEnumerator EstimateVelocityCoroutine()
    {
        sampleCount = 0;

        Vector3 previousPosition = _grabbedRigidbody.transform.position;
        Quaternion previousRotation = _grabbedRigidbody.transform.rotation;

        while (true)
        {
            yield return new WaitForEndOfFrame();

            float velocityFactor = 1.0f / Time.deltaTime;

            int v = sampleCount % velocitySamples.Length;
            int w = sampleCount % angularVelocitySamples.Length;
            sampleCount++;

            // Estimate linear velocity
            velocitySamples[v] = velocityFactor * (_grabbedRigidbody.transform.position - previousPosition);

            // Estimate angular velocity
            Quaternion deltaRotation = _grabbedRigidbody.transform.rotation * Quaternion.Inverse(previousRotation);

            float theta = 2.0f * Mathf.Acos(Mathf.Clamp(deltaRotation.w, -1.0f, 1.0f));
            if (theta > Mathf.PI)
            {
                theta -= 2.0f * Mathf.PI;
            }

            Vector3 angularVelocity = new Vector3(deltaRotation.x, deltaRotation.y, deltaRotation.z);
            if (angularVelocity.sqrMagnitude > 0.0f)
            {
                angularVelocity = theta * velocityFactor * angularVelocity.normalized;
            }

            angularVelocitySamples[w] = angularVelocity;

            previousPosition = _grabbedRigidbody.transform.position;
            previousRotation = _grabbedRigidbody.transform.rotation;
        }
    }

    #endregion
}