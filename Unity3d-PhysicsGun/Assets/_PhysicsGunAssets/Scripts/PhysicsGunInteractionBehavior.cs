using UnityEngine;
using UnityEngine.UI;
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

[RequireComponent(typeof(GunLineRenderer))]
public class PhysicsGunInteractionBehavior : MonoBehaviour
{
    [Header("Input Setting")]
    public KeyCode Rotate                   = KeyCode.R;
    public KeyCode SnapRotation             = KeyCode.LeftShift;
    public KeyCode SwitchAxis               = KeyCode.Tab;
    public KeyCode RotateZ                  = KeyCode.Space;
    public KeyCode RotationSpeedIncrease    = KeyCode.LeftControl;
    public KeyCode ResetRotation            = KeyCode.LeftAlt;

    /// <summary>For easy enable/disable mouse look when rotating objects, we store this reference</summary>
    private FirstPersonController   _firstPersonController;

    /// <summary>The rigidbody we are currently holding</summary>
    private Rigidbody               _grabbedRigidbody;

    /// <summary>The transfor of the rigidbody we are holding</summary>
    private Transform               _grabbedTransform; 

    /// <summary>The offset vector from the object's position to hit point, in local space</summary>
    private Vector3                 _hitOffsetLocal;

    /// <summary>The distance we are holding the object at</summary>
    private float                   _currentGrabDistance;

    /// <summary>The interpolation state when first grabbed</summary>
    private RigidbodyInterpolation  _initialInterpolationSetting;

    /// <summary>The difference between player & object rotation, updated when picked up or when rotated by the player</summary>
    private Quaternion              _rotationDifference;

    /// <summary>The start point for the Laser. This will typically be on the end of the gun</summary>
    [SerializeField]
    private Transform               _laserStartPoint = null;

    /// <summary>Tracks player input to rotate current object. Used and reset every fixedupdate call</summary>
    private Vector3                 _rotationInput          = Vector3.zero;
    [Header("Rotation Settings")]
    [SerializeField]
    private float                   _rotationSenstivity     = 1.5f;
    public  float                   _snapRotationDegrees    = 45f;
    [SerializeField]
    private float                   _snappedRotationSens    = 15f;
    [SerializeField]
    private float                   _rotationSpeed          = 5f;

    private Quaternion              _desiredRotation = Quaternion.identity;

    /// <summary>The maximum distance at which a new object can be picked up</summary>
    private const float             _maxGrabDistance        = 50f;

    [SerializeField, Tooltip("Input values above this will be considered and intentional change in rotation")]
    private float                   _rotationTollerance = 0.8f;

    private bool                    _userRotation;
    private bool                    _snapRotation;
   
    private Vector3                 _lockedRot;
    private Vector3                 _forward;
    private Vector3                 _up;
    private Vector3                 _right;

    private bool                    _rotationAxis;
    [SerializeField]
    private Text                    _rotationAxisText = null;

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

    private GunLineRenderer         _lineRendererController;

    private GameObject              _laserGlowEndPoint;

    private bool                    _justReleased;
    private bool                    _wasKinematic;

    private void Start()
    {
        _firstPersonController = GetComponent<FirstPersonController>();
        if(_firstPersonController == null)
            Debug.LogError($"{nameof(_firstPersonController)} is null and the gravity gun won't work properly!", this);

        _lineRendererController = GetComponent<GunLineRenderer>();

        SetRotationAxisText();

        if (_laserStartPoint == null)
            _laserStartPoint = transform;
    }

	private void Update ()
    {
        _userRotation = Input.GetKey(Rotate);

        _firstPersonController.enabled = !_userRotation;

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
                    _grabbedTransform                   = _grabbedRigidbody.transform;
                    // Set rigidbody's interpolation for proper collision detection when being moved by the player
                    _grabbedRigidbody.interpolation     = RigidbodyInterpolation.Interpolate;

                    if (_lineRendererController != null)
                        _lineRendererController.StartLineRenderer(_grabbedRigidbody.gameObject);
#if UNITY_EDITOR
                    Debug.DrawRay(hit.point, hit.normal * 10f, Color.red, 10f);
#endif
                }
            }
        }
        else
        {
            if(Input.GetKeyDown(Rotate))
            {
                SetRotationAxisText();
            }

            if (Input.GetKey(ResetRotation))
            {
                _grabbedRigidbody.MoveRotation(Quaternion.identity);
                SetRotationAxisText();
            }

            // We are already holding an object, listen for rotation input
            if (Input.GetKey(Rotate))
            {
                var rotateZ         = Input.GetKey(RotateZ);

                var increaseSens    = Input.GetKey(RotationSpeedIncrease) ? 2.5f : 1f;

                if(Input.GetKeyDown(SwitchAxis))
                {
                    _rotationAxis = !_rotationAxis;

                    SetRotationAxisText();
                }

                //Snap Object nearest _snapRotationDegrees
                if (Input.GetKeyDown(SnapRotation))
                {
                    _snapRotation = true;

                    var newRot = _grabbedRigidbody.transform.rotation.eulerAngles;

                    newRot.x = Mathf.Round(newRot.x / _snapRotationDegrees) * _snapRotationDegrees;
                    newRot.y = Mathf.Round(newRot.y / _snapRotationDegrees) * _snapRotationDegrees;
                    newRot.z = Mathf.Round(newRot.z / _snapRotationDegrees) * _snapRotationDegrees;

                    _grabbedRigidbody.MoveRotation(Quaternion.Euler(newRot));

                    SetRotationAxisText();
                }
                else if(Input.GetKeyUp(SnapRotation))
                {
                    _snapRotation = false;
                    SetRotationAxisText();
                }

                var x = Input.GetAxisRaw("Mouse X");
                var y = Input.GetAxisRaw("Mouse Y");

                if (Mathf.Abs(x) > _rotationTollerance)
                {
                    _rotationInput.x = rotateZ ? 0f : x * _rotationSenstivity * increaseSens;                   
                    _rotationInput.z = rotateZ ? x * _rotationSenstivity * increaseSens : 0f;
                }

                if(Mathf.Abs(y) > _rotationTollerance)
                {
                    _rotationInput.y = y * _rotationSenstivity * increaseSens;
                }
            }
            else
            {
                _snapRotation = false;
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
                _grabbedRigidbody.collisionDetectionMode    = !_wasKinematic ? CollisionDetectionMode.ContinuousSpeculative : CollisionDetectionMode.Continuous;
                _grabbedRigidbody.isKinematic               = _wasKinematic = !_wasKinematic;
               
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

            UpdateRotationAxis();

#if UNITY_EDITOR
            Debug.DrawRay(_grabbedTransform.position, _up * 5f, Color.green);
            Debug.DrawRay(_grabbedTransform.position, _right * 5f, Color.blue);
            Debug.DrawRay(_grabbedTransform.position, _forward * 5f, Color.red);
#endif
            // Apply any intentional rotation input made by the player & clear tracked input
            var intentionalRotation         = Quaternion.AngleAxis(_rotationInput.z, _forward) * Quaternion.AngleAxis(_rotationInput.y, _right) * Quaternion.AngleAxis(-_rotationInput.x, _up) *  _grabbedRigidbody.rotation;
            var relativeToPlayerRotation    = transform.rotation * _rotationDifference;

            if (_userRotation && _snapRotation)
            {
                //Add mouse movement to vector so we can measure the amount of movement
                _lockedRot += _rotationInput;    

                //If the mouse has moved far enough to rotate the snapped object
                if (Mathf.Abs(_lockedRot.x) > _snappedRotationSens || Mathf.Abs(_lockedRot.y) > _snappedRotationSens || Mathf.Abs(_lockedRot.z) > _snappedRotationSens)
                {
                    for (var i = 0; i < 3; i++)
                    {
                        if (_lockedRot[i] > _snappedRotationSens)
                        {
                            _lockedRot[i] += _snapRotationDegrees;
                        }
                        else if (_lockedRot[i] < -_snappedRotationSens)
                        {
                            _lockedRot[i] += -_snapRotationDegrees;
                        }
                        else
                        {
                            _lockedRot[i] = 0;
                        }
                    }

                    var q = Quaternion.AngleAxis(-_lockedRot.x, _up) * Quaternion.AngleAxis(_lockedRot.y, _right) * Quaternion.AngleAxis(_lockedRot.z, _forward) * _grabbedRigidbody.rotation;

                    var newRot = q.eulerAngles;

                    newRot.x = Mathf.Round(newRot.x / _snapRotationDegrees) * _snapRotationDegrees;
                    newRot.y = Mathf.Round(newRot.y / _snapRotationDegrees) * _snapRotationDegrees;
                    newRot.z = Mathf.Round(newRot.z / _snapRotationDegrees) * _snapRotationDegrees;

                    _desiredRotation = Quaternion.Euler(newRot);

                    _lockedRot = _zeroVector2;
                }
            }
            else
            {                
                //Rotate the object to remain consistent with any changes in player's rotation
                _desiredRotation = _userRotation ? intentionalRotation : relativeToPlayerRotation;
            }
            // Remove all torque, reset rotation input & store the rotation difference for next FixedUpdate call
            _grabbedRigidbody.angularVelocity   = _zeroVector3;
            _rotationInput                      = _zeroVector2;
            _rotationDifference                 = Quaternion.Inverse(transform.rotation) * _grabbedRigidbody.rotation;

            // Calculate object's center position based on the offset we stored
            // NOTE: We need to convert the local-space point back to world coordinates
            // Get the destination point for the point on the object we grabbed
            var holdPoint           = ray.GetPoint(_currentGrabDistance) + _scrollWheelInput;
            var centerDestination = holdPoint - _grabbedTransform.TransformVector(_hitOffsetLocal);

#if UNITY_EDITOR
            Debug.DrawLine(ray.origin, holdPoint, Color.blue, Time.fixedDeltaTime);
#endif
            // Find vector from current position to destination
            var toDestination = centerDestination - _grabbedTransform.position;

            // Calculate force
            var force = (toDestination / Time.fixedDeltaTime * 0.3f) / _grabbedRigidbody.mass;

            //force += _scrollWheelInput;
            // Remove any existing velocity and add force to move to final position
            _grabbedRigidbody.velocity = _zeroVector3;
            _grabbedRigidbody.AddForce(force, ForceMode.VelocityChange);

            //Rotate object
            RotateGrabbedObject();

            //We need to recalculte the grabbed distance as the object distance from the player has been changed
            if (_distanceChanged)
            {
                _distanceChanged = false;
                _currentGrabDistance = Vector3.Distance(ray.origin, holdPoint);
            }

            if (_lineRendererController != null)
                _lineRendererController.UpdateArcPoints(_laserStartPoint.position, holdPoint, _grabbedTransform.TransformPoint(_hitOffsetLocal));
        }
    }

    private void RotateGrabbedObject()
    {
        if (_grabbedRigidbody == null)
            return;

        _grabbedRigidbody.MoveRotation(Quaternion.Lerp(_grabbedRigidbody.rotation, _desiredRotation, Time.fixedDeltaTime * _rotationSpeed));
    }
    /// <summary>
    /// Takes a Vector direction and finds the nearest directional vector to it from a transforms directions
    /// </summary>
    /// <param name="v">Vector Direction</param>
    /// <param name="t">Transform to check</param>
    /// <returns></returns>
    private Vector3 NearestDirection(Vector3 v, Transform t)
    {
        var directions = new Vector3[]
        {
            t.right,
            -t.right,
            t.up,
            -t.up,
            t.forward,
            -t.forward
        };

        var maxDot  = -Mathf.Infinity;
        var ret     = _zeroVector3;

        for(var i = 0; i < 6; i++)
        {
            var dot = Vector3.Dot(v, directions[i]);
            if(dot > maxDot)
            {
                ret     = directions[i];
                maxDot  = dot;
            }
        }

        return ret;
    }

    //Update Rotation axis based on movement
    private void UpdateRotationAxis()
    {
        if (!_snapRotation)
        {
            //if (Mathf.Abs(_rotationInput.x) > _rotationTollerance * _rotationSenstivity)
            //{
            //    forward = _rotationAxis ? t.forward : NearestDirection(transform.forward, t);
            //    right   = _rotationAxis ? t.right   : NearestDirection(transform.right, t);
            //}
            //if (Mathf.Abs(_rotationInput.y) > _rotationTollerance * _rotationSenstivity)
            //{
            //    forward = _rotationAxis ? t.forward : NearestDirection(transform.forward, t);
            //    up      = _rotationAxis ? t.up      : NearestDirection(transform.up, t);
            //}
            //if (Mathf.Abs(_rotationInput.z) > _rotationTollerance * _rotationSenstivity)
            //{
            //    right = _rotationAxis ? t.right : NearestDirection(transform.right, t);
            //    up    = _rotationAxis ? t.up    : NearestDirection(transform.up, t);
            //}

            _forward = transform.forward;
            _right   = transform.right;
            _up      = transform.up;

            return;
        }

        _forward = _rotationAxis ? _grabbedTransform.forward : NearestDirection(transform.forward, _grabbedTransform);
        _right   = _rotationAxis ? _grabbedTransform.right   : NearestDirection(transform.right, _grabbedTransform);
        _up      = _rotationAxis ? _grabbedTransform.up      : NearestDirection(transform.up, _grabbedTransform);
    }

    private void SetRotationAxisText()
    {
        if (_rotationAxisText != null)
        {
            if(!_snapRotation)
            {
                _rotationAxisText.enabled = false;
                return;
            }

            _rotationAxisText.enabled = true;
            _rotationAxisText.text = _rotationAxis ? "Snapped Rotation Axis = Objects Axis" : "Snapped Rotation Axis = Player Axis";
        }
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
        _grabbedTransform                           = null;

        if (_lineRendererController != null)
            _lineRendererController.StopLineRenderer();
    }
}