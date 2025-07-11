﻿using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using MalbersAnimations.Events;
using MalbersAnimations.Scriptables;
using MalbersAnimations.Controller;
using System.Collections;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>  Horse Animset Pro RIDING SYSTEM  </summary>
/// <summary>  Version 1.2.1 </summary>

namespace MalbersAnimations.HAP
{
    public enum DismountType { Random, Input, Last }
    [AddComponentMenu("Malbers/Riding/Rider")]
    public class MRider : MonoBehaviour, IAnimatorListener, IRider
    {
        #region Public Variables
        /// <summary>Parent to mount Point </summary>
        public BoolReference Parent = new BoolReference(true);

        /// <summary>This animal is the one that you can call or StartMount </summary>
        public Mount MountStored;
        /// <summary>True iff we want to start mounted an animal </summary>
        public BoolReference StartMounted;

        [Tooltip("Resync Animations with the Mount")]
        public bool ReSync = true;

        public Vector3Reference Gravity = new Vector3Reference(Vector3.down);



        [SerializeField] private BoolReference m_CanMount = new BoolReference(false);
        [SerializeField] private BoolReference m_CanDismount = new BoolReference(false);
        [SerializeField] private BoolReference m_CanCallAnimal = new BoolReference(false);

        /// <summary>Changes the Dismount animation on the Rider</summary>
        public DismountType DismountType = DismountType.Random;

        /// <summary>Mounted Layer Path</summary>
        public string LayerPath = "Layers/Mount v2";
        /// <summary>Mounted Layer Name</summary>
        public string MountLayer = "Mounted";


        //public readonly static int LocomotionHash = Animator.StringToHash("Locomotion");
        //public readonly static int LocomotionStrafeHash = Animator.StringToHash("Locomotion Strafe");
        //public readonly static int FlyHash = Animator.StringToHash("Fly");
        //public readonly static int FlyStrafeHash = Animator.StringToHash("Fly Strafe");

        public readonly static int IKLeftFootHash = Animator.StringToHash("IKLeftFoot");
        public readonly static int IKRightFootHash = Animator.StringToHash("IKRightFoot");
        public readonly static int MountHash = Animator.StringToHash("Mount");
        public readonly static int MountSideHash = Animator.StringToHash("MountSide");
        public static readonly int EmptyHash = Animator.StringToHash("Empty");



        /// <summary>Type to Update to set Everyframe the position and rotation of the rider to the Animal Mount Point</summary>
        [Utilities.Flag("Update Type")]
        public UpdateMode LinkUpdate = UpdateMode.Update | UpdateMode.FixedUpdate;



        /// <summary>Time to Align to the Mount Trigger Position while is playing the Mount Animation</summary>
        public FloatReference AlingMountTrigger = new FloatReference(0.2f);

        private Hashtable animatorParams;


        public bool debug;

        #region Call Animal
        public AudioClip CallAnimalA;
        public AudioClip StopAnimalA;
        public AudioSource RiderAudio;
        /// <summary>Call for  the animal, True: Calls The Animal, False: if the animal was calling then stop its movement</summary>
        public bool ToggleCall { get; set; }
        #endregion

        #region ExtraCollider

        public CapsuleCollider MainCollider;
        public OverrideCapsuleCollider ModifyMainCollider;
        private OverrideCapsuleCollider Def_CollPropeties;


        //public bool CreateColliderMounted;
        //public float Col_radius = 0.225f;
        //public bool Col_Trigger = true;
        //public float Col_height = 0.8f;
        //public float Col_Center = 1.2f;
        #endregion

        #region UnityEvents

        public GameObjectEvent OnFindMount = new GameObjectEvent();
        public BoolEvent OnCanMount = new BoolEvent();
        public BoolEvent OnCanDismount = new BoolEvent();
        public BoolEvent CanCallMount = new BoolEvent();

        public UnityEvent OnStartMounting = new UnityEvent();
        public UnityEvent OnEndMounting = new UnityEvent();
        public UnityEvent OnStartDismounting = new UnityEvent();
        public UnityEvent OnEndDismounting = new UnityEvent();
        public UnityEvent OnAlreadyMounted = new UnityEvent();

        #endregion

        public BoolReference DisableComponents;
        public Behaviour[] DisableList;
        #endregion

        #region Auto Properties

        /// <summary>Montura stored when the Riders enters a MountTrigger</summary>
        public Mount Montura { get; private set; }

        public virtual IInputSource MountInput { get; private set; }

        /// <summary> If Null means that we are NOT Near to an Animal</summary>
        public MountTriggers MountTrigger { get; private set; }

        public bool CanMount { get => m_CanMount.Value; private set => m_CanMount.Value = value; }
        public bool CanDismount { get => m_CanDismount.Value; private set => m_CanDismount.Value = value; }

        /// <summary>Check if we can call the Animal</summary>
        public bool CanCallAnimal { get => m_CanCallAnimal.Value; private set => m_CanCallAnimal.Value = value; }


        private bool mounted;
        public bool Mounted
        {
            get => mounted;
            private set
            {
                mounted = value;
                SetAnimParameter(MountHash, Mounted);                           //Update Mount Parameter on the Animator
            }
        }
        public bool IsOnHorse { get; private set; }

        /// <summary> Speed Multiplier for the Speeds Changes while using other Animals</summary>
        public float SpeedMultiplier { get; set; }
        public float TargetSpeedMultiplier { get; set; }

        public bool ForceLateUpdateLink { get; set; }

        /// <summary> Store all the MonoBehaviours on this GameObject</summary>
        protected MonoBehaviour[] AllComponents { get; private set; }
        #endregion

        #region IK VARIABLES    
        protected float L_IKFootWeight = 0f;        //IK Weight for Left Foot
        protected float R_IKFootWeight = 0f;        //IK Weight for Right Foot
        #endregion

        /// <summary>Target Rotation the Rider does while Mounting / Dismouting</summary>
        public Quaternion MountRotation { get; set; }

        /// <summary>Target Posttion the Rider does while Mounting / Dismouting</summary>
        public Vector3 MountPosition { get; set; }

        internal int MountLayerIndex = -1;                    //Mount Layer Index
        protected AnimatorUpdateMode Default_Anim_UpdateMode;

        #region Properties
        public bool IsRiding => IsOnHorse && Mounted;

        /// <summary>Returns true if the Rider is from the Start of the Mount to the End of the Dismount</summary>
        public bool IsMountingDismounting => IsOnHorse || Mounted;

        public bool IsMounting => !IsOnHorse && Mounted;

        /// <summary>Returns true if the Rider is between the Start and the End of the Dismount Animations</summary>
        public bool IsDismounting => IsOnHorse && !Mounted;


        #region private vars
        /// <summary>Straight Spine Weight for the Bones</summary>
        protected float SP_Weight;

        // protected float SAim_Weight;
        protected RigidbodyConstraints DefaultConstraints;
        protected CollisionDetectionMode DefaultCollision;
        #region Re-Sync with Horse
        //Used this for Sync Animators
        private float RiderNormalizedTime;
        private float HorseNormalizedTime;
        // private float LastSyncTime;
        #endregion
        #endregion

        #region References

        [SerializeField] private Animator animator;
        [SerializeField] private Rigidbody m_rigidBody;

        /// <summary>Reference for the Animator </summary>
        public Animator Anim { get => animator; private set => animator = value; }  //Reference for the Animator 
        /// <summary>Reference for the rigidbody</summary>
        public Rigidbody RB { get => m_rigidBody; private set => m_rigidBody = value; }  //Reference for the Animator 

        /// <summary>Root Gameobject for the Rider Character</summary>
        public Transform RiderRoot { get => m_root; private set => m_root = value; }

        [SerializeField] private Transform m_root;
        #region Bones
        /// <summary>Spine Bone Transform</summary>
        public Transform Spine { get; private set; }
        //  public Transform Hips { get; private set; }
        public Transform Chest { get; private set; }

        ///// <summary>Set from the Rider Combat that the Rider is Aiming</summary>
        //public bool CombatAim { get; internal set; }

        /// <summary>Ground Character Controller</summary>
        public ISleepController GroundController { get; private set; }


        #endregion

        /// <summary>Reference for all the colliders on this gameObject</summary>
        protected List<Collider> colliders;

        protected CapsuleCollider mountedCollider;  //For creating a collider when is mounted for Hit Porpouse
        #endregion
        #endregion

        /// <summary>  Store All colliders that are enabled and not Triggers </summary>
        private void GetExtraColliders()
        {
            colliders = GetComponentsInChildren<Collider>().ToList();

            var CleanCol = new List<Collider>();

            foreach (var col in colliders)
            {
                if (col.enabled && !col.isTrigger)
                    CleanCol.Add(col);
            }

            colliders = new List<Collider>(CleanCol);


            if (MainCollider == null) MainCollider = GetComponent<CapsuleCollider>();

            if (MainCollider)
            {
                Def_CollPropeties = new OverrideCapsuleCollider(MainCollider) { modify = (CapsuleModifier)(-1) };
                colliders.Remove(MainCollider); //Remove the Main Collider from the Extra Colliders
            }
        }

        void Start()
        {
            if (RiderRoot == null) RiderRoot = transform.root;
            if (Anim == null) Anim = this.FindComponent<Animator>();
            if (RB == null) RB = this.FindComponent<Rigidbody>();

            animatorParams = new Hashtable();

            if (Anim)
            {
                //Store all the Animator parameters
                foreach (AnimatorControllerParameter parameter in Anim.parameters)
                    animatorParams.Add(parameter.nameHash, parameter.name);

                MountLayerIndex = Anim.GetLayerIndex(MountLayer);

                Anim.SetLayerWeight(MountLayerIndex, 1); //Just in case

                Spine = Anim.GetBoneTransform(HumanBodyBones.Spine);                   //Get the Rider Spine transform
                //Hips = Anim.GetBoneTransform(HumanBodyBones.Hips);                   //Get the Rider Hips transform
                Chest = Anim.GetBoneTransform(HumanBodyBones.Chest);                   //Get the Rider Chest transform

                Default_Anim_UpdateMode = Anim.updateMode;                             //Gets the Update Mode of the Animator to restore later when dismounted.
            }

            GetExtraColliders();

            IsOnHorse = Mounted = false;
            ForceLateUpdateLink = false;
            SpeedMultiplier = 1f;
            TargetSpeedMultiplier = 1f;

            if ((int)LinkUpdate == 0 || !Parent)
                LinkUpdate = UpdateMode.FixedUpdate | UpdateMode.LateUpdate;

            if (StartMounted.Value) Start_Mounted();

            UpdateCanMountDismount();
        }

        void Update()
        {
            if ((LinkUpdate & UpdateMode.Update) == UpdateMode.Update) UpdateRiderTransform();
        }


        private void LateUpdate()
        {
            if ((LinkUpdate & UpdateMode.LateUpdate) == UpdateMode.LateUpdate || ForceLateUpdateLink) UpdateRiderTransform();
        }

        private void FixedUpdate()
        {
            if ((LinkUpdate & UpdateMode.FixedUpdate) == UpdateMode.FixedUpdate) UpdateRiderTransform();
        }

        /// <summary>Updates the Rider Position to the Mount Point</summary>
        public virtual void UpdateRiderTransform()
        {
            if (IsRiding)
            {
                transform.position = Montura.MountPoint.position;
                transform.rotation = Montura.MountPoint.rotation;

                //Update the Mount Position/Rotation Also
                MountRotation = transform.rotation;
                MountPosition = transform.position;
            }
        }



        /// <summary>Add the Mount Rotation and Position Modifications while Mounting Dismounting called y the animator</summary>
        public virtual void Mount_TargetTransform()
        {
            transform.position = MountPosition;
            transform.rotation = MountRotation;
        }


        /// <summary>Set the Mount Index Value</summary>
        internal void SetMountSide(int side) => SetAnimParameter(MountSideHash, side);

        public virtual void MountAnimal()
        {
            if (!CanMount) return;

            if (debug) Debug.Log($"<b>{name}:<color=cyan> [Mount Animal] </color> </b>");  //Debug


            if (!Montura.InstantMount)                                           //If is instant Mount play it      
            {
                Mounted = true;                                                  //Update MountSide Parameter In the Animator
                SetMountSide(MountTrigger.MountID);                              //Update MountSide Parameter In the Animator
                                                                                 // Anim?.Play(MountTrigger.MountAnimation, MountLayerIndex);      //Play the Mounting Animations
            }
            else
            {
                Anim?.Play(Montura.MountIdle, MountLayerIndex);                //Ingore the Mounting Animations
                Anim?.Update(0);                             //Update the Animator ????

                Start_Mounting();
                End_Mounting();
            }
        }

        public virtual void DismountAnimal()
        {
            if (!CanDismount) return;

            if (debug) Debug.Log($"<b>{name}:<color=cyan> [Dismount Animal] </color> </b>");  //Debug


            Montura.Mounted = Mounted = false;                                  //Unmount the Animal

            MountTrigger = GetDismountTrigger();

            foreach (var mt in Montura.MountTriggers)
                if (mt.AutoMount) mt.WasAutomounted = true;                 //Set to all the Auto Mounted Triggers that it dismounting

            SetMountSide(MountTrigger.MountID);                               //Update MountSide Parameter In the Animator

            if (Montura.InstantMount)                                           //Use for Instant mount
            {
                Anim.Play(Hash.Empty, MountLayerIndex);
                SetMountSide(0);                                                //Update MountSide Parameter In the Animator

                Start_Dismounting();
                End_Dismounting();

                RiderRoot.position = MountTrigger.transform.position + (MountTrigger.transform.forward * -0.2f);   //Move the rider directly to the mounttrigger
            }
        }


        /// <summary>Return the Correct Mount Trigger using the DismountType</summary>
        private MountTriggers GetDismountTrigger()
        {
            switch (DismountType)
            {
                case DismountType.Last:
                    if (MountTrigger == null) MountTrigger = Montura.MountTriggers[Random.Range(0, Montura.MountTriggers.Count)];
                    return MountTrigger;
                case DismountType.Input:
                    var MoveInput = Montura.Animal.MovementAxis;

                    MountTriggers close = MountTrigger;

                    float Diference = Vector3.Angle(MountTrigger.Direction, MoveInput);

                    foreach (var mt in Montura.MountTriggers)
                    {
                        var newDiff = Vector3.Angle(mt.Direction, MoveInput);

                        if (newDiff < Diference)
                        {
                            Diference = newDiff;
                            close = mt;
                        }
                    }

                    return close;

                case DismountType.Random:
                    int Randomindex = Random.Range(0, Montura.MountTriggers.Count);
                    return Montura.MountTriggers[Randomindex];
                default:
                    return MountTrigger;
            }
        }

        /// <summary>Set all the correct atributes and variables to Start Mounted on the next frame</summary>
        public void Start_Mounted()
        {
            if (MountStored && MountStored.gameObject.activeSelf)
            {
                Montura = MountStored;

                if (debug) Debug.Log($"<b>{name}:<color=green> [Start MOUNTED] </color> </b>");  //Debug

                StopMountAI();

                Montura.Rider = this;

                if (MountTrigger == null)
                    Montura.transform.GetComponentInChildren<MountTriggers>(); //Save the first Mount trigger you found


                if (Montura.DisableMountTriggers)
                    Montura.EnableMountTriggers(false);                 //Disable all Mount Trigger to avoid ON Enter ON Exit Trigger Events**

                Start_Mounting();
                End_Mounting();

                Anim?.Play(Montura.MountIdle, MountLayerIndex);               //Play Mount Idle Animation Directly

                Montura.Mounted = Mounted = true;                                     //Send to the animalMount that mounted is active

                OnAlreadyMounted.Invoke();

                UpdateRiderTransform();
            }
        }



        /// <summary>Force the Rider to Dismount</summary>
        public virtual void ForceDismount()
        {
            if (debug) Debug.Log($"<b>{name}:<color=green> [Force Dismount] </color> </b>");  //Debug


            DisconnectWithMount();
            Anim?.Play(EmptyHash, MountLayerIndex);
            SetMountSide(0);                                //Update MountSide Parameter In the Animator
            Start_Dismounting();
            End_Dismounting();
        }

        /// <summary>CallBack at the Start of the Mount Animations</summary>
        internal virtual void Start_Mounting()
        {
            Montura.Rider = this;                                   //Send to the Montura that it has a rider
            Montura.Mounted = Mounted = true;                       //Sync Mounted Values in Animal and Rider
            MountInput = Montura.GetComponent<IInputSource>();      //Get the Input of the controller

            if (Montura.DisableMountTriggers)
                Montura.EnableMountTriggers(false);                 //Disable all Mount Trigger to avoid ON Enter ON Exit Trigger Events**

            GroundController = GetComponent<ISleepController>();
            if (GroundController != null) GroundController.Sleep = true;


            StopMountAI();

            if (RB)                                                 //Deactivate stuffs for the Rider's Rigid Body
            {
                RB.useGravity = false;
                DefaultConstraints = RB.constraints;                //Store the Contraints before mounting
                DefaultCollision = RB.collisionDetectionMode;       //Store the Contraints before mounting
                RB.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                RB.constraints = RigidbodyConstraints.FreezeAll;
                RB.isKinematic = true;
            }

            ToogleColliders(false);                         //Deactivate All Colliders on the Rider IMPORTANT ... or the Rider will try to push the animal

            ToggleCall = false;                              //Set the Call to Stop Animal
            CallAnimal(false);                              //If is there an animal following us stop him

            MountStored = Montura;                          //Store the last animal you mounted

            if (Parent) RiderRoot.parent = Montura.MountPoint;

            if (!MountTrigger) MountTrigger = Montura.GetComponentInChildren<MountTriggers>();         //If null add the first mount trigger found

            if (DisableComponents)
            {
                ToggleComponents(false);                                                //Disable all Monobehaviours breaking the Riding System
            }

            OnStartMounting.Invoke();                                                   //Invoke UnityEvent for  Start Mounting

            if (!Anim) End_Mounting();                                                  //If is there no Animator  execute the End_Dismounting part

            UpdateCanMountDismount();

            if (debug) Debug.Log($"<b>{name}:<color=green> [Start Mounting] </color> </b>");  //Debug
        }

        /// <summary>CallBack at the End of the Mount Animations </summary>
        internal virtual void End_Mounting()
        {
            Montura.Mounted = Mounted = IsOnHorse = true;                              //Sync Mounted Values in Animal and Rider again Double Check

            if (Parent)
            {
                RiderRoot.localPosition = Vector3.zero;                                    //Reset Position when PARENTED
                RiderRoot.localRotation = Quaternion.identity;                             //Reset Rotation when PARENTED
            }

            MountInput?.Enable(true);

            Montura.EnableInput(true);                                              //Enable Animal Controls

            Montura.SetAnimatorSpeed();



            if (Anim)
            {
                Anim.updateMode = Montura.Anim.updateMode;                              //Use the Same UpdateMode from the Animal

                SetAnimParameter(Montura.Animal.hash_Grounded, Montura.Animal.Grounded);
                SetAnimParameter(Montura.Animal.hash_State, Montura.Animal.ActiveStateID.ID);
                SetAnimParameter(Montura.Animal.hash_Mode, Montura.Animal.ModeAbility);
                SetAnimParameter(Montura.Animal.hash_Stance, Montura.ID);

                ConnectWithMount();
            }
            OnEndMounting.Invoke();

            UpdateCanMountDismount();

            if (debug) Debug.Log($"<b>{name}:<color=green> [End Mounting] </color> </b>");  //Debug
        }

        /// <summary> CallBack at the Start of the Dismount Animations</summary>
        internal virtual void Start_Dismounting()
        {
            RiderRoot.parent = null;

            Montura.Mounted = Mounted = false;                                      //Disable Mounted on everyone
            Montura.EnableInput(false);                                              //Disable Montura Controls

            OnStartDismounting.Invoke();

            if (Anim)
            {
                Anim.updateMode = Default_Anim_UpdateMode;                               //Restore Update mode to its original
                DisconnectWithMount();
            }
            else
            {
                End_Dismounting();
            }

            UpdateCanMountDismount();

            if (debug) Debug.Log($"<b>{name}:<color=green> [Start Dismounting] </color> </b>");  //Debug
        }

        /// <summary>CallBack at the End of the Dismount Animations</summary>
        internal virtual void End_Dismounting()
        {
            IsOnHorse = false;                                                              //Is no longer on the Animal

            if (Montura)
            {
                Montura.EnableInput(false);                                                 //Disable Montura Controls
                Montura.Rider = null;
                Montura.EnableMountTriggers(true);                                         //Enable all Mount Triggers
                Montura = null;                                                             //Reset the Montura
            }

            MountTrigger = null;

            ToggleCall = false;                                                             //Reset the Call Animal

            if (RB)                                                                          //Reactivate stuffs for the Rider's Rigid Body
            {
                RB.isKinematic = false;
                RB.useGravity = true;
                RB.constraints = DefaultConstraints;
                RB.collisionDetectionMode = DefaultCollision;
            }

            if (Anim)
            {
                Anim.speed = 1;                                                             //Reset AnimatorSpeed

                MTools.ResetFloatParameters(Anim);
            }

            RiderRoot.rotation = Quaternion.FromToRotation(RiderRoot.up, Gravity) * RiderRoot.rotation;    //Reset the Up Vector; ****IMPORTANT when  CHANGE THE GRAVIY

            ToogleColliders(true);                                                          //Enabled colliders

            if (DisableComponents) ToggleComponents(true);                                  //Enable all Monobehaviours breaking the Mount System


            OnEndDismounting.Invoke();                                                      //Invoke UnityEvent when is off Animal

            UpdateCanMountDismount();

            if (GroundController != null)
            {
                GroundController.Sleep = false;
                GroundController = null; // Ground controller will be enable back again
            }

            if (debug) Debug.Log($"<b>{name}:<color=green> [End Dismounting] </color> </b>");  //Debug
        }

        private void ConnectWithMount()
        {
            Montura.Animal.SetBoolParameter += SetAnimParameter;
            Montura.Animal.SetIntParameter += SetAnimParameter;
            Montura.Animal.SetFloatParameter += SetAnimParameter;
            if (ReSync) Montura.Animal.StateCycle += Animators_Locomotion_ReSync;
        }

        /// <summary>Disconnect the Animal Events from the Riders Methods (Grounded, State, Mode)</summary>
        private void DisconnectWithMount()
        {
            Montura.Animal.SetBoolParameter -= SetAnimParameter;
            Montura.Animal.SetIntParameter -= SetAnimParameter;
            Montura.Animal.SetFloatParameter -= SetAnimParameter;
            if (ReSync) Montura.Animal.StateCycle -= Animators_Locomotion_ReSync;
        }

        internal virtual void MountTriggerEnter(Mount mount, MountTriggers mountTrigger)
        {
            Montura = mount;                                           //Set to Mount on this Rider    
            MountTrigger = mountTrigger;                               //Send the side transform to mount
            OnFindMount.Invoke(mount.transform.root.gameObject);       //Invoke Found Animal

            if (!mountTrigger.AutoMount)
                Montura.OnCanBeMounted.Invoke(Montura.CanBeMountedByState); //Invoke Can Be mounted to true ???

            Montura.NearbyRider = true;

            UpdateCanMountDismount();
        }

        internal virtual void MountTriggerExit()
        {
            MountTrigger = null;

            if (Montura)
            {
                Montura.EnableInput(false);
                Montura.OnCanBeMounted.Invoke(false);
                Montura.NearbyRider = false;
            }

            Montura = null;
            MountInput = null;
            OnFindMount.Invoke(null);

            UpdateCanMountDismount();
        }

        /// <summary> Update the values Can Mount Can Dismount </summary>
        internal virtual void UpdateCanMountDismount()
        {
            bool canMount = Montura && !Mounted && !IsOnHorse && Montura.CanBeMountedByState;
            //  if (CanMount != canMount)
            {
                CanMount = canMount;
                OnCanMount.Invoke(CanMount);
            }


            bool canDismount = IsRiding && Montura.CanBeDismountedByState;
            // if (CanDismount != canDismount)
            {
                CanDismount = canDismount;
                OnCanDismount.Invoke(CanDismount);
            }


            bool canCallAnimal = !Montura && !Mounted && !IsOnHorse;
            //   if (CanCallAnimal != canCallAnimal)
            {
                CanCallAnimal = canCallAnimal;
                CanCallMount.Invoke(CanCallAnimal);
            }
        }

        /// <summary> Syncronize the Animal/Rider animations if Rider loose sync with the animal on the locomotion state </summary>
        protected virtual void Animators_Locomotion_ReSync(int CurrentState)
        {
            if (!Anim) return;
            if (Montura.Animal.Stance != 0) return;     //Skip if the we are not on the default stance                                                       
            if (Montura.ID != 0) return;                // if is not the Horse (Wagon do not sync )                                                        
            if (Anim.IsInTransition(MountLayerIndex) || Montura.Animal.Anim.IsInTransition(0)) return; //Do not Resync when is  

            if (MTools.CompareOR(CurrentState, StateEnum.Locomotion, StateEnum.Fly))             //Search for syncron the locomotion state on the animal. Sync every 1 sec
            {

                // var StatePath = CurrentState == 1 ? LocomotionHash : FlyHash;
                //if (Montura.Animal.Strafe)
                //{
                //    StatePath = CurrentState == 1 ? LocomotionStrafeHash : FlyHash;
                //}

                var HorseStateInfo = Montura.Animal.Anim.GetCurrentAnimatorStateInfo(0);
                var RiderStateInfo = Anim.GetCurrentAnimatorStateInfo(MountLayerIndex);

                HorseNormalizedTime = HorseStateInfo.normalizedTime;            //Get the normalized time from the Rider
                RiderNormalizedTime = RiderStateInfo.normalizedTime;            //Get the normalized time from the Horse

                if (Mathf.Abs(HorseNormalizedTime - RiderNormalizedTime) >= 0.2f)   //Checking if the animal and the rider are unsync by 0.2
                {
                    //if (Anim.HasState(MountLayerIndex, MasterStateInfo.fullPathHash))
                    Anim.CrossFade(RiderStateInfo.fullPathHash, 0.2f, MountLayerIndex, HorseNormalizedTime);                 //Normalized with blend
                                                                                                                             //  Anim.CrossFade(StatePath, 0.02f, MountLayerIndex, HorseNormalizedTime);                 //Normalized with blend
                                                                                                                             // Debug.Log($"Resync   [Mount:{MainTime:F3}  Rider:{SlaveTime}]");
                }
            }
            else
            {
                RiderNormalizedTime = HorseNormalizedTime = 0;
            }
        }



        /// <summary> If the Animal has a IMountAI component it can be called</summary>
        public virtual void CallAnimal(bool call)
        {
            if (CanCallAnimal && MountStored && MountStored.AI != null)
            {
                ToggleCall = call;

                if (ToggleCall)
                {
                    MountStored.AI.SetTarget(transform); //Set the Rider as the Target to follow
                    MountStored.AI.SetMoveAgentOnMovingTarget(true); //Set the Rider as the Target to follow
                    if (CallAnimalA)
                        RiderAudio.PlayOneShot(CallAnimalA);
                }
                else
                {
                    StopMountAI();
                    if (StopAnimalA)
                        RiderAudio.PlayOneShot(StopAnimalA);
                }
            }
        }


        public virtual void StopMountAI()
        {
            MountStored?.AI?.Stop();
            MountStored?.AI?.SetMoveAgentOnMovingTarget(false);
            //Montura.AI?.SetTarget(null);
        }


        public virtual void CallAnimalToggle()
        {
            ToggleCall ^= true;
            CallAnimal(ToggleCall);
        }


        /// <summary>Enable/Disable The  Colliders in this gameobject </summary>
        protected virtual void ToogleColliders(bool active)
        {
            MountingCollider(!active);                    //Reestore Collider

            foreach (var col in colliders) col.enabled = active;

        }

        /// <summary> Create a collider from hip to chest to check hits  when is on the horse  </summary>
        private void MountingCollider(bool Mounting)
        {
            if (Mounting)
                ModifyMainCollider.Modify(MainCollider); //Modify
            else
                Def_CollPropeties.Modify(MainCollider); //Restore
        }


        /// <summary>Toogle the MonoBehaviour Components Attached to this game Objects but the Riders Scripts </summary>
        protected virtual void ToggleComponents(bool enabled)
        {
            if (DisableList.Length == 0)
            {
                foreach (var component in AllComponents)
                {
                    if (component is MRider || component is MWeaponManager) //Do not Disable or enable Rider or RiderCombat
                        continue;

                    component.enabled = enabled;
                }
            }
            else
            {
                foreach (var component in DisableList)
                {
                    if (component != null) component.enabled = enabled;
                }
            }
        }

        #region Set Animator Parameters
        /// <summary>Set a Int on the Animator</summary>
        public void SetAnimParameter(int hash, int value) { if (Anim && HasParam(hash)) Anim.SetInteger(hash, value); }

        /// <summary>Set a float on the Animator</summary>
        public void SetAnimParameter(int hash, float value) { if (Anim && HasParam(hash)) Anim.SetFloat(hash, value); }

        /// <summary>Set a Bool on the Animator</summary>
        public void SetAnimParameter(int hash, bool value) { if (Anim && HasParam(hash)) Anim.SetBool(hash, value); }
        #endregion

        private bool HasParam(int hash) => animatorParams.ContainsKey(hash);

        #region Link Animator
        protected virtual void SyncAnimator()
        {
            MAnimal animal = Montura.Animal;

            SetAnimParameter(animal.hash_Vertical, animal.VerticalSmooth);
            SetAnimParameter(animal.hash_Horizontal, animal.HorizontalSmooth);
            SetAnimParameter(animal.hash_Slope, animal.SlopeNormalized);
            SetAnimParameter(animal.hash_Grounded, animal.Grounded);

            SetAnimParameter(animal.hash_ModeStatus, animal.ModeInt);
            SetAnimParameter(animal.hash_StateFloat, animal.State_float);

            if (!Montura.UseSpeedModifiers) SpeedMultiplier = animal.SpeedMultiplier; //In case the Mount is not using Speed Modifiers

            SpeedMultiplier = Mathf.MoveTowards(SpeedMultiplier, TargetSpeedMultiplier, Time.deltaTime * 5f);
            SetAnimParameter(animal.hash_SpeedMultiplier, SpeedMultiplier);
        }

        void AnimalGrounded(bool grounded)
        {
            Anim.SetBool(Montura.Animal.hash_Grounded, grounded);
        }

        void AnimalState(int State)
        {
            Anim.SetInteger(Montura.Animal.hash_State, State);
        }

        void AnimalStance(int stance)
        {
            if (Montura.ID != 0) return;  //Skip if the Mount ID is Not  the Default
            Anim.SetInteger(Montura.Animal.hash_Stance, stance);
        }


        void AnimalMode(int mode)
        {
            Anim.SetInteger(Montura.Animal.hash_Mode, mode);
        }
        #endregion 



        /// <summary> Checks and Execute  without Input if the Rider can Mount, Dismount or Call an Animal </summary>
        public virtual void CheckMountDismount()
        {
            UpdateCanMountDismount();

            if (CanMount) MountAnimal();              //if are near an animal and we are not already on an animal//Run mounting Animations
            else if (CanDismount) DismountAnimal();           //if we are already mounted and the animal is not moving (Mounted && IsOnHorse && Montura.CanDismount)//Run Dismounting Animations
            else if (CanCallAnimal) CallAnimalToggle();         //if there is no horse near, call the animal stored
        }

        /// <summary>IK Feet Adjustment while mounting</summary>
        void OnAnimatorIK()
        {
            if (Anim == null) return;           //If there's no animator skip

            if (IsMountingDismounting)
                IKFeet();
            SolveStraightMount();
        }


        private void SolveStraightMount()
        {
            if (IsRiding /*&& !CombatAim*/)
            {
                if (Montura.StraightSpine)
                {
                    SP_Weight = Mathf.MoveTowards(SP_Weight, Montura.StraightSpine ? 1 : 0, Montura.Animal.DeltaTime * Montura.smoothSM / 2);
                }
                else
                {
                    SP_Weight = Mathf.MoveTowards(SP_Weight, 0, Montura.Animal.DeltaTime * Montura.smoothSM / 2);
                }

                if (SP_Weight != 0)
                {
                    Anim.SetLookAtPosition(Montura.MonturaSpineOffset);
                    Anim.SetLookAtWeight(SP_Weight, 0.6f, 1);
                }
            }
        }

        private void IKFeet()
        {
            if (Montura.FootLeftIK == null || Montura.FootRightIK == null
                || Montura.KneeLeftIK == null || Montura.KneeRightIK == null) return;  //if is there missing an IK point do nothing

            //linking the weights to the animator
            if (Mounted || IsOnHorse)
            {
                L_IKFootWeight = 1f;
                R_IKFootWeight = 1f;

                if (IsMounting || IsDismounting)
                {
                    L_IKFootWeight = Anim.GetFloat(IKLeftFootHash);
                    R_IKFootWeight = Anim.GetFloat(IKRightFootHash);
                }

                //setting the weight
                Anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, L_IKFootWeight);
                Anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, R_IKFootWeight);

                Anim.SetIKHintPositionWeight(AvatarIKHint.LeftKnee, L_IKFootWeight);
                Anim.SetIKHintPositionWeight(AvatarIKHint.RightKnee, R_IKFootWeight);

                //Knees
                Anim.SetIKRotationWeight(AvatarIKGoal.LeftFoot, L_IKFootWeight);
                Anim.SetIKRotationWeight(AvatarIKGoal.RightFoot, R_IKFootWeight);

                //Set the IK Positions
                Anim.SetIKPosition(AvatarIKGoal.LeftFoot, Montura.FootLeftIK.position);
                Anim.SetIKPosition(AvatarIKGoal.RightFoot, Montura.FootRightIK.position);

                //Knees
                Anim.SetIKHintPosition(AvatarIKHint.LeftKnee, Montura.KneeLeftIK.position);    //Position
                Anim.SetIKHintPosition(AvatarIKHint.RightKnee, Montura.KneeRightIK.position);  //Position

                Anim.SetIKHintPositionWeight(AvatarIKHint.LeftKnee, L_IKFootWeight);   //Weight
                Anim.SetIKHintPositionWeight(AvatarIKHint.RightKnee, R_IKFootWeight);  //Weight

                //setting the IK Rotations of the Feet
                Anim.SetIKRotation(AvatarIKGoal.LeftFoot, Montura.FootLeftIK.rotation);
                Anim.SetIKRotation(AvatarIKGoal.RightFoot, Montura.FootRightIK.rotation);
            }
            else
            {
                Anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0f);
                Anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0f);

                Anim.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0f);
                Anim.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0f);
            }
        }

        /// <summary>Used for listening Message behaviour from the Animator</summary>
        public virtual bool OnAnimatorBehaviourMessage(string message, object value) => this.InvokeWithParams(message, value);

        //void SolveStraightAIM()
        //{
        //    if (IsRiding  && CombatAim)
        //    {
        //        SAim_Weight = Mathf.MoveTowards(SAim_Weight, 1, Montura.Animal.DeltaTime * Montura.smoothSM / 2);
        //    }
        //    else 
        //    {
        //        SAim_Weight = Mathf.MoveTowards(SAim_Weight, 0, Montura.Animal.DeltaTime * Montura.smoothSM / 2);
        //    }

        //    if (SAim_Weight != 0)
        //    {
        //        if (Montura.UseStraightAim)
        //        {
        //            var MP = Montura.MountPoint;
        //            var TargetRotation = Quaternion.FromToRotation(MP.up, Montura.Animal.UpVector) * Montura.Animal.transform.rotation;
        //            var DeltaRotation = Quaternion.Lerp(Anim.bodyRotation, TargetRotation * Quaternion.Euler(Montura.AimOffset), SAim_Weight);
        //            var toWorld = Quaternion.Inverse(DeltaRotation) * Spine.rotation;
        //            //  Anim.bodyRotation = Quaternion.Lerp(Anim.bodyRotation, TargetRotation * Quaternion.Euler(Montura.AimOffset), SAim_Weight);
        //            Anim.SetBoneLocalRotation(HumanBodyBones.Spine, toWorld);
        //        }
        //    }
        //}

        //private void SolveStraightMount()
        //{  
        //    if (IsRiding && Montura.StraightSpine)
        //    {
        //        var MP = Montura.MountPoint;

        //        SP_Weight = Mathf.MoveTowards(SP_Weight, 1, Montura.Animal.DeltaTime * Montura.smoothSM / 2);

        //        var TargetRotation = Quaternion.FromToRotation(MP.up, Montura.Animal.UpVector) * Montura.Animal.transform.rotation;     //Calculate the orientation to the Up Vector  

        //        TargetRotation  = Quaternion.Inverse(Spine.rotation) * TargetRotation;

        //        TargetRotation *= Montura.PointOffset;

        //        straightRotation = Quaternion.Lerp(straightRotation, TargetRotation, SP_Weight);
        //    }
        //    else if (IsDismounting || !Montura.StraightSpine)
        //    {
        //        SP_Weight = Mathf.MoveTowards(SP_Weight, 0, Montura.Animal.DeltaTime * Montura.smoothSM / 2);

        //        straightRotation = Quaternion.Lerp(Spine.localRotation, straightRotation, SP_Weight);
        //    }

        //    if (SP_Weight != 0)
        //    {
        //        Anim.SetBoneLocalRotation(HumanBodyBones.Spine, straightRotation);
        //    }
        //}


        #region PUBLIC METHODS / CALLBACKS

        /// <summary> Enable Disable the Input for the Mount</summary>
        public virtual void EnableMountInput(bool value) => Montura?.EnableInput(value);

        /// <summary> Enable Disable an Input for the Mount</summary>
        public void DisableMountInput(string input) => Montura?.MountInput?.DisableInput(input);

        /// <summary> Enable Disable an Input for the Mount</summary>
        public void EnableMountInput(string input) => Montura?.MountInput?.EnableInput(input);


        #region IKREINS
        /// <summary>This will be call by the Animator Layer "Rider Arm Right"  to free the Reins from the Right Hand </summary>
        public virtual void FreeRightHand(bool value)
        {
            Montura?.GetComponent<IKReins>()?.FreeRightHand(value);       //Send to the Reins Script that the  RIGHT Hand is free or not
        }


        /// <summary>This will be call by the Animator Layer "Rider Arm Right"  to free the Reins from the Right Hand </summary>
        public virtual void FreeBothtHands()
        {
            Montura?.GetComponent<IKReins>()?.FreeBothHands();   //Tell the Mountura that The Rider does not have any weaon on the hands
        }

        public virtual void WeaponInHands()
        {
            Montura?.GetComponent<IKReins>()?.WeaponInHands();   //Tell the Mountura that The Rider has a weapon in  the hands
        }

        /// <summary>This will be call by the Animator Layer "Rider Arm Left"  to free the Reins from the Left Hand </summary>
        public virtual void FreeLeftHand(bool value)
        {
            Montura?.GetComponent<IKReins>()?.FreeLeftHand(value);
        }
        #endregion


        #endregion








#if UNITY_EDITOR

        private void Reset()
        {
            animator = this.FindComponent<Animator>();
            RB = this.FindComponent<Rigidbody>();
            RiderRoot = transform; //IMPORTANT

            MainCollider = GetComponent<CapsuleCollider>();

            if (MainCollider)
            {
                ModifyMainCollider = new OverrideCapsuleCollider(MainCollider)
                {
                    modify = CapsuleModifier.center | CapsuleModifier.enabled | CapsuleModifier.height | CapsuleModifier.radius | CapsuleModifier.isTrigger,
                    radius = 0.225f,
                    enabled = true,
                    isTrigger = true,
                    height = 0.8f,
                    center = new Vector3(0, 1.2f)

                };

                Def_CollPropeties = new OverrideCapsuleCollider(MainCollider) { modify = (CapsuleModifier)(-1) };
            }

            BoolVar CanMountV = MTools.GetInstance<BoolVar>("Can Mount");
            BoolVar CanDismountV = MTools.GetInstance<BoolVar>("Can Dismount");
            BoolVar CanCallMountV = MTools.GetInstance<BoolVar>("Can Call Mount");


            MEvent CanMountE = MTools.GetInstance<MEvent>("Rider Can Mount");
            MEvent CanDismountE = MTools.GetInstance<MEvent>("Rider Can Dismount");
            MEvent RiderMountUI = MTools.GetInstance<MEvent>("Rider Mount UI");

            MEvent CanCallMountE = MTools.GetInstance<MEvent>("Rider Can Call Mount");

            MEvent RiderisRiding = MTools.GetInstance<MEvent>("Rider is Riding");
            MEvent SetCameraSettings = MTools.GetInstance<MEvent>("Set Camera Settings");
            BoolVar RCWeaponInput = MTools.GetInstance<BoolVar>("RC Weapon Input");

            m_CanCallAnimal.Variable = CanCallMountV;
            m_CanCallAnimal.UseConstant = false;

            m_CanMount.Variable = CanMountV;
            m_CanMount.UseConstant = false;

            m_CanDismount.Variable = CanDismountV;
            m_CanDismount.UseConstant = false;



            OnCanMount = new BoolEvent();
            OnCanDismount = new BoolEvent();
            CanCallMount = new BoolEvent();
            OnStartMounting = new UnityEvent();
            OnEndMounting = new UnityEvent();
            OnStartMounting = new UnityEvent();
            OnStartDismounting = new UnityEvent();


            // if (CanMountV != null) UnityEditor.Events.UnityEventTools.AddPersistentListener(OnCanMount, CanMountV.SetValue);
            if (CanMountE != null) UnityEditor.Events.UnityEventTools.AddPersistentListener(OnCanMount, CanMountE.Invoke);

            // if (CanDismountV != null) UnityEditor.Events.UnityEventTools.AddPersistentListener(OnCanDismount, CanDismountV.SetValue);
            if (CanDismountE != null) UnityEditor.Events.UnityEventTools.AddPersistentListener(OnCanDismount, CanDismountE.Invoke);

            //  if (CanCallMountV != null) UnityEditor.Events.UnityEventTools.AddPersistentListener(CanCallMount, CanCallMountV.SetValue);
            if (CanCallMountE != null) UnityEditor.Events.UnityEventTools.AddPersistentListener(CanCallMount, CanCallMountE.Invoke);

            if (RiderMountUI != null) UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(OnStartMounting, RiderMountUI.Invoke, false);

            if (RiderisRiding != null)
            {
                UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(OnEndMounting, RiderisRiding.Invoke, true);
                UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(OnStartDismounting, RiderisRiding.Invoke, false);
            }

            if (SetCameraSettings != null) UnityEditor.Events.UnityEventTools.AddObjectPersistentListener<Transform>(OnStartDismounting, SetCameraSettings.Invoke, transform);

            if (RCWeaponInput != null)
            {
                UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(OnStartDismounting, RCWeaponInput.SetValue, false);
                UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(OnEndMounting, RCWeaponInput.SetValue, true);
            }


            var malbersinput = GetComponent<MalbersInput>();

            if (malbersinput)
            {
                UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(OnStartMounting, malbersinput.SetMoveCharacter, false);
                UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(OnEndDismounting, malbersinput.SetMoveCharacter, true);
            }
        }

        ///Editor Variables
        [HideInInspector] public int Editor_Tabs1;

        [ContextMenu("Create Mount Inputs")]
        void ConnectToInput()
        {
            MInput input = GetComponent<MInput>();

            if (input == null)
            { input = gameObject.AddComponent<MInput>(); }


            #region Mount Input
            var mountInput = input.FindInput("Mount");

            if (mountInput == null)
            {
                mountInput = new InputRow("Mount", "Mount", KeyCode.F, InputButton.Down, InputType.Key);
                input.inputs.Add(mountInput);

                mountInput.active.Variable = MTools.GetInstance<BoolVar>("Can Mount");
                mountInput.active.UseConstant = false;

                UnityEditor.Events.UnityEventTools.AddPersistentListener(mountInput.OnInputDown, MountAnimal);


                Debug.Log("<B>Mount</B> Input created and connected to Rider.MountAnimal");
            }
            #endregion

            #region Dismount Input


            var DismountInput = input.FindInput("Dismount");

            if (DismountInput == null)
            {
                DismountInput = new InputRow("Dismount", "Dismount", KeyCode.F, InputButton.LongPress, InputType.Key);

                DismountInput.LongPressTime = 0.2f;

                input.inputs.Add(DismountInput);

                DismountInput.active.Variable = MTools.GetInstance<BoolVar>("Can Dismount");
                DismountInput.active.UseConstant = false;

                var RiderDismountUI = MTools.GetInstance<MEvent>("Rider Dismount UI");

                UnityEditor.Events.UnityEventTools.AddPersistentListener(DismountInput.OnLongPress, DismountAnimal);

                if (RiderDismountUI != null)
                {
                    UnityEditor.Events.UnityEventTools.AddPersistentListener(DismountInput.OnLongPress, RiderDismountUI.Invoke);
                    UnityEditor.Events.UnityEventTools.AddPersistentListener(DismountInput.OnPressedNormalized, RiderDismountUI.Invoke);
                    UnityEditor.Events.UnityEventTools.AddPersistentListener(DismountInput.OnInputUp, RiderDismountUI.Invoke);
                    UnityEditor.Events.UnityEventTools.AddIntPersistentListener(DismountInput.OnInputDown, RiderDismountUI.Invoke, 0);
                }


                Debug.Log("<B>Dismount</B> Input created and connected to Rider.DismountAnimal");
            }

            #endregion

            #region CanCallMount Input


            var CanCallMount = input.FindInput("Call Mount");

            if (CanCallMount == null)
            {
                CanCallMount = new InputRow("Call Mount", "Call Mount", KeyCode.F, InputButton.Down, InputType.Key);
                input.inputs.Add(CanCallMount);

                CanCallMount.active.Variable = MTools.GetInstance<BoolVar>("Can Call Mount");
                CanCallMount.active.UseConstant = false;

                UnityEditor.Events.UnityEventTools.AddPersistentListener(CanCallMount.OnInputDown, CallAnimalToggle);


                Debug.Log("<B>Call Mount</B> Input created and connected to Rider.CallAnimalToggle");
            }

            #endregion

            UnityEditor.EditorUtility.SetDirty(this);
        }

        [ContextMenu("Create Event Listeners")]
        void CreateEventListeners()
        {
            MEvent RiderSetMount = MTools.GetInstance<MEvent>("Rider Set Mount");
            MEvent RiderSetDismount = MTools.GetInstance<MEvent>("Rider Set Dismount");

            MEventListener listener = GetComponent<MEventListener>();

            if (listener == null)
            {
                listener = gameObject.AddComponent<MEventListener>();
            }

            if (listener.Events == null) listener.Events = new List<MEventItemListener>();

            if (listener.Events.Find(item => item.Event == RiderSetMount) == null)
            {
                var item = new MEventItemListener()
                {
                    Event = RiderSetMount,
                    useVoid = true,
                };

                UnityEditor.Events.UnityEventTools.AddPersistentListener(item.Response, MountAnimal);
                listener.Events.Add(item);

                Debug.Log("<B>Rider Set Mount</B> Added to the Event Listeners");
            }

            if (listener.Events.Find(item => item.Event == RiderSetDismount) == null)
            {
                var item = new MEventItemListener()
                {
                    Event = RiderSetDismount,
                    useVoid = true,
                };

                UnityEditor.Events.UnityEventTools.AddPersistentListener(item.Response, DismountAnimal);
                listener.Events.Add(item);

                Debug.Log("<B>Rider Set Dismount</B> Added to the Event Listeners");
            }

        }

        void OnDrawGizmos()
        {
            if (Anim && Mounted && Montura.debug && Montura.Animal.ActiveStateID == StateEnum.Locomotion)
            {
                Transform head = Anim.GetBoneTransform(HumanBodyBones.Head);

                Gizmos.color = (int)RiderNormalizedTime % 2 == 0 ? Color.red : Color.white;

                Gizmos.DrawSphere((head.position - transform.root.right * 0.2f), 0.05f);

                Gizmos.color = (int)HorseNormalizedTime % 2 == 0 ? new Color(0.11f, 1f, 0.25f) : Color.white;
                Gizmos.DrawSphere((head.position + transform.root.right * 0.2f), 0.05f);

                UnityEditor.Handles.color = Color.white;
                UnityEditor.Handles.Label(head.position + transform.up * 0.5f, "Sync Status");

            }
        }
#endif
    }

    #region INSPECTOR
#if UNITY_EDITOR
    [CustomEditor(typeof(MRider), true)]
    public class MRiderEd : Editor
    {
        protected MRider M;
        private MonoScript script;
        private SerializedProperty MountStored, StartMounted, Parent, animator, m_rigidBody, m_root, gravity, ReSync,
            MountLayer, LayerPath, OnCanMount, OnCanDismount, OnStartMounting, OnEndMounting, m_CanMount, m_CanDismount, m_CanCallAnimal,
            OnStartDismounting, OnEndDismounting, OnFindMount, CanCallMount, OnAlreadyMounted, DisableList, MainCollider, ModifyMainCollider,
        /*  CreateColliderMounted,   Col_Center, Col_height, Col_radius, Col_Trigger,*/ CallAnimalA, StopAnimalA, RiderAudio,
         LinkUpdate, debug, AlingMountTrigger, DismountType, DisableComponents, Editor_Tabs1;


        //bool EventHelp = false;
        //bool CallHelp = false;

        protected virtual void OnEnable()
        {
            M = (MRider)target;
            script = MonoScript.FromMonoBehaviour(M);

            MountStored = serializedObject.FindProperty("MountStored");
            MainCollider = serializedObject.FindProperty("MainCollider");
            ModifyMainCollider = serializedObject.FindProperty("ModifyMainCollider");

            ReSync = serializedObject.FindProperty("ReSync");

            m_CanMount = serializedObject.FindProperty("m_CanMount");
            m_CanDismount = serializedObject.FindProperty("m_CanDismount");
            m_CanCallAnimal = serializedObject.FindProperty("m_CanCallAnimal");
            gravity = serializedObject.FindProperty("Gravity");


            animator = serializedObject.FindProperty("animator");
            m_rigidBody = serializedObject.FindProperty("m_rigidBody");
            m_root = serializedObject.FindProperty("m_root");
            StartMounted = serializedObject.FindProperty("StartMounted");
            Parent = serializedObject.FindProperty("Parent");
            MountLayer = serializedObject.FindProperty("MountLayer");
            LayerPath = serializedObject.FindProperty("LayerPath");
            // Editor_RiderCallAnimal = serializedObject.FindProperty("Editor_RiderCallAnimal");


            Editor_Tabs1 = serializedObject.FindProperty("Editor_Tabs1");

            OnCanMount = serializedObject.FindProperty("OnCanMount");
            OnCanDismount = serializedObject.FindProperty("OnCanDismount");
            OnStartMounting = serializedObject.FindProperty("OnStartMounting");
            OnEndMounting = serializedObject.FindProperty("OnEndMounting");
            OnStartDismounting = serializedObject.FindProperty("OnStartDismounting");
            OnEndDismounting = serializedObject.FindProperty("OnEndDismounting");
            OnFindMount = serializedObject.FindProperty("OnFindMount");
            CanCallMount = serializedObject.FindProperty("CanCallMount");
            OnAlreadyMounted = serializedObject.FindProperty("OnAlreadyMounted");

            //CreateColliderMounted = serializedObject.FindProperty("CreateColliderMounted");
            //Col_Center = serializedObject.FindProperty("Col_Center");
            //Col_height = serializedObject.FindProperty("Col_height");
            //Col_radius = serializedObject.FindProperty("Col_radius");
            //Col_Trigger = serializedObject.FindProperty("Col_Trigger");

            CallAnimalA = serializedObject.FindProperty("CallAnimalA");
            StopAnimalA = serializedObject.FindProperty("StopAnimalA");
            RiderAudio = serializedObject.FindProperty("RiderAudio");

            LinkUpdate = serializedObject.FindProperty("LinkUpdate");
            //CanMount = serializedObject.FindProperty("CanMount");
            //CanDismount = serializedObject.FindProperty("CanDismount");
            debug = serializedObject.FindProperty("debug");
            AlingMountTrigger = serializedObject.FindProperty("AlingMountTrigger");
            DismountType = serializedObject.FindProperty("DismountType");
            //MainCamera = serializedObject.FindProperty("MainCamera");
            //Target = serializedObject.FindProperty("Target");

            DisableComponents = serializedObject.FindProperty("DisableComponents");
            DisableList = serializedObject.FindProperty("DisableList");
            // DismountMountOnDeath = serializedObject.FindProperty("DismountMountOnDeath");
        }

        #region GUICONTENT
        private readonly GUIContent G_DisableComponents = new GUIContent("Disable Components", "If some of the scripts are breaking the Rider Script: disable them");
        private readonly GUIContent G_DisableList = new GUIContent("Disable List", "Monobehaviours that will be disabled while mounted");
        private readonly GUIContent G_CreateColliderMounted = new GUIContent("Create capsule collider while Mounted", "This collider is for hit the Rider while mounted");
        private readonly GUIContent G_Parent = new GUIContent("Parent to Mount", "Parent the Rider to the Mount Point on the Mountable Animal");
        private readonly GUIContent G_DismountType = new GUIContent("Dismount Type", "Changes the Dismount animation on the Rider.\nRandom: Randomly select a Dismount Animation.\nInput: Select the Dismount Animation by the Horizontal and Vertical Input Axis.\n Last: Uses the Last Mount Animation as a reference for the Dismount Animation.");
        // private readonly GUIContent G_DismountMountOnDeath = new GUIContent("Dismount if mount dies", "The Rider will automatically dismount if the Animal Dies");
        #endregion

        public override void OnInspectorGUI()
        {
            MalbersEditor.DrawDescription("Riding Logic");
            EditorGUILayout.BeginVertical(MalbersEditor.StyleGray);
            MalbersEditor.DrawScript(script);

            serializedObject.Update();

            Editor_Tabs1.intValue = GUILayout.Toolbar(Editor_Tabs1.intValue, new string[] { "General", "Events", "Advanced", "Debug" });


            int Selection = Editor_Tabs1.intValue;

            if (Selection == 0) DrawGeneral();
            else if (Selection == 1) DrawEvents();
            else if (Selection == 2) DrawAdvanced();
            else if (Selection == 3) DrawDebug();

            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.EndVertical();

            AddMountLayer();

        }

        private void AddMountLayer()
        {
            Animator anim = M.GetComponentInChildren<Animator>();

            UnityEditor.Animations.AnimatorController controller = null;

            if (anim) controller = (UnityEditor.Animations.AnimatorController)anim.runtimeAnimatorController;


            if (controller)
            {
                List<UnityEditor.Animations.AnimatorControllerLayer> layers = controller.layers.ToList();

                if (layers.Find(layer => layer.name == M.MountLayer) == null)

                    if (GUILayout.Button(new GUIContent("Add Mounted Layer", "Used this to add the Parameters and 'Mounted' Layer from the Mounted Animator to your custom TCP animator ")))
                    {
                        AddLayerMounted(controller);
                    }
            }
        }



        private void DrawDebug()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.PropertyField(debug);

                if (Application.isPlaying && debug.boolValue)
                {
                    EditorGUI.BeginDisabledGroup(true);

                    EditorGUILayout.ToggleLeft("Can Mount", M.CanMount);
                    EditorGUILayout.ToggleLeft("Can Dismount", M.CanDismount);
                    EditorGUILayout.ToggleLeft("Can Call Animal", M.CanCallAnimal);
                    EditorGUILayout.Space();
                    EditorGUILayout.ToggleLeft("Mounted", M.Mounted);

                    EditorGUILayout.ToggleLeft("Is on Horse", M.IsOnHorse);
                    EditorGUILayout.ToggleLeft("Is Mounting", M.IsMounting);
                    EditorGUILayout.ToggleLeft("Is Riding", M.IsRiding);
                    EditorGUILayout.ToggleLeft("Is Dismounting", M.IsDismounting);
                    //EditorGUILayout.FloatField("Straight Spine", M.SP_Weight);
                    EditorGUILayout.Space();
                    EditorGUILayout.ObjectField("Current Mount", M.Montura, typeof(Mount), false);
                    EditorGUILayout.ObjectField("Mount Trigger", M.MountTrigger, typeof(MountTriggers), false);
                    EditorGUI.EndDisabledGroup();
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawAdvanced()
        {

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(animator);
                EditorGUILayout.PropertyField(m_rigidBody);
                EditorGUILayout.PropertyField(m_root, new GUIContent("Rider's Root", "Root Gameobject for the Rider Character"));
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.PropertyField(ReSync);
                EditorGUILayout.PropertyField(AlingMountTrigger, new GUIContent("Align MTrigger Time", "Time to Align to the Mount Trigger Position while is playing the Mount Animation"));
                EditorGUILayout.PropertyField(LayerPath);
                EditorGUILayout.PropertyField(MountLayer);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawEvents()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                //    if (EventHelp)
                //    {
                //        EditorGUILayout.HelpBox(
                //            "On Start Mounting: Invoked when the rider start the mount animation. " +
                //            "\n\nOn End Mounting: Invoked when the rider finish the mount animation.\n" +
                //            "\nOn Start Dismounting: Invoked when the rider start the dismount animation.\n" +
                //            "\nOn End Dismounting: Invoked when the rider finish the dismount animation." +
                //            "\nOn Find Mount: Invoked when the rider founds something to mount.", MessageType.None);
                //    }

                EditorGUILayout.PropertyField(OnCanMount);
                EditorGUILayout.PropertyField(OnCanDismount);

                EditorGUILayout.PropertyField(OnStartMounting);
                EditorGUILayout.PropertyField(OnEndMounting);
                EditorGUILayout.PropertyField(OnStartDismounting);
                EditorGUILayout.PropertyField(OnEndDismounting);

                EditorGUILayout.PropertyField(OnFindMount);
                EditorGUILayout.PropertyField(CanCallMount);

                if (M.StartMounted.Value)
                {
                    EditorGUILayout.PropertyField(OnAlreadyMounted);
                }

            }
            EditorGUILayout.EndVertical();
        }

        private void DrawGeneral()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.PropertyField(StartMounted, new GUIContent("Start Mounted", "Set an animal to start mounted on it"));
                EditorGUILayout.PropertyField(MountStored, new GUIContent("Stored Mount", "If Start Mounted is Active this will be the Animal to mount"));

                if (M.StartMounted.Value && MountStored.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Select a Animal with 'IMount' interface from the scene if you want to start mounted on it", MessageType.Warning);
                }
            }
            EditorGUILayout.EndVertical();


            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.PropertyField(Parent, G_Parent);
                EditorGUILayout.PropertyField(LinkUpdate, new GUIContent("Link Update", "Updates Everyframe the position and rotation of the rider to the Animal Mount Point"));
                EditorGUILayout.PropertyField(DismountType, G_DismountType);
                EditorGUILayout.PropertyField(gravity, new GUIContent("Gravity Dir"));
            }
            EditorGUILayout.EndVertical();



            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                // CreateColliderMounted.boolValue = EditorGUILayout.ToggleLeft(G_CreateColliderMounted, CreateColliderMounted.boolValue);
                EditorGUILayout.LabelField("Rider Collider", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(MainCollider, new GUIContent("Main Collider", "Main Character collider for the Rider"));
                EditorGUILayout.PropertyField(ModifyMainCollider, new GUIContent("Collider Modifier", "When mounting the Collider will change its properties"));
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.PropertyField(DisableComponents, G_DisableComponents);

                if (M.DisableComponents)
                {
                    MalbersEditor.Arrays(DisableList, G_DisableList);

                    if (M.DisableList != null && M.DisableList.Length == 0)
                    {
                        EditorGUILayout.HelpBox("If 'Disable List' is empty , it will disable all Monovehaviours while riding", MessageType.Info);
                    }
                }
            }
            EditorGUILayout.EndVertical();


            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.PropertyField(m_CanMount, new GUIContent("Can Mount", "It will be enabled when the Rider is near a mount Trigger,\nIt's used on the Active parameter of the Mount Input"));
                EditorGUILayout.PropertyField(m_CanDismount, new GUIContent("Can Dismount", "It will be enabled when the Rider riding a mount,\nIt's used on the Active parameter of the Dismount Input"));
                EditorGUILayout.PropertyField(m_CanCallAnimal, new GUIContent("Can Call Mount", "It will be enabled when the Rider has a Mount Stored and is not near or mounted is near the mount,\nIt's used on the Active parameter of the Can call Mount Input"));
            }
            EditorGUILayout.EndVertical();
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.PropertyField(CallAnimalA, new GUIContent("Call Animal", "Sound to call the Stored Animal"));
                EditorGUILayout.PropertyField(StopAnimalA, new GUIContent("Stop Animal", "Sound to stop calling the Stored Animal"));
                EditorGUILayout.PropertyField(RiderAudio, new GUIContent("Audio Source", "The reference for the audio source"));
            }
            EditorGUILayout.EndVertical();
        }

        void AddLayerMounted(UnityEditor.Animations.AnimatorController AnimController)
        {
            var MountAnimator = Resources.Load<UnityEditor.Animations.AnimatorController>(M.LayerPath);

            AddParametersOnAnimator(AnimController, MountAnimator);

            foreach (var item in MountAnimator.layers)
            {
                AnimController.AddLayer(item);
            }
        }

        public static void AddParametersOnAnimator(UnityEditor.Animations.AnimatorController AnimController, UnityEditor.Animations.AnimatorController Mounted)
        {
            AnimatorControllerParameter[] parameters = AnimController.parameters;
            AnimatorControllerParameter[] Mountedparameters = Mounted.parameters;

            foreach (var param in Mountedparameters)
            {
                if (!SearchParameter(parameters, param.name))
                {
                    AnimController.AddParameter(param);
                }
            }
        }

        public static bool SearchParameter(AnimatorControllerParameter[] parameters, string name)
        {
            foreach (AnimatorControllerParameter item in parameters)
            {
                if (item.name == name) return true;
            }
            return false;
        }
    }
#endif
    #endregion
}