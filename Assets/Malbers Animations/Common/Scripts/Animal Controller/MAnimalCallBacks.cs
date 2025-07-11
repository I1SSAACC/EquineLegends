﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MalbersAnimations.Scriptables;
using UnityEngine.Events;

namespace MalbersAnimations.Controller
{
    /// All Callbacks/Public Methods are Here
    public partial class MAnimal
    {
        #region INPUTS
        /// <summary>Updates all the Input from Malbers Input in case needed (Rewired conextion(</summary>
        public virtual void UpdateInputSource(bool connect = true)
        {
            InputSource = this.FindComponent<IInputSource>(); //Find if we have a InputSource

            if (InputSource != null)
            {
                foreach (var state in states)
                {
                    SetStatesInput(state, false);
                    if (connect) SetStatesInput(state, true);
                }

                foreach (var mode in modes)
                {
                    SetModesInput(mode, false);
                    if (connect) SetModesInput(mode, true);
                }
            }
        }

        /// <summary> Get the Inputs for the Source to add it to the States </summary>
        internal virtual void SetStatesInput(State state, bool connect)
        {
            if (!string.IsNullOrEmpty(state.Input)) //If a State has an Input 
            {
                    var input = InputSource.GetInput(state.Input);

                    if (input != null)
                    {
                        if (connect)
                            input.InputChanged.AddListener(state.ActivatebyInput);
                        else
                            input.InputChanged.RemoveListener(state.ActivatebyInput);
                    }
                }
        }

        /// <summary> Get the Inputs for the Source to add it to the States </summary>
        internal virtual void SetModesInput(Mode mode, bool connect)
        {
            if (!string.IsNullOrEmpty(mode.Input)) //If a mode has an Input 
            {
                var input = InputSource.GetInput(mode.Input);

                if (input != null)
                {
                    if (connect)
                        input.InputChanged.AddListener(mode.ActivatebyInput);
                    else
                        input.InputChanged.RemoveListener(mode.ActivatebyInput);
                }
            }
        }
        #endregion

        #region Player
        /// <summary>Set an Animal as the Main Player and remove the otherOne</summary>
        public virtual void SetMainPlayer()
        {
            if (MainAnimal) //if there's a main animal already seted
            {
                MainAnimal.isPlayer.Value = false;
            }

            this.isPlayer.Value = true;
            MainAnimal = this;
        }

        public void DisableMainPlayer()
        {
            if (MainAnimal == this) MainAnimal = null;
        }
        #endregion

        #region Teleport    
        public virtual void Teleport(Transform newPos)
        {
            if (newPos)
            {
                Teleport(newPos.position);
            }
            else
            {
                Debug.LogWarning("You are using Teleport but the Transform you are entering on the parameters is null");
            }
        }

        public virtual void TeleportRot(Transform newPos)
        {
            if (newPos)
            {
                Teleport(newPos.position);
                transform.rotation = newPos.rotation;
            }
            else
            {
                Debug.LogWarning("You are using TeleportRot but the Transform you are entering on the parameters is null");
            }
        }


        public virtual void Teleport(Vector3 newPos)
        {
          //  Debug.Log("Teleport00");
            base.transform.position = newPos;
            LastPos = base.transform.position;
            platform = null;
        }

        #endregion

        #region Gravity
        /// <summary>Resets the gravity to the default Vector.Down value</summary>
        public void ResetGravityDirection() => Gravity = Vector3.down;

        /// <summary>Clears the Gravity Logic</summary>
        internal void ResetGravityValues()
        {
            GravityTime = m_gravityTime;
            GravityStoredVelocity = Vector3.zero;
            GravitySpeed = 0;
        }

        internal void ResetExternalForce()
        {
            CurrentExternalForce = Vector3.zero;
            ExternalForce = Vector3.zero;
            ExternalForceAcel = 0;
            // Debug.Log("Reset external force");
        }

        internal void ResetUPVector()
        {
            RB.linearVelocity = Vector3.ProjectOnPlane(RB.linearVelocity, UpVector); //Cleann the Gravity IMPORTAAANT!!!!!
         //   RB.velocity = Vector3.zero;

            //Extra OPTIONAL
            AdditivePosition = Vector3.ProjectOnPlane(AdditivePosition, UpVector);
            DeltaPos = Vector3.ProjectOnPlane(DeltaPos, UpVector);
        }

        /// <summary>The Ground</summary>
        public void GroundChangesGravity(bool value)
        {
            if (value)
                UseCameraInput = false;
            else
                UseCameraInput = DefaultCameraInput;

            ground_Changes_Gravity = value;
        }
        /// <summary>Aling with no lerp to the Gravity Direction</summary>
        public void AlignGravity()
        {
            Quaternion AlignRot = Quaternion.FromToRotation(transform.up, UpVector) * transform.rotation;  //Calculate the orientation to Terrain 
            base.transform.rotation = AlignRot;
        }
        #endregion

        #region Stances
        /// <summary>Toogle the New Stance with the Default Stance▼▲ </summary>
        public void Stance_Toggle(int NewStance) => Stance = (Stance == NewStance) ? DefaultStance : NewStance;

        /// <summary>Toogle the New Stance with the Default Stance▼▲ </summary>
        public void Stance_Toggle(StanceID NewStance) => Stance_Toggle(NewStance.ID);

        public void Stance_Set(StanceID id) => Stance = id ?? 0;
        public void Stance_Set(int id) => Stance = id;
        public void Stance_Reset() => Stance = defaultStance;

        #endregion

        #region Animator Methods

        /// <summary>  Method required for the Interface IAnimator Listener to send messages From the Animator to any class who uses this Interface</summary>
        public virtual bool OnAnimatorBehaviourMessage(string message, object value)
        {
            foreach (var state in states) state.ReceiveMessages(message, value);

            return this.InvokeWithParams(message, value);
        }

        /// <summary>Set a Int on the Animator</summary>
        public void SetAnimParameter(int hash, int value) => Anim.SetInteger(hash, value);

        /// <summary>Set a float on the Animator</summary>
        public void SetAnimParameter(int hash, float value) => Anim.SetFloat(hash, value);

        /// <summary>Set a Bool on the Animator</summary>
        public void SetAnimParameter(int hash, bool value) => Anim?.SetBool(hash, value);

        /// <summary>Set a Trigger to the Animator</summary>
        public void SetAnimParameter(int hash) => Anim.SetTrigger(hash);

        public void SetOptionalAnimParameter(int Hash, float value)
        {
            if (animatorParams.ContainsKey(Hash)) SetFloatParameter(Hash, value);
        }

        private void SetOptionalAnimParameter(int Hash, int value)
        {
            if (animatorParams.ContainsKey(Hash))
                SetIntParameter?.Invoke(Hash, value);
        }

        private void SetOptionalAnimParameter(int Hash, bool value)
        {
            if (animatorParams.ContainsKey(Hash)) SetBoolParameter(Hash, value);
        }


        /// <summary> Set the Parameter Random to a value and pass it also to the Animator </summary>
        public void SetRandom(int value)
        {
            if (!enabled || Sleep) return;

            RandomID = Randomizer/* && !IsPlayingMode */? value : 0;
            SetOptionalAnimParameter(hash_Random, RandomID);
        }



        /// <summary>Used by Animator Events </summary>
        public virtual void EnterTag(string tag) => AnimStateTag = Animator.StringToHash(tag);
        #endregion

        #region States
        /// <summary> Set the Parameter Float ID to a value and pass it also to the Animator </summary>
        public void State_SetFloat(float value) => SetFloatParameter(hash_StateFloat, State_float = value);

        /// <summary>Find an old State and replace it for  a new one at RunTime </summary>
        public void State_Replace(State NewState)
        {
            if (CloneStates)
            {
                State instance = (State)ScriptableObject.CreateInstance(NewState.GetType());
                instance = ScriptableObject.Instantiate(NewState);                                 //Create a clone from the Original Scriptable Objects! IMPORTANT
                instance.name = instance.name.Replace("(Clone)", "(C)");
                NewState = instance;
            }

            var oldState = states.Find(s => s.ID == NewState.ID);

            if (oldState)
            {
                var index = states.IndexOf(oldState);
                var oldStatePriority = oldState.Priority;

                if (CloneStates) Destroy(oldState); //Destroy the Clone

                oldState = NewState;
                oldState.SetAnimal(this);
                oldState.Priority = oldStatePriority;
                oldState.AwakeState();
                oldState.InitializeState();
                oldState.ExitState();


                states[index] = oldState; //Replace the list Item

                UpdateInputSource(); //Need to Update the Sources
            }
        }

        /// <summary>Force the Activation of an state regarding if is enable or not</summary>
        public virtual void State_Force(StateID ID) => State_Force(ID.ID);

        /// <summary>Returns if the Animal has a state by its ID</summary>
        public bool HasState(StateID ID) => HasState(ID.ID);


        /// <summary>Returns if the Animal has a state by its int ID value</summary>
        public bool HasState(int ID) => State_Get(ID) != null;

        /// <summary>Returns if the Animal has a state by its name</summary>
        public bool HasState(string statename) => states.Find(s => s.name == statename) != null;

        public virtual void State_SetStatus(int status) => SetOptionalAnimParameter(hash_StateStatus, status);

        public virtual void State_Enable(StateID ID) => State_Enable(ID.ID);
        public virtual void State_Disable(StateID ID) => State_Disable(ID.ID);

        public virtual void State_Enable(int ID) => State_Get(ID)?.Enable(true);

        public virtual void State_Disable(int ID) => State_Get(ID)?.Enable(false);

        /// <summary>Force the Activation of an state regarding if is enable or not</summary>
        public virtual void State_Force(int ID)
        {
            State state = State_Get(ID);

            if (state == ActiveState)
                state.EnterCoreAnimation(); //Little HACK
            else
                state?.ForceActivate();
        }

        /// <summary>  Allow Lower States to be activated  </summary>
        public virtual void State_AllowExit(StateID ID) => State_AllowExit(ID.ID);

        /// <summary>  Allow Lower States to be activated  </summary>
        public virtual void State_AllowExit(int ID)
        {
            State state = State_Get(ID);
            if (state && state != ActiveState) return; //Do not Exit if we are not the Active State
            state?.AllowExit();
        }
        
        public virtual void State_AllowExit() => ActiveState.AllowExit();
        public virtual void State_InputTrue(StateID ID) => State_Get(ID)?.SetInput(true);
        public virtual void State_InputFalse(StateID ID) => State_Get(ID)?.SetInput(false);
        public virtual void ActiveStateAllowExit() => ActiveState.AllowExit();


        /// <summary>Try to Activate a State direclty from the Animal Script </summary>
        public virtual void State_Activate(StateID ID) => State_Activate(ID.ID);
       
        public virtual bool State_TryActivate(int ID)
        {
            State NewState = State_Get(ID);
            if (NewState && NewState.CanBeActivated )
            {
                if (!NewState.QUEUED())
                    return NewState.TryActivate();
            }
            return false;
        }

        /// <summary>Try to Activate a State direclty from the Animal Script </summary>
        public virtual void State_Activate(int ID)
        {
            State NewState = State_Get(ID);
            
            if (NewState && NewState.CanBeActivated)
            {
                if (!NewState.QUEUED())
                    NewState.Activate();
            }
        }

        /// <summary> Return a State by its  ID value </summary>
        public virtual State State_Get(int ID) => states.Find(s => s.ID == ID);

        /// <summary> Return a State by its ID</summary>
        public virtual State State_Get(StateID ID)
        {
            if (ID == null) return null;
            return State_Get(ID.ID);
        }

        /// <summary> Call the Reset State on the given State </summary>
        public virtual void State_Reset(int ID) => State_Get(ID)?.ResetState();

        /// <summary> Call the Reset State on the given State </summary>
        public virtual void State_Reset(StateID ID) => State_Reset(ID.ID);

        ///<summary> Find to the Possible State and store it to the (PreState) using an StateID value</summary>
        public virtual void State_Pin(StateID stateID) => State_Pin(stateID.ID);

        ///<summary> Find to the Possible State and store it to the (PreState) using an int value</summary>
        public virtual void State_Pin(int stateID) => Pin_State = State_Get(stateID);

        ///<summary>Use the (PreState) the and Try to activate it using an Input</summary>
        public virtual void State_Pin_ByInput(bool input) => Pin_State?.ActivatebyInput(input);

        ///<summary> Send to the Possible State (PreState) the value of the Input</summary>
        public virtual void State_Activate_by_Input(StateID stateID, bool input) => State_Activate_by_Input(stateID.ID, input);

        ///<summary> Send to the Possible State (PreState) the value of the Input</summary>
        public virtual void State_Activate_by_Input(int stateID, bool input)
        {
            State_Pin(stateID);
            State_Pin_ByInput(input);
        }
        #endregion

        #region Modes
        public bool HasMode(ModeID ID) => HasMode(ID.ID);

        /// <summary> Returns if the Animal has a mode By its ID</summary>
        public bool HasMode(int ID) => Mode_Get(ID) != null;

        /// <summary> Returns a Mode by its ID</summary>
        public virtual Mode Mode_Get(ModeID ModeID) => Mode_Get(ModeID.ID);

        /// <summary> Returns a Mode by its ID</summary>
        public virtual Mode Mode_Get(int ModeID) => modes.Find(m => m.ID == ModeID);

        /// <summary> Set the Parameter Int ID to a value and pass it also to the Animator </summary>
        public void SetModeStatus(int value) => SetIntParameter?.Invoke(hash_ModeStatus, ModeInt = value);
        public void Mode_SetPower(float value) => SetOptionalAnimParameter(hash_ModePower, ModePower = value);

        /// <summary>Activate a Random Ability on the Animal using a Mode ID</summary>
        public virtual void Mode_Activate(ModeID ModeID) => Mode_Activate(ModeID.ID, -99);

        /// <summary>Enable a mode on the Animal</summary>
        /// <param name="ModeID">ID of the Mode</param>
        /// <param name="AbilityIndex">Ability Index. If this value is -1 then the Mode will activate a random Ability</param>
        public virtual void Mode_Activate(ModeID ModeID, int AbilityIndex) => Mode_Activate(ModeID.ID, AbilityIndex);

        #region INTERFACE ICHARACTER ACTION
        public bool PlayAction(int Set, int Index) => Mode_TryActivate(Set, Index);

        public bool ForceAction(int Set, int Index) => Mode_ForceActivate(Set, Index);

        public bool IsPlayingAction => IsPlayingMode;
        #endregion

        /// <summary>Activate a mode on the Animal combining the Mode and Ability e.g 4002</summary>
        public virtual void Mode_Activate(int ModeID)
        {
            if (ModeID == 0) return;

            var id = Mathf.Abs(ModeID / 1000);

            if (id == 0)
            {
                Mode_Activate(ModeID, -99);
            }
            else
            {
                Mode_Activate(id, ModeID % 100);
            }
        }
       
        /// <summary>Activate a mode on the Animal</summary>
        /// <param name="ModeID">ID of the Mode</param>
        /// <param name="AbilityIndex">Ability Index. If this value is -99 then the Mode will activate a random Ability</param>
        public virtual void Mode_Activate(int ModeID, int AbilityIndex)
        {
            var mode = Mode_Get(ModeID);

            if (mode != null)
            {
                Pin_Mode = mode;
                Pin_Mode.AbilityIndex = AbilityIndex;
                Pin_Mode.TryActivate();
            }
            else
            {
                Debug.LogWarning("You are trying to Activate a Mode but here's no Mode with the ID or is Disabled: " + ModeID);
            }
        }

        public virtual bool Mode_ForceActivate(ModeID ModeID, int AbilityIndex) => Mode_ForceActivate(ModeID.ID, AbilityIndex);

        public virtual void Mode_ForceActivate(ModeID ModeID)
        {
            var mode = Mode_Get(ModeID);
            if (mode != null) Mode_ForceActivate(ModeID.ID, mode.AbilityIndex);
        }

        public bool Mode_ForceActivate(int ModeID, int AbilityIndex)
        {
            var mode = Mode_Get(ModeID);

            if (mode != null)
            {
                Pin_Mode = mode;
                return Pin_Mode.ForceActivate(AbilityIndex);
            }
            return false;
        }


        /// <summary>
        /// Returns True and Activate  the mode in case ir can be Activated, if not it will return false</summary>
        public bool Mode_TryActivate(int ModeID, int AbilityIndex = -1)
        {
            var mode = Mode_Get(ModeID);

            if (mode != null)
            {
                Pin_Mode = mode;
                Pin_Mode.AbilityIndex = AbilityIndex;
                return Pin_Mode.TryActivate();
            }
            return false;
        }


        /// <summary>Stop all modes </summary>
        public virtual void Mode_Stop()
        {
            if (IsPlayingMode)
            {
                activeMode.InputValue = false;
                Mode_Interrupt();
            }
            else
            {
                SetModeStatus(ModeAbility = Int_ID.Available);
                ModeStatus = MStatus.None; //IMPORTANT!
            }

           
            ActiveMode = null;
            InputMode = null;    //IT will not allow to do Continous activation REMOVED
            ModeTime = 0;                            //Reset Mode Time 
        }
 

        /// <summary>Set IntID to -2 to exit the Mode Animation</summary>
        public virtual void Mode_Interrupt()
        {
            SetModeStatus(Int_ID.Interrupted);//Means the Mode is interrupted
            if (IsPlayingMode) ModeStatus = MStatus.Interrupted;
        }


        public virtual void Mode_DisableAll()
        {
            foreach (var mod in modes)
                mod.Disable();
        }

        /// <summary>Disable a Mode by his ID</summary>
        public virtual void Mode_Disable(ModeID id) => Mode_Disable((int)id);

        /// <summary>Disable a Mode by his ID</summary>
        public virtual void Mode_Disable(int id)
        {
            var mod = Mode_Get(id);
            if (mod != null)
            {
                mod.Disable();
            }
        }


        /// <summary>Enable a Mode by his ID</summary>
        public virtual void Mode_Enable(ModeID id) => Mode_Enable(id.ID);

        /// <summary>Enable a Mode by his ID</summary>
        public virtual void Mode_Enable(int id)
        {
            var newMode = Mode_Get(id);
            if (newMode != null)
                newMode.Active = true;
        }


        /// <summary>Pin a mode to Activate later</summary>
        public virtual void Mode_Pin(ModeID ID)
        {
            if (Pin_Mode != null && Pin_Mode.ID == ID) return;  //the mode is already pinned

            var pin = Mode_Get(ID);

            Pin_Mode = null; //Important! Clean the Pin Mode 

            if (pin == null)
                Debug.LogWarning("There's no " + ID.name + "Mode");
            else if (pin.Active)
                Pin_Mode = pin;
        }

    


        /// <summary>Pin an Ability on the Pin Mode to Activate later</summary>
        public virtual void Mode_Pin_Ability(int AbilityIndex)
        {
            if (AbilityIndex == 0) return;

            Pin_Mode?.SetAbilityIndex(AbilityIndex);
        }


        /// <summary>Changes  Pinned Mode Status in all the Abilities</summary>
        public virtual void Mode_Pin_Status(int aMode)
        {
            if (Pin_Mode != null)
            //   Pin_Mode.Global.Status = (AbilityStatus)aMode;
            {
                foreach (var ab in Pin_Mode.Abilities)
                    ab.Properties.Status = (AbilityStatus)aMode;
            }
        }

        /// <summary>Changes the Pinned Mode time when using Hold by time Status</summary>
        public virtual void Mode_Pin_Time(float time)
        {
            if (Pin_Mode != null)
                //Pin_Mode.Global.HoldByTime = time;
                foreach (var ab in Pin_Mode.Abilities)
                    ab.Properties.HoldByTime = time;
        }

        public virtual void Mode_Pin_Enable(bool value) => Pin_Mode?.SetActive(value);
        public virtual void Mode_Pin_EnableInvert(bool value) => Pin_Mode?.SetActive(!value);

        public virtual void Mode_Pin_Input(bool value) => Pin_Mode?.ActivatebyInput(value);

        /// <summary>Tries to Activate the Pin Mode</summary>
        public virtual void Mode_Pin_Activate() => Pin_Mode?.TryActivate();

        /// <summary>Tries to Activate the Pin Mode with an Ability</summary>
        public virtual void Mode_Pin_AbilityActivate(int AbilityIndex)
        {
            if (AbilityIndex == 0) return;

            if (Pin_Mode != null)
            {
                Pin_Mode.AbilityIndex = AbilityIndex;
                Pin_Mode.TryActivate();
            }
        }
        #endregion

        #region Movement


        public virtual void Strafe_Toogle() => Strafe ^= true;


        /// <summary>Gets the movement from the Input Script or AI</summary>
        public virtual void Move(Vector3 move) => MoveDirection(move);

        /// <summary>Gets the movement from the Input using a 2 Vector  (ex UI Axis Joystick)</summary>
        public virtual void Move(Vector2 move) => MoveDirection(new Vector3(move.x, 0, move.y));

        /// <summary>Gets the movement from the Input ignoring the Direction Vector, using a 2 Vector  (ex UI Axis Joystick)</summary>
        public virtual void MoveWorld(Vector2 move) => MoveWorld(new Vector3(move.x, 0, move.y));

        /// <summary>Stop the animal from moving, cleaning the Movement Axis</summary>
        public virtual void StopMoving() => Move(Vector3.zero);

        /// <summary>Add Inertia to the Movement</summary>d
        public virtual void AddInertia(ref Vector3 Inertia, float speed = 1f)
        {
            AdditivePosition += Inertia;
            Inertia = Vector3.Lerp(Inertia, Vector3.zero, DeltaTime * speed);
        } 
        #endregion

        #region Speeds
        /// <summary>Change the Speed Up</summary>
        public virtual void SpeedUp() => Speed_Add(+1);

        /// <summary> Changes the Speed Down </summary>
        public virtual void SpeedDown() => Speed_Add(-1);

        /// <summary> Get a SpeedSet by its name</summary>
        public virtual MSpeedSet SpeedSet_Get(string name) => speedSets.Find(x => x.name == name);

        public virtual MSpeed Speed_GetModifier(string name, int index)
        {
            var set = SpeedSet_Get(name);

            if (set != null && index < set.Speeds.Count)
                return set[index - 1];

            return MSpeed.Default;
        }

        /// <summary>Set a custom speed created via script and it uses it as the Current Speed Modifier (used on the Fall and Jump State)</summary>
        public virtual void SetCustomSpeed(MSpeed customSpeed, bool keepInertiaSpeed = false)
        {
            CustomSpeed = true;
            CurrentSpeedModifier = customSpeed;

            currentSpeedSet = null; //IMPORTANT SET THE CURRENT SPEED SET TO NULL

            if (keepInertiaSpeed)
                InertiaPositionSpeed = TargetSpeed; //Set the Target speed to the Fall Speed so there's no Lerping when the speed changes
        }

        private void Speed_Add(int change) => CurrentSpeedIndex += change;

        /// <summary> Set an specific Speed for a State </summary>
        public virtual void Speed_CurrentIndex_Set(int speedIndex) => CurrentSpeedIndex = speedIndex;

        /// <summary> Set an specific Speed for a State using IntVars </summary>
        public virtual void Speed_CurrentIndex_Set(IntVar speedIndex) => CurrentSpeedIndex = speedIndex;

        /// <summary>Lock Speed Changes on the Animal</summary>
        public virtual void Speed_Change_Lock(bool lockSpeed) => SpeedChangeLocked = lockSpeed;

        public virtual void SpeedSet_Set_Active(string SpeedSetName, int activeIndex)
        {
            var speedSet = SpeedSet_Get(SpeedSetName);

            if (speedSet != null)
            {
                speedSet.CurrentIndex = activeIndex;

                if (CurrentSpeedSet == speedSet)
                {
                    CurrentSpeedIndex = activeIndex;
                    speedSet.StartVerticalIndex = activeIndex; //Set the Start Vertical Index as the new Speed 
                }
            }
        }

        public virtual void Speed_Update_Current() => CurrentSpeedIndex = CurrentSpeedIndex;
        public virtual void Speed_SetTopIndex(int topIndex) 
        {
            CurrentSpeedSet.TopIndex = topIndex;
            Speed_Update_Current();
        }

        public virtual void Speed_SetTopIndex(string SpeedSetName, int topIndex)
        {
            var speedSet = SpeedSet_Get(SpeedSetName);
            if (speedSet != null)
            {
                speedSet.TopIndex = topIndex;
                Speed_Update_Current();
            }
        }


        /// <summary> Change the Speed of a Speed Set</summary>
        public virtual void SpeedSet_Set_Active(string SpeedSetName, string activeSpeed)
        {
            var speedSet = speedSets.Find(x => x.name.ToLower() == SpeedSetName.ToLower());

            if (speedSet != null)
            {
               var mspeedIndex = speedSet.Speeds.FindIndex(x => x.name.ToLower() == activeSpeed.ToLower());

                if (mspeedIndex != -1)
                {
                    speedSet.CurrentIndex = mspeedIndex + 1;

                    if (CurrentSpeedSet == speedSet)
                    {
                        CurrentSpeedIndex = mspeedIndex + 1;
                        speedSet.StartVerticalIndex = CurrentSpeedIndex; //Set the Start Vertical Index as the new Speed 
                    }
                }
            }
            else
            {
                Debug.LogWarning("There's no Speed Set called : " + SpeedSetName);
            }
        }

        #endregion

        #region Extrass

        /// <summary> If the Animal has touched the ground then Grounded will be set to true  </summary>
        public void CheckIfGrounded()
        {
            if (!Grounded)
            {
                AlignRayCasting();

                if (MainRay && FrontRay)
                {
                    Grounded = true;   //Activate the Grounded Parameter so the Idle and the Locomotion State can be activated
                }
            }
        }

       public void Always_Forward(bool value) => AlwaysForward = value;

        /// <summary>Activate Attack triggers </summary>
        public virtual void ActivateDamager(int ID)
        {
            if (ID == -1)                         //Enable all Attack Triggers
            {
                foreach (var dam in Attack_Triggers) dam.DoDamage(true);
            }
            else if (ID == 0)                     //Disable all Attack Triggers
            {
                foreach (var dam in Attack_Triggers) dam.DoDamage(false);
            }
            else
            {
                var Att_T = Attack_Triggers.FindAll(x => x.ID == ID);        //Enable just a trigger with an index

                if (Att_T != null)
                    foreach (var dam in Att_T) dam.DoDamage(true);
            }
        }

        /// <summary>Store all the Animal Colliders </summary>
        internal void GetAnimalColliders()
        {
           var colls = GetComponentsInChildren<Collider>(true).ToList();      //Get all the Active colliders

           colliders = new List<Collider>();

            foreach (var item in colls)
            {
                if (!item.isTrigger/* && item.gameObject.layer == gameObject.layer*/) colliders.Add(item);        //Add the Animal Colliders Only
            }
        }

        /// <summary>Enable/Disable All Colliders on the animal. Avoid the Triggers </summary>
        public virtual void EnableColliders(bool active)
        {
            foreach (var item in colliders) item.enabled = active;
        }

      

        /// <summary>Disable this Script and MalbersInput Script if it has it.</summary>
        public virtual void DisableAnimal()
        {
            enabled = false;
            MalbersInput MI = GetComponent<MalbersInput>();
            if (MI) MI.enabled = false;
        }

        public void SetTimeline(bool isonTimeline)
        {
            Sleep = isonTimeline;
            
            //Unparent the Rotator since breaks the Cinemachine Logic
            if (Rotator != null)   RootBone.parent = isonTimeline ? null : Rotator;
        }


        /// <summary>InertiaPositionSpeed = TargetSpeed</summary>
        public void ClearInertiaSpeed() => InertiaPositionSpeed = TargetSpeed;

        public void UseCameraBasedInput() => UseCameraInput = true;
        #endregion
    }
}