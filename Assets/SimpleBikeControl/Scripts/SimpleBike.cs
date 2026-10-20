using UnityEngine;

namespace KikiNgao.SimpleBikeControl
{
    public class SimpleBike : MonoBehaviour
    {
        [Tooltip("Control without biker")]
        public bool noBikerCtrl;

        private InputManager inputManager;
        public Transform bikerHolder;

        public WheelCollider frontWheelCollider;
        public WheelCollider rearWheelCollider;
        public GameObject frontWheel;
        public GameObject rearWheel;

        public Transform handlerBar;
        public Transform cranksetTransform;

        [SerializeField] private float legPower = 10;
        [Tooltip("speed multiply max")]
        [SerializeField] private float powerUpMax = 2;
        [Tooltip("define how fast to reach power up max")]
        [SerializeField] private float powerUpSpeed = .5f;
        [SerializeField] private float airResistance = 6;
        [Tooltip("Rigidbody Drag while standing")]
        [SerializeField] private float restDrag = 2f;
        [Tooltip("Rigidbody AngularDrag while standing")]
        [SerializeField] private float restAngularDrag = .2f;
        [Tooltip("ratio of wheels and crankset rotation ")]
        // Ex:forceRatio = 2f; mean when crankset rotate 1 round the wheels rotate 2 round
        [SerializeField] private float forceRatio = 2f;
        // default: speed [0 -> 50] Angle [1 -> 35]
        [SerializeField] private AnimationCurve frontWheelRestrictCurve = new AnimationCurve(new Keyframe(0f, 35f), new Keyframe(50f, 1f));

        [Header("Handling")]
        [Tooltip("How fast the handlebar turns toward the input direction (deg/sec)")]
        [SerializeField] private float steerSpeed = 180f;
        [Tooltip("How fast the handlebar returns to center when steering is released (deg/sec)")]
        [SerializeField] private float steerReturnSpeed = 240f;
        [Tooltip("0 = pure physics turning, 1 = bike turns exactly as the handlebar points")]
        [Range(0f, 1f)]
        [SerializeField] private float turnAssist = 0.35f;
        [Tooltip("0 = tyres alone hold the bike (can slide), 1 = no sideways sliding at all")]
        [Range(0f, 1f)]
        [SerializeField] private float sideGrip = 0.3f;
        [Tooltip("Rigidbody AngularDrag while riding, lower = easier turning")]
        [SerializeField] private float ridingAngularDrag = 1.5f;
        [Tooltip("Steering angle never goes below this, so the bike can still turn at high speed")]
        [SerializeField] private float minSteerAngle = 8f;
        [Tooltip("Top speed in km/h (multiplied by speed up power)")]
        [SerializeField] private float maxSpeedKm = 30f;
        [Tooltip("Brake torque when pressing back while moving forward")]
        [SerializeField] private float brakePower = 250f;
        [Tooltip("Rigidbody Drag while rolling without pedaling")]
        [SerializeField] private float coastDrag = 0.3f;

        [Header("Balance")]
        [Tooltip("Keep the bike upright by code instead of letting physics tip it over")]
        [SerializeField] private bool autoBalance = true;
        [SerializeField] private float balanceStrength = 8f;
        [Tooltip("Max lean into turns (visual + physics)")]
        [SerializeField] private float maxLeanAngle = 15f;
        [Tooltip("Lean angle while standing still")]
        [SerializeField] private float restLeanAngle = 5f;

        public GameObject MyDriver;
        public Transform leftHandTarget, rightHandTarget;
        public Transform leftPendalTarget, rightPendalTarget;
        public Transform leftStandTarget, rightStandTarget;

        private Transform centerOfMass;

        private Rigidbody m_Rigidbody;
        public Rigidbody GetRigidbody() => m_Rigidbody;


        [HideInInspector]
        public bool falling;
        private float fallingDrag = 1;
        private float fallingAngurlarDrag = 0.01f;

        private float currentSteerAngle;
        private Quaternion handlerBarBaseRotation;
        private float wheelBase = 1f;

        private float currentLegPower;
        private float reversePower;
        private EventManager eventManager;

        // pressing back while still rolling forward brakes instead of reversing
        private bool IsBraking() => inputManager.vertical < 0 && m_Rigidbody != null && GetForwardSpeedMs() > 0.5f;
        public bool IsReverse() => inputManager.vertical < 0 && !IsBraking();
        public bool IsMovingToward => inputManager.vertical > 0;
        private bool IsRest() => inputManager.vertical == 0;
        public bool IsMoving() => inputManager.vertical != 0;
        private bool IsSpeedUp() => inputManager.speedUp;
        private bool IsGrounded() => frontWheelCollider.isGrounded || rearWheelCollider.isGrounded;

        private float GetBikeSpeedKm() => GetBikeSpeedMs() * 3.6f;
        private float GetBikeSpeedMs() => m_Rigidbody.velocity.magnitude;
        private float GetForwardSpeedMs() => Vector3.Dot(m_Rigidbody.velocity, transform.forward);
        private float GetBikeAngle() => WrapAngle(transform.eulerAngles.z);

        public bool TiltToRight() => WrapAngle(transform.eulerAngles.z) <= 0;

        public bool Freeze { get => m_Rigidbody.isKinematic; set => m_Rigidbody.isKinematic = value; }
        public bool FreezeCrankset { get; set; }
        public bool ReadyToRide()
        {
            if (noBikerCtrl) return true;
            if (bikerHolder.childCount == 0) return false;
            if (bikerHolder.GetChild(0).CompareTag("Player")) return true;
            return false;
        }

        void Start()
        {
            inputManager = GameManager.Instance.GetInputManager;
            eventManager = GameManager.Instance.GetEventManager;

            CreateCenterOfMass();
            SettingRigidbody();
            if (handlerBar) handlerBarBaseRotation = handlerBar.localRotation;
            wheelBase = Mathf.Max(Vector3.Distance(frontWheelCollider.transform.position, rearWheelCollider.transform.position), 0.5f);

            currentLegPower = legPower * 10;
            reversePower = legPower * 3;
        }

        void OnDisable()
        {
            if (!ReadyToRide())
                SetParked(true);
        }

        //creat cemter of mass and add to the bike
        private void CreateCenterOfMass()
        {
            centerOfMass = new GameObject().transform;
            centerOfMass.name = "CenterOfMass";
            Vector3 center = new Vector3();
            Vector3 rearPosition = rearWheelCollider.transform.position;
            center.x = rearPosition.x;
            center.y = 0;
            center.z = rearPosition.z + (frontWheelCollider.transform.position.z - rearPosition.z) / 2;

            centerOfMass.transform.position = center;
            centerOfMass.parent = transform;
        }
        private void SettingRigidbody()
        {
            m_Rigidbody = transform.GetComponent<Rigidbody>();
            m_Rigidbody.centerOfMass = centerOfMass.localPosition;
        }
        float powerUp = 1f;
        bool emergencyStopped;
        bool parked;

        public void SetParked(bool value)
        {
            parked = value;
            if (m_Rigidbody == null)
                m_Rigidbody = GetComponent<Rigidbody>();
            if (m_Rigidbody == null)
                return;

            if (parked)
            {
                emergencyStopped = false;
                falling = false;
                m_Rigidbody.isKinematic = false;
                m_Rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
                m_Rigidbody.velocity = Vector3.zero;
                m_Rigidbody.angularVelocity = Vector3.zero;

                Vector3 euler = transform.eulerAngles;
                transform.rotation = Quaternion.Euler(0f, euler.y, 0f);

                // parked bike is a static object, the player on foot can't push it
                m_Rigidbody.isKinematic = true;
            }
            else
            {
                m_Rigidbody.constraints = RigidbodyConstraints.None;
                m_Rigidbody.isKinematic = false;
            }
        }

        public void EmergencyStop()
        {
            emergencyStopped = true;
            falling = false;

            if (m_Rigidbody == null)
                m_Rigidbody = GetComponent<Rigidbody>();

            ResetWheelsCollider();
            if (frontWheelCollider != null)
                frontWheelCollider.brakeTorque = 10000f;
            if (rearWheelCollider != null)
                rearWheelCollider.brakeTorque = 10000f;

            if (m_Rigidbody != null)
            {
                m_Rigidbody.velocity = Vector3.zero;
                m_Rigidbody.angularVelocity = Vector3.zero;
            }

            Freeze = true;
        }

        private void FixedUpdate()
        {
            if (emergencyStopped) return;

            if (parked && !ReadyToRide())
            {
                if (m_Rigidbody != null && !m_Rigidbody.isKinematic)
                    m_Rigidbody.angularVelocity = Vector3.zero;
                return;
            }

            // falling when exit bike 
            if (falling) { Falling(); return; };

            // <<< no control handle above
            if (!ReadyToRide()) return;
            // under control handle under >>

            UpdateSteering();

            // ready to ride and no key press
            if (IsRest()) Rest();
            else MovingBike();

            if (autoBalance) KeepBalance();
            ApplyTurnAssist();
            ApplySideGrip();

            UpdateLegPower(IsSpeedUp());
            if (!FreezeCrankset) UpdateCranksetRotation();
            UpdateWheelDisplay();
        }

        private void UpdateLegPower(bool speedUp)
        {
            if (speedUp)
            {
                powerUp += powerUpSpeed * Time.deltaTime;
                if (powerUp >= powerUpMax) powerUp = powerUpMax;
                currentLegPower = legPower * 10 * powerUp;

                eventManager?.OnSpeedUp();

                return;
                //Debug.Log(powerUp);
            }
            eventManager?.OnNormalSpeed();
            powerUp = 1f;
            currentLegPower = legPower * 10 * powerUp;
        }
        private void MovingBike()
        {
            Freeze = false;
            float speedMs = GetBikeSpeedMs();
            m_Rigidbody.drag = speedMs / m_Rigidbody.mass * airResistance;
            m_Rigidbody.angularDrag = ridingAngularDrag;

            frontWheelCollider.motorTorque = 0;
            if (IsBraking())
            {
                float brake = brakePower * -inputManager.vertical;
                frontWheelCollider.brakeTorque = brake;
                rearWheelCollider.brakeTorque = brake;
                rearWheelCollider.motorTorque = 0;
            }
            else
            {
                frontWheelCollider.brakeTorque = 0;
                rearWheelCollider.brakeTorque = 0;

                // no pedal power in the air or above top speed
                bool tooFast = IsMovingToward && GetBikeSpeedKm() >= maxSpeedKm * powerUp;
                if (tooFast || !IsGrounded()) rearWheelCollider.motorTorque = 0;
                else rearWheelCollider.motorTorque = (IsMovingToward ? currentLegPower : reversePower) * inputManager.vertical;
            }

            UpdateCenterOfMass();
        }

        // steering runs every step so the handlebar always eases back to center when input is released
        private void UpdateSteering()
        {
            // handlerBar is restricted while speed is high
            float maxAngle = Mathf.Max(frontWheelRestrictCurve.Evaluate(GetBikeSpeedKm()), minSteerAngle);
            float targetAngle = maxAngle * inputManager.horizontal;
            float rate = Mathf.Abs(targetAngle) < Mathf.Abs(currentSteerAngle) ? steerReturnSpeed : steerSpeed;
            currentSteerAngle = Mathf.MoveTowards(currentSteerAngle, targetAngle, rate * Time.fixedDeltaTime);

            frontWheelCollider.steerAngle = currentSteerAngle;
            //rotate handlerbar by local Y axis
            if (handlerBar) handlerBar.localRotation = handlerBarBaseRotation * Quaternion.Euler(0, currentSteerAngle, 0);
        }

        // hold the bike upright and lean it into turns, instead of balancing on physics alone
        private void KeepBalance()
        {
            if (m_Rigidbody.isKinematic) return;

            Vector3 forward = transform.forward;
            float lean;
            if (IsRest() && GetBikeSpeedMs() < 1f)
                lean = TiltToRight() ? -restLeanAngle : restLeanAngle;
            else
                lean = Mathf.Clamp(-currentSteerAngle * GetBikeSpeedMs() * 0.15f, -maxLeanAngle, maxLeanAngle);

            Quaternion target = Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(0, 0, lean);
            m_Rigidbody.MoveRotation(Quaternion.Slerp(m_Rigidbody.rotation, target, balanceStrength * Time.fixedDeltaTime));

            // remove roll spin so physics doesn't fight the balance
            Vector3 angularVelocity = m_Rigidbody.angularVelocity;
            m_Rigidbody.angularVelocity = angularVelocity - Vector3.Project(angularVelocity, forward);
        }

        // pull the yaw rate toward what the handlebar angle should give (simple bicycle model),
        // so turning follows the input instead of fighting tyre slip
        private void ApplyTurnAssist()
        {
            if (turnAssist <= 0f || m_Rigidbody.isKinematic || !IsGrounded()) return;

            float forwardSpeed = GetForwardSpeedMs();
            if (Mathf.Abs(forwardSpeed) < 0.5f) return;

            float targetYawRate = forwardSpeed * Mathf.Tan(currentSteerAngle * Mathf.Deg2Rad) / wheelBase;
            Vector3 angularVelocity = m_Rigidbody.angularVelocity;
            float currentYawRate = Vector3.Dot(angularVelocity, Vector3.up);
            float newYawRate = Mathf.Lerp(currentYawRate, targetYawRate, turnAssist);
            m_Rigidbody.angularVelocity = angularVelocity + Vector3.up * (newYawRate - currentYawRate);
        }

        // cancel part of the sideways velocity each step so the bike carves through turns instead of sliding
        private void ApplySideGrip()
        {
            if (sideGrip <= 0f || m_Rigidbody.isKinematic || !IsGrounded()) return;

            Vector3 velocity = m_Rigidbody.velocity;
            float sidewaysSpeed = Vector3.Dot(velocity, transform.right);
            m_Rigidbody.velocity = velocity - transform.right * (sidewaysSpeed * sideGrip);
        }

        // the bike keep moving slowly if we forgot reset motor force
        private void ResetWheelsCollider()
        {
            currentSteerAngle = 0f;
            frontWheelCollider.steerAngle = 0f;
            frontWheelCollider.motorTorque = 0;
            rearWheelCollider.motorTorque = 0;
            rearWheelCollider.brakeTorque = 0;
            frontWheelCollider.brakeTorque = 0;
        }

        // when had biker and no key press
        private void Rest()
        {
            // coast freely, only use the high stop drag once almost stopped
            float speedMs = GetBikeSpeedMs();
            bool almostStopped = speedMs < 1f;
            m_Rigidbody.drag = almostStopped ? restDrag : coastDrag;
            m_Rigidbody.angularDrag = almostStopped ? restAngularDrag : ridingAngularDrag;

            frontWheelCollider.motorTorque = 0;
            rearWheelCollider.motorTorque = 0;
            frontWheelCollider.brakeTorque = 0;
            rearWheelCollider.brakeTorque = 0;
            UpdateCenterOfMass();
        }
        //When biker exit bike it's start falling 
        public void Falling()
        {
            falling = true;
            // the bike auto stop due to high drag
            m_Rigidbody.drag = fallingDrag;
            m_Rigidbody.angularDrag = fallingAngurlarDrag;

            UpdateCenterOfMass();
            UpdateWheelDisplay();
            ResetWheelsCollider();

            //Debug.Log(gameObject.name + "  Falling ");
            float angle = GetBikeAngle();
            if (angle < -75 || angle > 75) { Freeze = true; falling = false; }
        }
        private void UpdateCranksetRotation()
        {
            cranksetTransform.rotation *= Quaternion.Euler(GetBikeSpeedKm() / forceRatio, 0, 0);
            Quaternion ro = Quaternion.Euler(-GetBikeSpeedKm() / forceRatio, 0, 0);
            cranksetTransform.GetChild(0).rotation *= ro;
            cranksetTransform.GetChild(1).rotation *= ro;
        }

        private void UpdateWheelDisplay()
        {
            Vector3 temporaryVector;
            Quaternion temporaryQuaternion;

            rearWheelCollider.GetWorldPose(out temporaryVector, out temporaryQuaternion);
            rearWheel.transform.position = temporaryVector;

            // rpm * 6 = degrees per second
            rearWheel.transform.rotation *= Quaternion.Euler(rearWheelCollider.rpm * 6f * Time.fixedDeltaTime, 0, 0);

            frontWheel.transform.localRotation = rearWheel.transform.localRotation;
        }

        private void UpdateCenterOfMass()
        {
            var centerLocal = centerOfMass.localPosition;

            if (!falling)
            {
                if (IsRest())// when bike rest 
                {
                    centerLocal.y = 0;
                    centerLocal.x = TiltToRight() ? .01f : -.01f;
                }
                else centerLocal.y = -0.8f; //set center local y under ground to make bike tilt while riding 
            }
            else //falling
            {
                centerLocal.y = 0;
                centerLocal.x = TiltToRight() ? .2f : -0.2f;
            }

            // when bike falling
            m_Rigidbody.centerOfMass = centerLocal;
        }

        private bool OnGround(WheelCollider wheelCollider)
        {
            if (Physics.Raycast(wheelCollider.transform.position, -transform.up, out RaycastHit hit, wheelCollider.radius + 0.1f))
            {
                return true;
            }
            return false;
        }
        // convert runtime Euler Angle to angle that showing in Unity Editor 
        private static float WrapAngle(float angle)
        {
            angle %= 360;
            if (angle > 180)
                return angle - 360;

            return angle;
        }

    }
}
