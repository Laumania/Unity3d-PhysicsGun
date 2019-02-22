using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

/* Original script "Gravity Gun": https://pastebin.com/w1G8m3dH
 * Original author: Jake Perry, reddit.com/user/nandos13
 * 
 * February 2019, above script was used as the starting point of below script.
 * https://github.com/Laumania/Unity3d-PhysicsGun
 * Repository created and script enhanced by: 
 * Mads Laumann, https://github.com/laumania
 * WarmedxMints, https://github.com/WarmedxMints
 */

 [RequireComponent(typeof(LineRenderer))]
public class PhysicsGunInteractionBehavior : MonoBehaviour
{
    /// <summary>For easy enable/disable mouse look when rotating objects, we store this reference</summary>
    private FirstPersonController   _firstPersonController;

    /// <summary>The rigidbody we are currently holding</summary>
    private Rigidbody               _grabbedRigidbody;

    /// <summary>The offset vector from the object's position to hit point, in local space</summary>
    private Vector3                 _hitOffsetLocal;

    /// <summary>The distance we are holding the object at</summary>
    private float                   _currentGrabDistance;

    /// <summary>The interpolation state when first grabbed</summary>
    private RigidbodyInterpolation  _initialInterpolationSetting;

    /// <summary>The difference between player & object rotation, updated when picked up or when rotated by the player</summary>
    private Quaternion              _rotationDifference;

    /// <summary>Tracks player input to rotate current object. Used and reset every fixedupdate call</summary>
    private Vector2                 _rotationInput          = Vector2.zero;
    private float                   _rotationSenstivity;
    [Header("Rotation Settings")]
    [SerializeField]
    private float                   _freeRotationSens       = 1.5f;
    [SerializeField]
    private float                   _snappedRotationSens    = 12.5f;
    /// <summary>The maximum distance at which a new object can be picked up</summary>
    private const float             _maxGrabDistance        = 50;

    //ScrollWheel ObjectMovement
    private Vector3                 _scrollWheelInput       = Vector3.zero;

    [Header("Scroll Wheel Object Movement"), Space(5)]
    [SerializeField]
    private float                   _scrollWheelSensitivity = 5f;
    //The min distance the object can be from the player.  The max distance will be _maxGrabDistance;
    [SerializeField]
    private float                   _minObjectDistance      = 2.5f;
    private bool                    _distanceChanged;

    //Vector3.Zero and Vector2.zero create a new Vector3 each time they are called so these simply save that process and a small amount of cpu runtime.
    private Vector3                 _zeroVector3            = Vector3.zero;
    private Vector3                 _oneVector3             = Vector3.one;
    private Vector3                 _zeroVector2            = Vector2.zero;

    [Header("Line Renderer Settings"), Space(5)]
    [SerializeField]
    private Vector2                 _uvAnimationRate        = new Vector2(1.0f, 0.0f);
    private Vector2                 _uvOffset               = Vector2.zero;
    private int                     _mainTex                = Shader.PropertyToID("_MainTex");
    [SerializeField]
    private int                     _arcResolution          = 12;
    private Vector3[]               _inputPoints;
    private LineRenderer            _lineRenderer;

    private bool                    _justReleased;
    private bool                    _wasKinematic;

    private void Start()
    {
        _firstPersonController = GetComponent<FirstPersonController>();
        if(_firstPersonController == null)
            Debug.LogError($"{nameof(_firstPersonController)} is null and the gravity gun won't work properly!", this);
        
        _lineRenderer = GetComponent<LineRenderer>();
        if (_lineRenderer == null)
            Debug.LogError($"{nameof(_lineRenderer)} is null and this script won't work properly without it!", this);

        _inputPoints                = new Vector3[_arcResolution];
        _lineRenderer.positionCount = _arcResolution;
    }

	private void Update ()
    {
        _firstPersonController.enabled = !Input.GetKey(KeyCode.R); 

        if (!Input.GetMouseButton(0))
        {
            // We are not holding the mouse button. Release the object and return before checking for a new one
            if (_grabbedRigidbody != null)
            {                
                ReleaseObject();
            }

            _justReleased = false;
            return;
        }

        if (_grabbedRigidbody == null && !_justReleased)
        {

            // We are not holding an object, look for one to pick up
            Ray ray = CenterRay();
            RaycastHit hit;
                       
            //Just so These aren't included in a build
#if UNITY_EDITOR
            Debug.DrawRay(ray.origin, ray.direction * _maxGrabDistance, Color.blue, 0.01f);
#endif
            if (Physics.Raycast(ray, out hit, _maxGrabDistance))
            {
                // Don't pick up kinematic rigidbodies (they can't move)
                if (hit.rigidbody != null /*&& !hit.rigidbody.isKinematic*/)
                {
                    // Track rigidbody's initial information
                    _grabbedRigidbody                   = hit.rigidbody;
                    _wasKinematic                       = _grabbedRigidbody.isKinematic;
                    _grabbedRigidbody.isKinematic       = false;
                    _grabbedRigidbody.freezeRotation    = true;
                    _initialInterpolationSetting        = _grabbedRigidbody.interpolation;
                    _rotationDifference                 = Quaternion.Inverse(transform.rotation) * _grabbedRigidbody.rotation;
                    _hitOffsetLocal                     = hit.transform.InverseTransformVector(hit.point - hit.transform.position);
                    _currentGrabDistance                = hit.distance; // Vector3.Distance(ray.origin, hit.point);

                    // Set rigidbody's interpolation for proper collision detection when being moved by the player
                    _grabbedRigidbody.interpolation     = RigidbodyInterpolation.Interpolate;

                    _lineRenderer.enabled = true;                    
                }
            }
        }
        else
        {
            // We are already holding an object, listen for rotation input
            if (Input.GetKey(KeyCode.R))
            {
                _rotationInput.x = Input.GetAxisRaw("Mouse X") * _rotationSenstivity;
                _rotationInput.y = Input.GetAxisRaw("Mouse Y") * _rotationSenstivity;
            }

            var direction = Input.GetAxis("Mouse ScrollWheel");
        
            //Optional Keyboard inputs
            if (Input.GetKeyDown(KeyCode.T))
                direction = -0.1f;
            else if (Input.GetKeyDown(KeyCode.G))
                direction = 0.1f;

            if (Mathf.Abs(direction) > 0 && CheckObjectDistance(direction))
            {
                _distanceChanged = true;
                _scrollWheelInput = transform.forward * _scrollWheelSensitivity * direction;
            } 
            else
            {
                _scrollWheelInput = _zeroVector3;
            }

            if(Input.GetMouseButtonDown(1))
            {
                //To prevent warnings in the inpector
                _grabbedRigidbody.collisionDetectionMode = !_wasKinematic ? CollisionDetectionMode.ContinuousSpeculative : CollisionDetectionMode.Continuous;
                _grabbedRigidbody.isKinematic = _wasKinematic = !_wasKinematic;
               
                _justReleased = true;
                ReleaseObject();
            }
        }
	}

    private void FixedUpdate()
    {
        if (_grabbedRigidbody)
        {
            // We are holding an object, time to rotate & move it
            Ray ray = CenterRay();

            // Apply any intentional rotation input made by the player & clear tracked input
            var intentionalRotation         = Quaternion.AngleAxis(_rotationInput.y, transform.right) * Quaternion.AngleAxis(-_rotationInput.x, transform.up) * _grabbedRigidbody.rotation;
            var relativeToPlayerRotation    = transform.rotation * _rotationDifference;

            var userRotation = Input.GetKey(KeyCode.R);

            if (userRotation && Input.GetKey(KeyCode.LeftShift))
            {
                _rotationSenstivity = _snappedRotationSens;
                var currentRot      = intentionalRotation;
                var newRot          = currentRot.eulerAngles;

                newRot.x = Mathf.Round(newRot.x / 45) * 45;
                newRot.y = Mathf.Round(newRot.y / 45) * 45;
                newRot.z = Mathf.Round(newRot.z / 45) * 45;

                _grabbedRigidbody.MoveRotation(Quaternion.Euler(newRot)); 
            }
            else
            {
                _rotationSenstivity = _freeRotationSens;
                //Rotate the object to remain consistent with any changes in player's rotation
                _grabbedRigidbody.MoveRotation(userRotation ? intentionalRotation : relativeToPlayerRotation);
            }
            // Remove all torque, reset rotation input & store the rotation difference for next FixedUpdate call
            _grabbedRigidbody.angularVelocity   = _zeroVector3;
            _rotationInput                      = _zeroVector2;
            _rotationDifference                 = Quaternion.Inverse(transform.rotation) * _grabbedRigidbody.rotation;

            // Calculate object's center position based on the offset we stored
            // NOTE: We need to convert the local-space point back to world coordinates
            // Get the destination point for the point on the object we grabbed
            var holdPoint           = ray.GetPoint(_currentGrabDistance) + _scrollWheelInput;
            var centerDestination   = holdPoint - _grabbedRigidbody.transform.TransformVector(_hitOffsetLocal);

#if UNITY_EDITOR
            Debug.DrawLine(ray.origin, holdPoint, Color.blue, Time.fixedDeltaTime);
#endif
            // Find vector from current position to destination
            var toDestination = centerDestination - _grabbedRigidbody.transform.position;

            // Calculate force
            var force = (toDestination / Time.fixedDeltaTime * 0.1f) / _grabbedRigidbody.mass;

            //force += _scrollWheelInput;
            // Remove any existing velocity and add force to move to final position
            _grabbedRigidbody.velocity = _zeroVector3;
            _grabbedRigidbody.AddForce(force, ForceMode.VelocityChange);

            //We need to recalculte the grabbed distance as the object distance from the player has been changed
            if (_distanceChanged)
            {
                _distanceChanged = false;
                _currentGrabDistance = Vector3.Distance(ray.origin, holdPoint);
            }

            RenderArc(transform.position, _grabbedRigidbody.transform.TransformPoint(_hitOffsetLocal), holdPoint);
        }
    }

    private void LateUpdate()
    {
        _uvOffset -= (_uvAnimationRate * Time.deltaTime);
        if (_lineRenderer.enabled)
        {
            _lineRenderer.material.SetTextureOffset(_mainTex, _uvOffset);
        }
    }

    //Create Arc on the line renderer
    private void RenderArc(Vector3 startpont, Vector3 endPoint, Vector3 midPoint)
    { 
        _lineRenderer.SetPositions(GetArcPoints(startpont, midPoint, endPoint));
    }

    public Vector3[] GetArcPoints(Vector3 a, Vector3 b, Vector3 c)
    {
        for (int i = 0; i < _arcResolution; i++)
        {
            var t           =  (float)(i) / (_arcResolution);
            _inputPoints[i] = Vector3.Lerp(Vector3.Lerp(a, b, t), Vector3.Lerp(b, c, t), t);
        }

        return _inputPoints;
    }

    /// <returns>Ray from center of the main camera's viewport forward</returns>
    private Ray CenterRay()
    {
        return Camera.main.ViewportPointToRay(_oneVector3 * 0.5f);
    }

    //Check distance is within range when moving object with the scroll wheel
    private bool CheckObjectDistance(float direction)
    {
        var pointA      = transform.position;
        var pointB      = _grabbedRigidbody.position;

        var distance    = Vector3.Distance(pointA, pointB);

        if (direction > 0)
            return distance <= _maxGrabDistance;

        if (direction < 0)
            return distance >= _minObjectDistance;

        return false;
    }

    private void ReleaseObject()
    {
        // Reset the rigidbody to how it was before we grabbed it
        _grabbedRigidbody.isKinematic               = _wasKinematic;
        _grabbedRigidbody.interpolation             = _initialInterpolationSetting;
        _grabbedRigidbody.freezeRotation            = false;
        _grabbedRigidbody                           = null;
        _scrollWheelInput                           = _zeroVector3;
        _lineRenderer.enabled                       = false;
        //Reset the line points so we do not get a brief flash or the previous line
        _lineRenderer.SetPositions(GetArcPoints(_zeroVector3, _zeroVector3, _zeroVector3));
    }
}