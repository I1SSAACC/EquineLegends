using UnityEngine;
using MalbersAnimations.Scriptables;
using System.Collections;
using UnityEngine.Events;

namespace MalbersAnimations
{
    public class MFreeLookCamera : MonoBehaviour
    {
#if REWIRED
        [Header("Rewired Connection")]
#else
        [HideInInspector]
#endif
        public string PlayerID = "Player0";

        [Space]

        public Transform m_Target;
        public UpdateType updateType = UpdateType.FixedUpdate;
        internal UpdateType defaultUpdate;

        public float m_MoveSpeed = 10f;
        [Range(0f, 10f)]
        public float m_TurnSpeed = 10f;
        public float m_TurnSmoothing = 10f;
        public float m_TiltMax = 75f;
        public float m_TiltMin = 45f;

        [SerializeField] private MouseSensitivitySlider _mouseSensitivitySlider;

        [Header("Camera Input Axis")]
        public InputAxis Vertical = new InputAxis("Mouse Y", true, false);
        public InputAxis Horizontal = new InputAxis("Mouse X", true, false);
        private IGravity TargetGravity;

        [Space]
        public FreeLockCameraManager manager;
        public FreeLookCameraState DefaultState;
        public Transform DefaultTarget { get; set; }

        [HideInInspector] public UnityEvent OnStateChange = new UnityEvent();

        [Space, Header("Sprint Field of View"), Tooltip("Additional FOV when Sprinting")]
        public FloatReference SprintFOV = new FloatReference(10f);
        [Tooltip("Additional FOV when Sprinting")]
        public FloatReference FOVTransition = new FloatReference(1f);

        private float m_LookAngle;
        private float m_TiltAngle;
        private Vector3 m_PivotEulers;
        private Vector3 m_UpVector;
        private Quaternion m_PivotTargetRot;
        private Quaternion m_TransformTargetRot;

        protected FreeLookCameraState NextState;
        protected FreeLookCameraState currentState;

        IEnumerator IChangeStates;
        IEnumerator IChange_FOV;

        public Transform Target
        {
            get => m_Target;
            set
            {
                m_Target = value;
                GetTargetGravity();
            }
        }

        public Camera Cam { get; private set; }

        public Transform CamT { get; private set; }

        public Transform Pivot { get; private set; }

        public float XCam { get; set; }

        public float YCam { get; set; }

        public float ActiveFOV { get; internal set; }

        private IInputSystem inputSystem;

        protected void Awake()
        {
            Cam = GetComponentInChildren<Camera>();
            CamT = GetComponentInChildren<Camera>().transform;
            Pivot = Cam.transform.parent;

            currentState = null;
            NextState = null;

            if (manager) manager.SetCamera(this);

            if (DefaultState) Set_State(DefaultState);

            m_PivotEulers = Pivot.rotation.eulerAngles;
            m_PivotTargetRot = Pivot.transform.localRotation;
            m_TransformTargetRot = transform.localRotation;

            ActiveFOV = Cam.fieldOfView;

            inputSystem = DefaultInput.GetInputSystem(PlayerID);

            Horizontal.InputSystem = Vertical.InputSystem = inputSystem;


            defaultUpdate = updateType;

            if (DefaultState == null)
            {
                DefaultState = ScriptableObject.CreateInstance<FreeLookCameraState>();

                DefaultState.CamFOV = Cam.fieldOfView;
                DefaultState.PivotPos = Pivot.localPosition;
                DefaultState.CamPos = CamT.localPosition;
                DefaultState.name = "Default State";
                OnStateChange.Invoke();
            }
        }

        void Start()
        {
            GetTargetGravity();
        }

        void GetTargetGravity()
        {
            if (Target)
                TargetGravity = Target?.GetComponentInChildren<IGravity>() ?? Target?.GetComponentInParent<IGravity>();
        }


        public virtual void Set_State(FreeLookCameraState state)
        {
            Pivot.localPosition = state.PivotPos;
            Cam.transform.localPosition = state.CamPos;
            Cam.fieldOfView = ActiveFOV = state.CamFOV;
            OnStateChange.Invoke();

            Debug.Log("Set_State" + state.name);
        }

        #region Private Methods
        protected void FollowTarget(float deltaTime)
        {
            if (m_Target == null) return;

            transform.position = Vector3.Lerp(transform.position, m_Target.position, deltaTime * m_MoveSpeed);
        }

        internal void UpdateState(FreeLookCameraState state)
        {
            if (state == null) return;

            Pivot.localPosition = state.PivotPos;
            CamT.localPosition = state.CamPos;
            Cam.fieldOfView = ActiveFOV = state.CamFOV;

            OnStateChange.Invoke();
        }


        public void EnableInput(bool value)
        {
            Vertical.active = value;
            Horizontal.active = value;
        }


        private void HandleRotationMovement(float time)
        {
            float mouseSensitivitySliderValue = _mouseSensitivitySlider.Value;

            if (Time.timeScale < float.Epsilon) return;

            if (Horizontal.active) XCam = Horizontal.GetAxis;
            if (Vertical.active) YCam = Vertical.GetAxis;

            m_LookAngle += XCam * m_TurnSpeed * mouseSensitivitySliderValue;

            if (TargetGravity != null) m_UpVector = Vector3.Slerp(m_UpVector, TargetGravity.UpVector, time * 15);
            m_TransformTargetRot = Quaternion.FromToRotation(transform.up, m_UpVector) * Quaternion.Euler(0f, m_LookAngle, 0f);
            m_TransformTargetRot = Quaternion.Euler(0f, m_LookAngle, 0f);
            m_TiltAngle -= YCam * m_TurnSpeed * mouseSensitivitySliderValue;
            m_TiltAngle = Mathf.Clamp(m_TiltAngle, -m_TiltMin, m_TiltMax);

            m_PivotTargetRot = Quaternion.Euler(m_TiltAngle, m_PivotEulers.y, m_PivotEulers.z);

            Pivot.localRotation = Quaternion.Slerp(Pivot.localRotation, m_PivotTargetRot, m_TurnSmoothing * time);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, m_TransformTargetRot, m_TurnSmoothing * time);
        }


        void FixedUpdate()
        {
            if (updateType == UpdateType.FixedUpdate)
            {
                FollowTarget(Time.fixedDeltaTime);
                HandleRotationMovement(Time.fixedDeltaTime);
            }
        }

        void LateUpdate()
        {
            if (updateType == UpdateType.LateUpdate)
            {
                FollowTarget(Time.deltaTime);
                HandleRotationMovement(Time.deltaTime);
            }
        }
        #endregion

        public void Set_State_Smooth(FreeLookCameraState state) => SetState(state, false);

        public void Set_State_Temporal(FreeLookCameraState state) => SetState(state, true);


        internal void SetState_Instant(FreeLookCameraState state, bool temporal)
        {
            if (state == null) return;
            if (currentState && state == currentState) return;

            NextState = state;

            if (IChangeStates != null) StopCoroutine(IChangeStates);

            if (!temporal) DefaultState = state;

            UpdateState(state);
        }


        internal void SetState(FreeLookCameraState state, bool temporal)
        {
            if (state == null) return;
            if (currentState && state == currentState) return;

            NextState = state;

            if (IChangeStates != null) StopCoroutine(IChangeStates);

            if (!temporal) DefaultState = state;

            IChangeStates = StateTransition(state.transition);
            StartCoroutine(IChangeStates);
        }

        public void Set_State_Default_Smooth() => SetState(DefaultState, true);

        public void Set_State_Default() => Set_State(DefaultState);

        public void ToggleSprintFOV(bool val) => ChangeFOV(val ? ActiveFOV + SprintFOV.Value : ActiveFOV);

        public void ChangeFOV(float newFOV)
        {
            if (IChange_FOV != null) StopCoroutine(IChange_FOV);

            IChange_FOV = C_SprintFOV(newFOV, FOVTransition);
            StartCoroutine(IChange_FOV);
        }


        #region Coroutines
        private IEnumerator StateTransition(float time)
        {
            float elapsedTime = 0;
            currentState = NextState;

            while (elapsedTime < time)
            {
                Pivot.localPosition = Vector3.Lerp(Pivot.localPosition, NextState.PivotPos, Mathf.SmoothStep(0, 1, elapsedTime / time));
                CamT.localPosition = Vector3.Lerp(CamT.localPosition, NextState.CamPos, Mathf.SmoothStep(0, 1, elapsedTime / time));
                Cam.fieldOfView = ActiveFOV = Mathf.Lerp(Cam.fieldOfView, NextState.CamFOV, Mathf.SmoothStep(0, 1, elapsedTime / time));
                OnStateChange.Invoke();
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            UpdateState(NextState);

            NextState = null;
            yield return null;
        }
        private IEnumerator C_SprintFOV(float newFOV, float time)
        {
            float elapsedTime = 0f;
            float startFOV = Cam.fieldOfView;

            while (elapsedTime < time)
            {
                Cam.fieldOfView = Mathf.Lerp(startFOV, newFOV, Mathf.SmoothStep(0, 1, elapsedTime / time));
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            Cam.fieldOfView = newFOV;
            yield return null;
        }
        #endregion


        public virtual void Target_Set(Transform newTransform) => m_Target = DefaultTarget = newTransform;

        public virtual void Target_Set_Temporal(Transform newTransform) => Target = newTransform;

        public virtual void Target_Restore() => Target = DefaultTarget;

        public virtual void Target_Set(GameObject newGO) => Target_Set(newGO.transform);
        public virtual void Target_Set_Temporal(GameObject newGO) => Target_Set_Temporal(newGO.transform);

        public virtual void ForceUpdateMode(bool val) => updateType = val ? UpdateType.LateUpdate : defaultUpdate;
    }
}