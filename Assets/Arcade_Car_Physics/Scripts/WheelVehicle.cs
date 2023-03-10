/*
 * This code is part of Arcade Car Physics for Unity by Saarg (2018)
 * 
 * This is distributed under the MIT Licence (see LICENSE.md for details)
 */

using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

#if MULTIOSCONTROLS
    using MOSC;
#endif

namespace VehicleBehaviour {
    [RequireComponent(typeof(Rigidbody))]
    public class WheelVehicle : MonoBehaviour {
        
        [Header("Inputs")]
    #if MULTIOSCONTROLS
        [SerializeField] PlayerNumber playerId;
    #endif
        // If isPlayer is false inputs are ignored
        [SerializeField] bool isPlayer = true;
        public bool IsPlayer { get => isPlayer;
            set => isPlayer = value;
        }

        // Input names to read using GetAxis
        CarControls carControls;
        string throttleInput = "Vertical";
        string turnInput = "Horizontal";
        
        /* 
         *  Turn input curve: x real input, y value used
         *  My advice (-1, -1) tangent x, (0, 0) tangent 0 and (1, 1) tangent x
         */
        [SerializeField] AnimationCurve turnInputCurve = AnimationCurve.Linear(-1.0f, -1.0f, 1.0f, 1.0f);

        [Header("Wheels")]
        [SerializeField] WheelCollider[] driveWheel = new WheelCollider[0];
        public WheelCollider[] DriveWheel => driveWheel;
        [SerializeField] WheelCollider[] turnWheel = new WheelCollider[0];

        public WheelCollider[] TurnWheel => turnWheel;

        // This code checks if the car is grounded only when needed and the data is old enough
        bool isGrounded = false;
        int lastGroundCheck = 0;
        public bool IsGrounded { get {
            if (lastGroundCheck == Time.frameCount)
                return isGrounded;

            lastGroundCheck = Time.frameCount;
            isGrounded = true;
            foreach (WheelCollider wheel in wheels)
            {
                if (!wheel.gameObject.activeSelf || !wheel.isGrounded)
                    isGrounded = false;
            }
            return isGrounded;
        }}

        [Header("Behaviour")]
        /*
         *  Motor torque represent the torque sent to the wheels by the motor with x: speed in km/h and y: torque
         *  The curve should start at x=0 and y>0 and should end with x>topspeed and y<0
         *  The higher the torque the faster it accelerate
         *  the longer the curve the faster it gets
         */
        [SerializeField] AnimationCurve motorTorque = new AnimationCurve(new Keyframe(0, 200), new Keyframe(50, 300), new Keyframe(200, 0));

        // Differential gearing ratio
        [Range(2, 16)]
        [SerializeField] float diffGearing = 4.0f;
        public float DiffGearing { get => diffGearing;
            set => diffGearing = value;
        }

        // Max steering hangle, usualy higher for drift car
        [Range(0f, 50.0f)]
        [SerializeField] float steerAngle = 30.0f;
        public float SteerAngle { get => steerAngle;
            set => steerAngle = Mathf.Clamp(value, 0.0f, 50.0f);
        }

        // The value used in the steering Lerp, 1 is instant (Strong power steering), and 0 is not turning at all
        [Range(0.001f, 1.0f)]
        [SerializeField] float steerSpeed = 0.2f;
        public float SteerSpeed { get => steerSpeed;
            set => steerSpeed = Mathf.Clamp(value, 0.001f, 1.0f);
        }

        // How hard do you want to drift?
        [Range(0.0f, 2f)]
        [SerializeField] float driftIntensity = 1f;
        public float DriftIntensity { get => driftIntensity;
            set => driftIntensity = Mathf.Clamp(value, 0.0f, 2.0f);
        }

        // Reset Values
        Vector3 spawnPosition;
        Quaternion spawnRotation;

        /*
         *  The center of mass is set at the start and changes the car behavior A LOT
         *  I recomment having it between the center of the wheels and the bottom of the car's body
         *  Move it a bit to the from or bottom according to where the engine is
         */
        [SerializeField] Transform centerOfMass = null;

        // Force aplied downwards on the car, proportional to the car speed
        [Range(0.5f, 10f)]
        [SerializeField] float downforce = 1.0f;

        public float Downforce
        {
            get => downforce;
            set => downforce = Mathf.Clamp(value, 0, 5);
        }     

        // When IsPlayer is false you can use this to control the steering
        float steering;
        public float Steering { get => steering;
            set => steering = Mathf.Clamp(value, -1f, 1f);
        } 

        // When IsPlayer is false you can use this to control the throttle
        float throttle;
        public float Throttle { get => throttle;
            set => throttle = Mathf.Clamp(value, -1f, 1f);
        }
        
        // Use this to disable drifting
        [HideInInspector] public bool allowDrift = true;
        bool drift;
        public bool Drift { get => drift;
            set => drift = value;
        }         

        // Use this to read the current car speed (you'll need this to make a speedometer)
        [SerializeField] float speed = 0.0f;
        public float Speed => speed;
        
        // Private variables set at the start
        Rigidbody rb = default;
        internal WheelCollider[] wheels = new WheelCollider[0];

        private void Awake()
        {
            carControls = new CarControls();
        }

        // Init rigidbody, center of mass, wheels and more
        void Start() {
#if MULTIOSCONTROLS
            Debug.Log("[ACP] Using MultiOSControls");
#endif

            rb = GetComponent<Rigidbody>();
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;

            if (rb != null && centerOfMass != null)
            {
                rb.centerOfMass = centerOfMass.localPosition;
            }

            wheels = GetComponentsInChildren<WheelCollider>();

            // Set the motor torque to a non null value because 0 means the wheels won't turn no matter what
            foreach (WheelCollider wheel in wheels)
            {
                wheel.motorTorque = 0.0001f;
            }
        }
        
        // Update everything
        void FixedUpdate () {
            // Mesure current speed
            speed = transform.InverseTransformDirection(rb.velocity).z * 3.6f;

            // Get all the inputs!
            if (isPlayer) {
                // Accelerate & brake
                if (throttleInput != "" && throttleInput != null)
                {
                    throttle = carControls.CarInput.Axis.ReadValue<Vector2>().y;
                }
                // Turn
                steering = turnInputCurve.Evaluate(carControls.CarInput.Axis.ReadValue<Vector2>().x) * steerAngle;
            }

            // Direction
            foreach (WheelCollider wheel in turnWheel)
            {
                wheel.steerAngle = Mathf.Lerp(wheel.steerAngle, steering, steerSpeed);
            }

            foreach (WheelCollider wheel in wheels)
            {
                wheel.motorTorque = 0.0001f;
                wheel.brakeTorque = 0;
            }

            if (throttle != 0 && (Mathf.Abs(speed) < 4 || Mathf.Sign(speed) == Mathf.Sign(throttle)))
            {
                foreach (WheelCollider wheel in driveWheel)
                {
                    wheel.motorTorque = throttle * motorTorque.Evaluate(speed) * diffGearing / driveWheel.Length;
                }
            }
            else if (throttle != 0)
            {
                foreach (WheelCollider wheel in wheels)
                {
                    wheel.brakeTorque = Mathf.Abs(throttle);
                }
            }

            // Drift
            if (drift && allowDrift) {
                Vector3 driftForce = -transform.right;
                driftForce.y = 0.0f;
                driftForce.Normalize();

                if (steering != 0)
                    driftForce *= rb.mass * speed/7f * throttle * steering/steerAngle;
                Vector3 driftTorque = transform.up * 0.1f * steering/steerAngle;


                rb.AddForce(driftForce * driftIntensity, ForceMode.Force);
                rb.AddTorque(driftTorque * driftIntensity, ForceMode.VelocityChange);             
            }
            
            // Downforce
            rb.AddForce(-transform.up * speed * downforce);
        }

        // Reposition the car to the start position
        public void ResetPos() {
            transform.position = spawnPosition;
            transform.rotation = spawnRotation;

            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // MULTIOSCONTROLS is another package I'm working on ignore it I don't know if it will get a release.
#if MULTIOSCONTROLS
        private static MultiOSControls _controls;
#endif

        private void OnEnable()
        {
            carControls.Enable();
        }

        private void OnDisable()
        {
            carControls.Disable();
        }
    }
}
