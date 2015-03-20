﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using InfernalRobotics.Command;
using InfernalRobotics.Effects;
using InfernalRobotics.Gui;
using KSP.IO;
using KSPAPIExtensions;
using UnityEngine;

namespace InfernalRobotics.Module
{
    public class MuMechToggle : PartModule
    {

        private const string ELECTRIC_CHARGE_RESOURCE_NAME = "ElectricCharge";
        private const float SPEED = 0.5f;

        private static Material debugMaterial;
        private static int globalCreationOrder;
        private ElectricChargeConstraintData electricChargeConstraintData;
        private ConfigurableJoint joint;

        [KSPField(isPersistant = true)] public float customSpeed = 1;
        [KSPField(isPersistant = true)] public Vector3 fixedMeshOriginalLocation;

        [KSPField(isPersistant = true)]
        public string forwardKey
        {
            get { return forwardKeyStore; }
            set { forwardKeyStore = value.ToLower(); }
        }

        [KSPField(isPersistant = true)] public bool freeMoving = false;
        [KSPField(isPersistant = true)] public string groupName = "";
        [KSPField(isPersistant = true)] public bool hasModel = false;
        [KSPField(isPersistant = true)] public bool invertAxis = false;
        [KSPField(isPersistant = true)] public bool isMotionLock;
        [KSPField(isPersistant = true)] public bool limitTweakable = false;
        [KSPField(isPersistant = true)] public bool limitTweakableFlag = false;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Max Range", guiFormat = "F2", guiUnits = ""), UI_FloatEdit(minValue = -360f, maxValue = 360f, incrementSlide = 0.01f, scene = UI_Scene.All)] 
        public float maxTweak = 360;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Min Range", guiFormat = "F2", guiUnits = ""), UI_FloatEdit(minValue = -360f, maxValue = 360f, incrementSlide = 0.01f, scene = UI_Scene.All)] 
        public float minTweak = 0;

        [KSPField(isPersistant = true)] public bool on = false;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Sound Pitch", guiFormat = "F2", guiUnits = ""),
         UI_FloatEdit(minValue = -10f, maxValue = 10f, incrementSlide = 1f, scene = UI_Scene.All)]
        public float pitchSet = 1f;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Sound Vol", guiFormat = "F2", guiUnits = ""),
            UI_FloatRange(minValue = 0f, maxValue = 1f, stepIncrement = 0.01f)]
        public float soundSet = .5f;

        [KSPField(isPersistant = true)]
        public string revRotateKey
        {
            get { return reverseRotateKeyStore; }
            set { reverseRotateKeyStore = value.ToLower(); }
        }

        [KSPField(isPersistant = true)]
        public string reverseKey
        {
            get { return reverseKeyStore; }
            set { reverseKeyStore = value.ToLower(); }
        }

        [KSPField(isPersistant = true)] public string rotateKey = "";
        [KSPField(isPersistant = true)] public bool rotateLimits = false;
        [KSPField(isPersistant = true)] public float rotateMax = 360;
        [KSPField(isPersistant = true)] public float rotateMin = 0;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Rotation:")] public float rotation = 0;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false)] public float rotationDelta = 0;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false)] public float rotationEuler = 0;
        [KSPField(isPersistant = true)] public string servoName = "";

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Speed", guiFormat = "0.00"), 
         UI_FloatEdit(minValue = 0f, incrementSlide = 0.1f, incrementSmall=1, incrementLarge=10)]
        public float speedTweak = 1f;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Accel", guiFormat = "0.00"), 
         UI_FloatEdit(minValue = 0.05f, incrementSlide = 0.1f, incrementSmall=1, incrementLarge=10)]
        public float accelTweak = 4f;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Step Increment"), UI_ChooseOption(options = new[] {"0.01", "0.1", "1.0"})] 
        public string stepIncrement = "0.1";

        [KSPField(isPersistant = true)] public bool translateLimits = false;
        [KSPField(isPersistant = true)] public float translateMax = 3;
        [KSPField(isPersistant = true)] public float translateMin = 0;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Translation")] 
        public float translation = 0f;
        [KSPField(isPersistant = true)] public float translationDelta = 0;

        [KSPField(isPersistant = true)]
        public string presetPositionsSerialized = "";

        [KSPField(isPersistant = false)] public string bottomNode = "bottom";
        [KSPField(isPersistant = false)] public bool debugColliders = false;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Electric Charge required", guiUnits = "EC/s")] public float electricChargeRequired = 2.5f;
        [KSPField(isPersistant = false)] public string fixedMesh = string.Empty;
        [KSPField(isPersistant = false)] public float friction = 0.5f;
        [KSPField(isPersistant = false)] public bool invertSymmetry = true;
        [KSPField(isPersistant = false)] public float jointDamping = 0;
        [KSPField(isPersistant = false)] public float jointSpring = 0;
        [KSPField(isPersistant = false)] public float keyRotateSpeed = 0;
        [KSPField(isPersistant = false)] public float keyTranslateSpeed = 0;
        [KSPField(isPersistant = false)]
        public string motorSndPath = "MagicSmokeIndustries/Sounds/infernalRoboticMotor";
        [KSPField(isPersistant = false)] public float offAngularDrag = 2.0f;
        [KSPField(isPersistant = false)] public float offBreakingForce = 22.0f;
        [KSPField(isPersistant = false)] public float offBreakingTorque = 22.0f;
        [KSPField(isPersistant = false)] public float offCrashTolerance = 9.0f;
        [KSPField(isPersistant = false)] public float offMaximumDrag = 0.2f;
        [KSPField(isPersistant = false)] public float offMinimumDrag = 0.2f;
        [KSPField(isPersistant = false)] public string offModel = "off";
        [KSPField(isPersistant = false)] public bool onActivate = true;
        [KSPField(isPersistant = false)] public string onKey = string.Empty;
        [KSPField(isPersistant = false)] public float onRotateSpeed = 0;
        [KSPField(isPersistant = false)] public float onTranslateSpeed = 0;
        [KSPField(isPersistant = false)] public float onAngularDrag = 2.0f;
        [KSPField(isPersistant = false)] public float onBreakingForce = 22.0f;
        [KSPField(isPersistant = false)] public float onBreakingTorque = 22.0f;
        [KSPField(isPersistant = false)] public float onCrashTolerance = 9.0f;
        [KSPField(isPersistant = false)] public float onMaximumDrag = 0.2f;
        [KSPField(isPersistant = false)] public float onMinimumDrag = 0.2f;
        [KSPField(isPersistant = false)] public string onModel = "on";
        [KSPField(isPersistant = false)] public Part origRootPart;
        [KSPField(isPersistant = false)] public string revTranslateKey = string.Empty;
        [KSPField(isPersistant = false)] public Vector3 rotateAxis = Vector3.forward;
        [KSPField(isPersistant = false)] public bool rotateJoint = false; 
        [KSPField(isPersistant = false)] public bool rotateLimitsOff = false;
        [KSPField(isPersistant = false)] public bool rotateLimitsRevertKey = false;
        [KSPField(isPersistant = false)] public bool rotateLimitsRevertOn = true;
        [KSPField(isPersistant = false)] public Vector3 rotatePivot = Vector3.zero;
        [KSPField(isPersistant = false)] public string rotateModel = "on";
        [KSPField(isPersistant = false)] public bool showGUI = false;
        [KSPField(isPersistant = false)] public bool toggleBreak = false;
        [KSPField(isPersistant = false)] public bool toggleCollision = false;
        [KSPField(isPersistant = false)] public bool toggleDrag = false;
        [KSPField(isPersistant = false)] public bool toggleModel = false;
        [KSPField(isPersistant = false)] public Vector3 translateAxis = Vector3.forward;
        [KSPField(isPersistant = false)] public bool translateJoint = false;
        [KSPField(isPersistant = false)] public string translateKey = string.Empty;
        [KSPField(isPersistant = false)] public bool translateLimitsOff = false;
        [KSPField(isPersistant = false)] public bool translateLimitsRevertKey = false;
        [KSPField(isPersistant = false)] public bool translateLimitsRevertOn = true;
        [KSPField(isPersistant = false)] public string translateModel = "on";

        private SoundSource motorSound;
        private string reverseKeyStore;
        private string reverseRotateKeyStore;
        private string forwardKeyStore;

        static MuMechToggle()
        {
            ResetWin = false;
        }

        public MuMechToggle()
        {
            Interpolator = new Interpolator();
            Translator = new Translator();
            RotationLast = 0;
            GroupElectricChargeRequired = 2.5f;
            OriginalTranslation = 0f;
            OriginalAngle = 0f;
            TweakIsDirty = false;
            UseElectricCharge = true;
            CreationOrder = 0;
            MoveFlags = 0;
            TranslationChanged = 0;
            RotationChanged = 0;
            MobileColliders = new List<Transform>();
            GotOrig = false;
            forwardKey = "";
            reverseKey = "";
            revRotateKey = "";

            //motorSound = new SoundSource(this.part, "motor");
        }

        protected Vector3 OrigTranslation { get; set; }
        protected bool GotOrig { get; set; }
        protected List<Transform> MobileColliders { get; set; }
        protected int RotationChanged { get; set; }
        protected int TranslationChanged { get; set; }
        protected Transform ModelTransform { get; set; }
        protected Transform OnModelTransform { get; set; }
        protected Transform OffModelTransform { get; set; }
        protected Transform RotateModelTransform { get; set; }
        protected Transform TranslateModelTransform { get; set; }
        protected bool UseElectricCharge { get; set; }
        //protected bool Loaded { get; set; }
        protected static Rect ControlWinPos2 { get; set; }
        protected static bool ResetWin { get; set; }

        //Interpolator represents a controller, assuring smooth movements
        public Interpolator Interpolator { get; set; }

        //Translator represents an interface to interact with the servo
        public Translator Translator { get; set; }
        public float RotationLast { get; set; }
        public Transform FixedMeshTransform { get; set; }
        public float GroupElectricChargeRequired { get; set; }
        public float LastPowerDraw { get; set; }
        public int MoveFlags { get; set; }
        public int CreationOrder { get; set; }
        public UIPartActionWindow TweakWindow { get; set; }
        public bool TweakIsDirty { get; set; }
        public float OriginalAngle { get; set; }
        public float OriginalTranslation { get; set; }

        public List<float> PresetPositions { get; set; }

        private Assembly MyResolveEventHandler(object sender, ResolveEventArgs args)
        {
            //This handler is called only when the common language runtime tries to bind to the assembly and fails.

            //Retrieve the list of referenced assemblies in an array of AssemblyName.
            string strTempAssmbPath = "";
;
            Assembly objExecutingAssembly = Assembly.GetExecutingAssembly();
            AssemblyName[] arrReferencedAssmbNames = objExecutingAssembly.GetReferencedAssemblies();

            //Loop through the array of referenced assembly names.
            foreach (AssemblyName strAssmbName in arrReferencedAssmbNames)
            {
                //Check for the assembly names that have raised the "AssemblyResolve" event.
                if (strAssmbName.FullName.Substring(0, strAssmbName.FullName.IndexOf(",")) ==
                    args.Name.Substring(0, args.Name.IndexOf(",")))
                {
                    //Build the path of the assembly from where it has to be loaded.        
                    Debug.Log("looking!");
                    strTempAssmbPath = "C:\\Myassemblies\\" + args.Name.Substring(0, args.Name.IndexOf(",")) + ".dll";
                    break;
                }
            }

            //Load the assembly from the specified path.                    
            Assembly myAssembly = Assembly.LoadFrom(strTempAssmbPath);

            //Return the loaded assembly.
            return myAssembly;
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Rotate Limits are Off", active = false)]
        public void LimitTweakableToggle()
        {
            limitTweakableFlag = !limitTweakableFlag;
            Events["LimitTweakableToggle"].guiName = limitTweakableFlag ? "Rotate Limits are On" : "Rotate Limits are Off";
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Invert Axis is Off")]
        public void InvertAxisToggle()
        {
            invertAxis = !invertAxis;
            Translator.IsAxisInverted = invertAxis;
            Events["InvertAxisToggle"].guiName = invertAxis ? "Invert Axis is On" : "Invert Axis is Off";
        }

        public bool IsSymmMaster()
        {
            return part.symmetryCounterparts.All( cp => ((MuMechToggle) cp.Modules["MuMechToggle"]).CreationOrder >= CreationOrder);
        }

        public void UpdateState()
        {
            if (on)
            {
                if (toggleModel)
                {
                    OnModelTransform.renderer.enabled = true;
                    OffModelTransform.renderer.enabled = false;
                }
                if (toggleDrag)
                {
                    part.angularDrag = onAngularDrag;
                    part.minimum_drag = onMinimumDrag;
                    part.maximum_drag = onMaximumDrag;
                }
                if (toggleBreak)
                {
                    part.crashTolerance = onCrashTolerance;
                    part.breakingForce = onBreakingForce;
                    part.breakingTorque = onBreakingTorque;
                }
            }
            else
            {
                if (toggleModel)
                {
                    OnModelTransform.renderer.enabled = false;
                    OffModelTransform.renderer.enabled = true;
                }
                if (toggleDrag)
                {
                    part.angularDrag = offAngularDrag;
                    part.minimum_drag = offMinimumDrag;
                    part.maximum_drag = offMaximumDrag;
                }
                if (toggleBreak)
                {
                    part.crashTolerance = offCrashTolerance;
                    part.breakingForce = offBreakingForce;
                    part.breakingTorque = offBreakingTorque;
                }
            }
            if (toggleCollision)
            {
                part.collider.enabled = on;
                part.collisionEnhancer.enabled = on;
                part.terrainCollider.enabled = on;
            }
        }

        private void OnDestroy()
        {
            PositionLock(false);
        }

        protected void ColliderizeChilds(Transform obj)
        {
            if (obj.name.StartsWith("node_collider")
                || obj.name.StartsWith("fixed_node_collider")
                || obj.name.StartsWith("mobile_node_collider"))
            {
                print("Toggle: converting collider " + obj.name);

                if (!obj.GetComponent<MeshFilter>())
                {
                    print("Collider has no MeshFilter (yet?): skipping Colliderize");
                }
                else
                {
                    var sharedMesh = Instantiate(obj.GetComponent<MeshFilter>().mesh) as Mesh;
                    Destroy(obj.GetComponent<MeshFilter>());
                    Destroy(obj.GetComponent<MeshRenderer>());
                    var meshCollider = obj.gameObject.AddComponent<MeshCollider>();
                    meshCollider.sharedMesh = sharedMesh;
                    meshCollider.convex = true;
                    obj.parent = part.transform;

                    if (obj.name.StartsWith("mobile_node_collider"))
                    {
                        MobileColliders.Add(obj);
                    }
                }
            }
            for (int i = 0; i < obj.childCount; i++)
            {
                ColliderizeChilds(obj.GetChild(i));
            }
        }

        public override void OnAwake()
        {
            Debug.Log("[IR OnAwake] Start");

            LoadConfigXml();

            FindTransforms();

            if (ModelTransform == null)
                Debug.LogWarning("[IR OnAwake] ModelTransform is null");

            ColliderizeChilds(ModelTransform);

            try
            {
                if (rotateJoint)
                {
                    minTweak = rotateMin;
                    maxTweak = rotateMax;
                    
                    if (limitTweakable)
                    {
                        Events["LimitTweakableToggle"].active = true;
                    }

                    if (freeMoving)
                    {
                        Events["InvertAxisToggle"].active = false;
                        Fields["minTweak"].guiActive = false;
                        Fields["minTweak"].guiActiveEditor = false;
                        Fields["maxTweak"].guiActive = false;
                        Fields["maxTweak"].guiActiveEditor = false;
                        Fields["speedTweak"].guiActive = false;
                        Fields["speedTweak"].guiActiveEditor = false;
                        Fields["speedTweakFine"].guiActive = false;
                        Fields["speedTweakFine"].guiActiveEditor = false;
                        Events["Activate"].active = false;
                        Events["Deactivate"].active = false;
                        Fields["stepIncrement"].guiActiveEditor = false;
                        Fields["stepIncrement"].guiActive = false;
                    }
                    
                    
                    Fields["translation"].guiActive = false;
                    Fields["translation"].guiActiveEditor = false;
                }
                else if (translateJoint)
                {
                    minTweak = translateMin;
                    maxTweak = translateMax;
                    
                    Events["LimitTweakableToggle"].active = false;
                    
                    Fields["rotation"].guiActive = false;
                    Fields["rotation"].guiActiveEditor = false;
                }

                if (motorSound==null) motorSound = new SoundSource(part, "motor");
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format("MMT.OnAwake exception {0}", ex.Message));
            }
            
            GameScenes scene = HighLogic.LoadedScene;
            if (scene == GameScenes.EDITOR)
            {
                if (rotateJoint)
                    ParseMinMaxTweaks(rotateMin, rotateMax);
                else if (translateJoint)
                    ParseMinMaxTweaks(translateMin, translateMax);
            }

            ParsePresetPositions();

            FixedMeshTransform = KSPUtil.FindInPartModel(transform, fixedMesh);

            Debug.Log("[IR OnAwake] End, rotateLimits=" + rotateLimits + ", minTweak=" + minTweak + ", maxTweak=" + maxTweak + ", rotateJoint=" + rotateLimits);
        }

        public Transform FindFixedMesh(Transform meshTransform)
        {
            Transform t = part.transform.FindChild("model").FindChild(fixedMesh);

            return t;
        }


        public override void OnSave(ConfigNode node)
        {
            Debug.Log("[IR OnSave] Start");
            base.OnSave(node);
            if (rotateJoint)
                ParseMinMaxTweaks(rotateMin, rotateMax);
            else if (translateJoint)
                ParseMinMaxTweaks(translateMin, translateMax);
            GameScenes scene = HighLogic.LoadedScene;

            if (scene == GameScenes.EDITOR)
            {
                if (rotateJoint)
                {
                    if (part.name.Contains("IR.Rotatron.OffAxis") && rotationEuler != 0f)
                    {
                        rotation = rotationEuler/0.7070f;
                        rotationEuler = 0f;
                    }
                }
                else
                    rotation = rotationEuler;
            }

            presetPositionsSerialized = SerializePresets();

            Debug.Log("[IR OnSave] End");
        }

        public void RefreshKeys()
        {
            translateKey = forwardKey;
            revTranslateKey = reverseKey;
            rotateKey = forwardKey;
            revRotateKey = reverseKey;
        }

        public void ParsePresetPositions()
        {
            string[] positionChunks = presetPositionsSerialized.Split('|');
            PresetPositions = new List<float> { };
            foreach (string chunk in positionChunks)
            {
                float tmp = 0;
                if(float.TryParse(chunk,out tmp))
                {
                    PresetPositions.Add(tmp);
                }
            }
        }

        public string SerializePresets()
        {
            string tmp = "";

            foreach (float s in PresetPositions)
            {
                tmp += s.ToString() + "|";
            }

            return tmp;
        }

        public override void OnLoad(ConfigNode config)
        {
            //Loaded = true;
            Debug.Log("[IR OnLoad] Start");

            FindTransforms();

            if (ModelTransform == null)
                Debug.LogWarning("[IR OnLoad] ModelTransform is null");

            ColliderizeChilds(ModelTransform);
            //maybe???
            rotationDelta = RotationLast = rotation;
            translationDelta = translation;

            GameScenes scene = HighLogic.LoadedScene;

            
            //TODO get rid of this hardcoded non-sense

            if (scene == GameScenes.FLIGHT)
            {
                if (part.name.Contains("Gantry"))
                {
                    FixedMeshTransform.Translate((-translateAxis.x*translation*2),
                        (-translateAxis.y*translation*2),
                        (-translateAxis.z*translation*2), Space.Self);
                }
            }

            

            if (scene == GameScenes.EDITOR)
            {
                if (part.name.Contains("Gantry"))
                {
                    FixedMeshTransform.Translate((-translateAxis.x*translation),
                        (-translateAxis.y*translation),
                        (-translateAxis.z*translation), Space.Self);
                }

                if (rotateJoint)
                {
                    if (!part.name.Contains("IR.Rotatron.OffAxis"))
                    {
                        FixedMeshTransform.Rotate(rotateAxis, -rotationEuler);
                    }
                    else
                    {
                        FixedMeshTransform.eulerAngles = (fixedMeshOriginalLocation);
                    }
                }
                else if (translateJoint && !part.name.Contains("Gantry"))
                {
                    FixedMeshTransform.Translate((translateAxis.x*translation),
                        (translateAxis.y*translation),
                        (translateAxis.z*translation), Space.Self);
                }
            }


            translateKey = forwardKey;
            revTranslateKey = reverseKey;
            rotateKey = forwardKey;
            revRotateKey = reverseKey;
            
            if (rotateJoint)
                ParseMinMaxTweaks(rotateMin, rotateMax);
            else if (translateJoint)
                ParseMinMaxTweaks(translateMin, translateMax);

            ParsePresetPositions();

            Debug.Log("[IR OnLoad] End");
        }

        private void ParseMinMaxTweaks(float movementMinimum, float movementMaximum)
        {
            var rangeMinF = (UI_FloatEdit) Fields["minTweak"].uiControlFlight;
            var rangeMinE = (UI_FloatEdit) Fields["minTweak"].uiControlEditor;
            rangeMinE.minValue = movementMinimum;
            rangeMinE.maxValue = movementMaximum;
            rangeMinE.incrementSlide = float.Parse(stepIncrement);
            rangeMinF.minValue = movementMinimum;
            rangeMinF.maxValue = movementMaximum;
            rangeMinF.incrementSlide = float.Parse(stepIncrement);
            var rangeMaxF = (UI_FloatEdit) Fields["maxTweak"].uiControlFlight;
            var rangeMaxE = (UI_FloatEdit) Fields["maxTweak"].uiControlEditor;
            rangeMaxE.minValue = movementMinimum;
            rangeMaxE.maxValue = movementMaximum;
            rangeMaxE.incrementSlide = float.Parse(stepIncrement);
            rangeMaxF.minValue = movementMinimum;
            rangeMaxF.maxValue = movementMaximum;
            rangeMaxF.incrementSlide = float.Parse(stepIncrement);

            if (rotateJoint)
            {
                Fields["minTweak"].guiName = "Min Rotate";
                Fields["maxTweak"].guiName = "Max Rotate";
            }
            else if (translateJoint)
            {
                Fields["minTweak"].guiName = "Min Translate";
                Fields["maxTweak"].guiName = "Max Translate";
            }
        }

        protected void DebugCollider(MeshCollider toDebug)
        {
            if (debugMaterial == null)
            {
                debugMaterial = new Material(Shader.Find("Self-Illumin/Specular"))
                {
                    color = Color.red
                };
            }
            MeshFilter mf = toDebug.gameObject.GetComponent<MeshFilter>()
                            ?? toDebug.gameObject.AddComponent<MeshFilter>();
            mf.sharedMesh = toDebug.sharedMesh;
            MeshRenderer mr = toDebug.gameObject.GetComponent<MeshRenderer>()
                              ?? toDebug.gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = debugMaterial;
        }

        protected void AttachToParent(Transform obj)
        {
            Transform fix = FixedMeshTransform;
            if (rotateJoint)
            {
                fix.RotateAround(transform.TransformPoint(rotatePivot), transform.TransformDirection(rotateAxis),
                    (invertSymmetry ? ((IsSymmMaster() || (part.symmetryCounterparts.Count != 1)) ? -1 : 1) : -1)*
                    rotation);
            }
            else if (translateJoint)
            {
                fix.Translate(transform.TransformDirection(translateAxis.normalized)*translation, Space.World);
            }
            fix.parent = part.parent.transform;
        }

        protected void ReparentFriction(Transform obj)
        {
            for (int i = 0; i < obj.childCount; i++)
            {
                Transform child = obj.GetChild(i);
                var tmp = child.GetComponent<MeshCollider>();
                if (tmp != null)
                {
                    tmp.material.dynamicFriction = tmp.material.staticFriction = friction;
                    tmp.material.frictionCombine = PhysicMaterialCombine.Maximum;
                    if (debugColliders)
                    {
                        DebugCollider(tmp);
                    }
                }
                if (child.name.StartsWith("fixed_node_collider") && (part.parent != null))
                {
                    print("Toggle: reparenting collider " + child.name);
                    AttachToParent(child);
                }
            }
            if ((MobileColliders.Count > 0) && (RotateModelTransform != null))
            {
                foreach (Transform c in MobileColliders)
                {
                    c.parent = RotateModelTransform;
                }
            }
        }

        public void BuildAttachments()
        {
            if (part.findAttachNodeByPart(part.parent).id.Contains(bottomNode)
                || part.attachMode == AttachModes.SRF_ATTACH)
            {
                if (fixedMesh != "")
                {
                    //Transform fix = model_transform.FindChild(fixedMesh);
                    Transform fix = FixedMeshTransform;
                    if ((fix != null) && (part.parent != null))
                    {
                        AttachToParent(fix);
                    }
                }
            }
            else
            {
                foreach (Transform t in ModelTransform)
                {
                    if (t.name != fixedMesh)
                    {
                        AttachToParent(t);
                    }
                }
                if (translateJoint)
                    translateAxis *= -1;
            }
            ReparentFriction(part.transform);
        }

        protected void FindTransforms()
        {
            ModelTransform = part.transform.FindChild("model");
            OnModelTransform = ModelTransform.FindChild(onModel);
            OffModelTransform = ModelTransform.FindChild(offModel);
            RotateModelTransform = ModelTransform.FindChild(rotateModel);
            TranslateModelTransform = ModelTransform.FindChild(translateModel);
        }

        private void OnEditorAttach()
        {
        }

        // mrblaq return an int to multiply by rotation direction based on GUI "invert" checkbox bool
        public int GetAxisInversion()
        {
            //returns inversed Axis for OffAxis Rotatron
            if (!part.name.Contains ("IR.Rotatron.OffAxis")) {
                
                return (invertAxis ? 1 : -1);
            } 
            else
            {
                return (invertAxis ? -1 : 1);
            }
        }

        public override void OnStart(StartState state)
        {
            Debug.Log("[IR MMT] OnStart Start");

            BaseField field = Fields["stepIncrement"];

            var optionsEditor = (UI_ChooseOption)field.uiControlEditor;
            var optionsFlight = (UI_ChooseOption)field.uiControlFlight;

            if (translateJoint)
            {
                optionsEditor.options = new[] { "0.01", "0.1", "1.0" };
                optionsFlight.options = new[] { "0.01", "0.1", "1.0" };
            }
            else if (rotateJoint)
            {
                optionsEditor.options = new[] { "0.1", "1", "10" };
                optionsFlight.options = new[] { "0.1", "1", "10" };
            }
            
            //part.stackIcon.SetIcon(DefaultIcons.STRUT);
            limitTweakableFlag = rotateLimits;
            float position = rotateJoint ? rotation : translation;
            if (!float.IsNaN(position))
                Interpolator.Position = position;

            //speed from .cfg will be used as the default unit of speed
            float defaultSpeed = rotateJoint ? keyRotateSpeed : keyTranslateSpeed;

            Translator.Init(Interpolator, defaultSpeed, invertAxis, isMotionLock);

            ConfigureInterpolator();


            if (vessel == null)
            {
                Debug.Log(String.Format("[IR MMT] OnStart vessel is null"));
                return;
            }

            if (motorSound==null) motorSound = new SoundSource(part, "motor");

            motorSound.Setup(motorSndPath, true);
            CreationOrder = globalCreationOrder++;

            FindTransforms();

            if (ModelTransform == null)
                Debug.LogWarning("[IR MMT] OnStart ModelTransform is null");

            BuildAttachments();

            UpdateState();

            if (rotateJoint)
            {
                ParseMinMaxTweaks(rotateMin, rotateMax);
                if (limitTweakable)
                {
                    Events["LimitTweakableToggle"].active = true;
                }
            }
            else if (translateJoint)
            {
                ParseMinMaxTweaks(translateMin, translateMax);
                if (limitTweakable)
                {
                    Events["LimitTweakableToggle"].active = false;
                }
            }

            ParsePresetPositions();

            Debug.Log("[IR MMT] OnStart End, rotateLimits=" + rotateLimits + ", minTweak=" + minTweak + ", maxTweak=" + maxTweak);
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Configure Interpolator", active = true)]
        public void ConfigureInterpolator()
        {
            // write interpolator configuration
            // (this should not change while it is active!!)
            if (Interpolator.Active)
            {
                Debug.Log("IR: configureInterpolator: busy, reconfiguration not possible now!");
                return;
            }

            if (rotateJoint)
            {
                Interpolator.IsModulo = !limitTweakableFlag;
                Interpolator.Position = Interpolator.ReduceModulo(Interpolator.Position);
            } 
            else
                Interpolator.IsModulo = false;

            if (Interpolator.IsModulo)
            {
                Interpolator.MinPosition = -180;
                Interpolator.MaxPosition =  180;
            } 
            else 
            {
                Interpolator.MinPosition = Math.Min(minTweak, maxTweak);
                Interpolator.MaxPosition = Math.Max(minTweak, maxTweak);
            }
            Interpolator.MaxAcceleration = accelTweak * Translator.GetSpeedUnit();
            Debug.Log("IR: configureInterpolator:" + Interpolator );
        }


        public bool SetupJoints()
        {
            if (!GotOrig)
            {
                // remove for less spam in editor
                //print("setupJoints - !gotOrig");
                if (rotateJoint || translateJoint)
                {
                    if (part.attachJoint != null)
                    {
                        // Catch reversed joint
                        // Maybe there is a best way to do it?
                        if (transform.position != part.attachJoint.Joint.connectedBody.transform.position)
                        {
                            joint = part.attachJoint.Joint.connectedBody.gameObject.AddComponent<ConfigurableJoint>();
                            joint.connectedBody = part.attachJoint.Joint.rigidbody;
                        }
                        else
                        {
                            joint = part.attachJoint.Joint.rigidbody.gameObject.AddComponent<ConfigurableJoint>();
                            joint.connectedBody = part.attachJoint.Joint.connectedBody;
                        }

                        joint.breakForce = 1e15f;
                        joint.breakTorque = 1e15f;
                        // And to default joint
                        part.attachJoint.Joint.breakForce = 1e15f;
                        part.attachJoint.Joint.breakTorque = 1e15f;
                        part.attachJoint.SetBreakingForces(1e15f, 1e15f);

                        // lock all movement by default
                        joint.xMotion = ConfigurableJointMotion.Locked;
                        joint.yMotion = ConfigurableJointMotion.Locked;
                        joint.zMotion = ConfigurableJointMotion.Locked;
                        joint.angularXMotion = ConfigurableJointMotion.Locked;
                        joint.angularYMotion = ConfigurableJointMotion.Locked;
                        joint.angularZMotion = ConfigurableJointMotion.Locked;

                        joint.projectionDistance = 0f;
                        joint.projectionAngle = 0f;
                        joint.projectionMode = JointProjectionMode.PositionAndRotation;

                        // Copy drives
                        joint.linearLimit = part.attachJoint.Joint.linearLimit;
                        joint.lowAngularXLimit = part.attachJoint.Joint.lowAngularXLimit;
                        joint.highAngularXLimit = part.attachJoint.Joint.highAngularXLimit;
                        joint.angularXDrive = part.attachJoint.Joint.angularXDrive;
                        joint.angularYZDrive = part.attachJoint.Joint.angularYZDrive;
                        joint.xDrive = part.attachJoint.Joint.xDrive;
                        joint.yDrive = part.attachJoint.Joint.yDrive;
                        joint.zDrive = part.attachJoint.Joint.zDrive;

                        // Set anchor position
                        joint.anchor =
                            joint.rigidbody.transform.InverseTransformPoint(joint.connectedBody.transform.position);
                        joint.connectedAnchor = Vector3.zero;

                        // Set correct axis
                        joint.axis =
                            joint.rigidbody.transform.InverseTransformDirection(joint.connectedBody.transform.right);
                        joint.secondaryAxis =
                            joint.rigidbody.transform.InverseTransformDirection(joint.connectedBody.transform.up);


                        if (translateJoint)
                        {
                            joint.xMotion = ConfigurableJointMotion.Free;
                            joint.yMotion = ConfigurableJointMotion.Free;
                            joint.zMotion = ConfigurableJointMotion.Free;
                        }

                        if (rotateJoint)
                        {
                            //Docking washer is broken currently?
                            joint.rotationDriveMode = RotationDriveMode.XYAndZ;
                            joint.angularXMotion = ConfigurableJointMotion.Free;
                            joint.angularYMotion = ConfigurableJointMotion.Free;
                            joint.angularZMotion = ConfigurableJointMotion.Free;

                            // Docking washer test
                            if (jointSpring > 0)
                            {
                                if (rotateAxis == Vector3.right || rotateAxis == Vector3.left)
                                {
                                    JointDrive drv = joint.angularXDrive;
                                    drv.positionSpring = jointSpring;
                                    joint.angularXDrive = drv;

                                    joint.angularYMotion = ConfigurableJointMotion.Locked;
                                    joint.angularZMotion = ConfigurableJointMotion.Locked;
                                }
                                else
                                {
                                    JointDrive drv = joint.angularYZDrive;
                                    drv.positionSpring = jointSpring;
                                    joint.angularYZDrive = drv;

                                    joint.angularXMotion = ConfigurableJointMotion.Locked;
                                    joint.angularZMotion = ConfigurableJointMotion.Locked;
                                }
                            }
                        }

                        // Reset default joint drives
                        var resetDrv = new JointDrive
                        {
                            mode = JointDriveMode.PositionAndVelocity,
                            positionSpring = 0,
                            positionDamper = 0,
                            maximumForce = 0
                        };

                        part.attachJoint.Joint.angularXDrive = resetDrv;
                        part.attachJoint.Joint.angularYZDrive = resetDrv;
                        part.attachJoint.Joint.xDrive = resetDrv;
                        part.attachJoint.Joint.yDrive = resetDrv;
                        part.attachJoint.Joint.zDrive = resetDrv;

                        GotOrig = true;
                        return true;
                    }
                    return false;
                }

                GotOrig = true;
                return true;
            }
            return false;
        }

        public override void OnActive()
        {
            if (onActivate)
            {
                on = true;
                UpdateState();
            }
        }

        protected void UpdatePosition()
        {
            float pos = Interpolator.GetPosition();
            if (rotateJoint)
            {
                if (rotation != pos) 
                {
                    rotation = pos;
                    RotationChanged |= 4;
                } 
                else
                    RotationChanged = 0;
            }
            else
            {
                if (translation != pos) 
                {
                    translation = pos;
                    TranslationChanged |= 4;
                } 
                else
                    TranslationChanged = 0;
            }
        }

        protected bool KeyPressed(string key)
        {
            return (key != "" && vessel == FlightGlobals.ActiveVessel
                    && InputLockManager.IsUnlocked(ControlTypes.LINEAR)
                    && Input.GetKey(key));
        }



        protected void CheckInputs()
        {
            if (part.isConnected && KeyPressed(onKey))
            {
                on = !on;
                UpdateState();
            }

            if (KeyPressed(rotateKey) || KeyPressed(translateKey))
            {
                Translator.Move(float.PositiveInfinity, speedTweak * customSpeed);
            }
            else if (KeyPressed(revRotateKey) || KeyPressed(revTranslateKey))
            {
                Translator.Move(float.NegativeInfinity, speedTweak * customSpeed);
            }
            
        }

        protected void DoRotation()
        {
            if ((RotationChanged != 0) && (rotateJoint || RotateModelTransform != null))
            {
                if (rotateJoint && joint != null)
                {
                    joint.targetRotation =
                        Quaternion.AngleAxis(
                            (invertSymmetry ? ((IsSymmMaster() || (part.symmetryCounterparts.Count != 1)) ? 1 : -1) : 1)*
                            (rotation - rotationDelta), rotateAxis);
                    RotationLast = rotation;
                }
                else if (transform != null)
                {
                    Quaternion curRot =
                        Quaternion.AngleAxis(
                            (invertSymmetry ? ((IsSymmMaster() || (part.symmetryCounterparts.Count != 1)) ? 1 : -1) : 1)*
                            rotation, rotateAxis);
                    transform.FindChild("model").FindChild(rotateModel).localRotation = curRot;
                }
                electricChargeConstraintData.RotationDone = true;
            }
            RotationChanged = 0;
        }

        protected void DoTranslation()
        {
            if ((TranslationChanged != 0) && (translateJoint || TranslateModelTransform != null) && joint != null)
            {
                if (translateJoint)
                {
                    joint.targetPosition = -translateAxis*(translation - translationDelta);
                }
                else
                {
                    joint.targetPosition = OrigTranslation - translateAxis.normalized*(translation - translationDelta);
                }
                electricChargeConstraintData.TranslationDone = true;
            }
            TranslationChanged = 0;
        }

        public void Resized()
        {
            UIPartActionWindow[] actionWindows = FindObjectsOfType<UIPartActionWindow>();
            if (actionWindows.Length > 0)
            {
                foreach (UIPartActionWindow actionWindow in actionWindows)
                {
                    if (actionWindow.part == part)
                    {
                        TweakWindow = actionWindow;
                        TweakIsDirty = true;
                    }
                }
            }
            else
            {
                TweakWindow = null;
            }
        }


        public void RefreshTweakUI()
        {
            if (HighLogic.LoadedScene != GameScenes.EDITOR) return;
            if (TweakWindow == null) return;

            if (translateJoint)
            {
                var rangeMinF = (UI_FloatEdit) Fields["minTweak"].uiControlEditor;
                rangeMinF.minValue = translateMin;
                rangeMinF.maxValue = translateMax;
                rangeMinF.incrementSlide = float.Parse(stepIncrement);
                minTweak = translateMin;
                var rangeMaxF = (UI_FloatEdit) Fields["maxTweak"].uiControlEditor;
                rangeMaxF.minValue = translateMin;
                rangeMaxF.maxValue = translateMax;
                rangeMaxF.incrementSlide = float.Parse(stepIncrement);
                maxTweak = translateMax;
                //this.updateGroupECRequirement(this.groupName);
            }
            else if (rotateJoint)
            {
                var rangeMinF = (UI_FloatEdit) Fields["minTweak"].uiControlEditor;
                rangeMinF.minValue = rotateMin;
                rangeMinF.maxValue = rotateMax;
                rangeMinF.incrementSlide = float.Parse(stepIncrement);
                minTweak = rotateMin;
                var rangeMaxF = (UI_FloatEdit) Fields["maxTweak"].uiControlEditor;
                rangeMaxF.minValue = rotateMin;
                rangeMaxF.maxValue = rotateMax;
                rangeMaxF.incrementSlide = float.Parse(stepIncrement);
                maxTweak = rotateMax;
            }

            if (part.symmetryCounterparts.Count > 1)
            {
                foreach (Part counterPart in part.symmetryCounterparts)
                {
                    ((MuMechToggle) counterPart.Modules["MuMechToggle"]).rotateMin = rotateMin;
                    ((MuMechToggle) counterPart.Modules["MuMechToggle"]).rotateMax = rotateMax;
                    ((MuMechToggle) counterPart.Modules["MuMechToggle"]).stepIncrement = stepIncrement;
                    ((MuMechToggle) counterPart.Modules["MuMechToggle"]).minTweak = rotateMin;
                    ((MuMechToggle) counterPart.Modules["MuMechToggle"]).maxTweak = maxTweak;
                }
            }
        }

        private double GetAvailableElectricCharge()
        {
            if (!UseElectricCharge || !HighLogic.LoadedSceneIsFlight)
            {
                return electricChargeRequired;
            }
            PartResourceDefinition resDef = PartResourceLibrary.Instance.GetDefinition(ELECTRIC_CHARGE_RESOURCE_NAME);
            var resources = new List<PartResource>();
            part.GetConnectedResources(resDef.id, resDef.resourceFlowMode, resources);
            return resources.Count <= 0 ? 0f : resources.Select(r => r.amount).Sum();
        }

        void Update()
        {
            if (motorSound!=null) motorSound.Update(soundSet, pitchSet);
        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedScene != GameScenes.FLIGHT && HighLogic.LoadedScene != GameScenes.EDITOR)
            {
                return;
            }

            if (HighLogic.LoadedScene == GameScenes.EDITOR)
            {
                if (TweakWindow != null && TweakIsDirty)
                {
                    RefreshTweakUI();
                    TweakWindow.UpdateWindow();
                    TweakIsDirty = false;
                }
            }

            if (part.State == PartStates.DEAD) // not sure what this means
            {                                  // probably: the part is destroyed but the object still exists?
                return;
            }

            if (SetupJoints())
            {
                RotationChanged = 4;
                TranslationChanged = 4;
            }

            if (HighLogic.LoadedSceneIsFlight)
            {
                electricChargeConstraintData = new ElectricChargeConstraintData(GetAvailableElectricCharge(),
                    electricChargeRequired*TimeWarp.fixedDeltaTime, GroupElectricChargeRequired*TimeWarp.fixedDeltaTime);
                CheckInputs();

                if (UseElectricCharge && !electricChargeConstraintData.Available)
                    Translator.Stop();

                Interpolator.Update(TimeWarp.fixedDeltaTime);
                UpdatePosition();

                if (Translator.IsMoving())
                    motorSound.Play();
                else
                    motorSound.Stop();
            }

            if (minTweak > maxTweak)
            {
                maxTweak = minTweak;
            }

            DoRotation();
            DoTranslation();

            if (HighLogic.LoadedSceneIsFlight)
                HandleElectricCharge();

            if (vessel != null)
            {
                part.UpdateOrgPosAndRot(vessel.rootPart);
                foreach (Part child in part.FindChildParts<Part>(true))
                {
                    child.UpdateOrgPosAndRot(vessel.rootPart);
                }
            }
        }

        public void HandleElectricCharge()
        {
            if (UseElectricCharge)
            {
                if (electricChargeConstraintData.RotationDone || electricChargeConstraintData.TranslationDone)
                {
                    part.RequestResource(ELECTRIC_CHARGE_RESOURCE_NAME, electricChargeConstraintData.ToConsume);
                    float displayConsume = electricChargeConstraintData.ToConsume/TimeWarp.fixedDeltaTime;
                    if (electricChargeConstraintData.Available)
                    {
                        LastPowerDraw = displayConsume;
                    }
                    LastPowerDraw = displayConsume;
                }
                else
                {
                    LastPowerDraw = 0f;
                }
            }
        }

        public override void OnInactive()
        {
            on = false;
            UpdateState();
        }

        public void SetLock(bool isLocked)
        {
            isMotionLock = isLocked;
            Events["MotionLockToggle"].guiName = isMotionLock ? "Disengage Lock" : "Engage Lock";

            Translator.IsMotionLock = isMotionLock;
            if (isMotionLock)
                Translator.Stop();
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Engage Lock", active = true)]
        public void MotionLockToggle()
        {
            SetLock(!isMotionLock);
        }
        [KSPAction("Toggle Lock")]
        public void MotionLockToggle(KSPActionParam param)
        {
            SetLock(!isMotionLock);
        }

        public void MoveNextPreset()
        {
            float currentPosition = Interpolator.Position;
            float nextPosition = currentPosition;

            var availablePositions = PresetPositions.FindAll (s => s > currentPosition);

            if (availablePositions.Count > 0)
                nextPosition = availablePositions.Min();
            
            Debug.Log ("[IR Action] NextPreset, currentPos = " + currentPosition + ", nextPosition=" + nextPosition);

            Translator.Move(nextPosition, customSpeed * speedTweak);
        }

        public void MovePrevPreset()
        {
            float currentPosition = Interpolator.Position;
            float nextPosition = currentPosition;

            var availablePositions = PresetPositions.FindAll (s => s < currentPosition);

            if (availablePositions.Count > 0)
                nextPosition = availablePositions.Max();
            
            Debug.Log ("[IR Action] PrevPreset, currentPos = " + currentPosition + ", nextPosition=" + nextPosition);

            Translator.Move(nextPosition, customSpeed * speedTweak);
        }

        [KSPAction("Move To Next Preset")]
        public void MoveNextPresetAction(KSPActionParam param)
        {
            switch (param.type)
            {
                case KSPActionType.Activate:
                    MoveNextPreset ();
                    break;
                case KSPActionType.Deactivate:
                    Translator.Stop();
                    break;
            }
        }

        [KSPAction("Move To Previous Preset")]
        public void MovePrevPresetAction(KSPActionParam param)
        {
            switch (param.type)
            {
                case KSPActionType.Activate:
                    MovePrevPreset ();
                    break;
                case KSPActionType.Deactivate:
                    Translator.Stop();
                    break;
            }
        }


        [KSPAction("Move +")]
        public void MovePlusAction(KSPActionParam param)
        {
            switch (param.type)
            {
                case KSPActionType.Activate:
                    Translator.Move(float.PositiveInfinity, customSpeed * speedTweak);
                    break;
                case KSPActionType.Deactivate:
                    Translator.Stop ();
                    break;
            }
        }

        [KSPAction("Move -")]
        public void MoveMinusAction(KSPActionParam param)
        {
            switch (param.type)
            {
                case KSPActionType.Activate:
                    Translator.Move(float.NegativeInfinity, customSpeed * speedTweak);
                    break;
                case KSPActionType.Deactivate:
                    Translator.Stop ();
                    break;
            }
        }

        [KSPAction("Move Center")]
        public void MoveCenterAction(KSPActionParam param)
        {
            switch (param.type)
            {
                case KSPActionType.Activate:
                    Translator.Move(0f, customSpeed * speedTweak);
                    break;
                case KSPActionType.Deactivate:
                    Translator.Stop ();
                    break;
            }
        }

        public void LoadConfigXml()
        {
            PluginConfiguration config = PluginConfiguration.CreateForType<MuMechToggle>();
            config.load();
            UseElectricCharge = config.GetValue<bool>("useEC");
        }

        public void SaveConfigXml()
        {
            PluginConfiguration config = PluginConfiguration.CreateForType<ControlsGUI>();
            config.SetValue("useEC", UseElectricCharge);
            config.save();
        }

        public void MoveRight()
        {
            if (rotateJoint)
            {
                if ((rotationEuler != rotateMin && rotationEuler > minTweak) ||
                    rotationEuler != rotateMax && rotationEuler < maxTweak)
                {
                    //GetAxisInversion checks for IR.RotatronOffAxis
                    rotationEuler = rotationEuler - (1*GetAxisInversion());

                    if (part.name.Contains("IR.Rotatron.OffAxis"))
                    {   
                        rotation = rotationEuler/0.7070f;
                    }
                    else
                    {
                        rotation = Mathf.Clamp(rotationEuler, minTweak, maxTweak);
                    }
                }
                if (rotateLimits || limitTweakableFlag)
                {
                    if ((rotationEuler > rotateMin && rotationEuler > minTweak) &&
                            (rotationEuler < rotateMax && rotationEuler < maxTweak) || (rotationEuler == 0))
                    {
                            FixedMeshTransform.Rotate(rotateAxis*GetAxisInversion(), Space.Self);
                            transform.Rotate(-rotateAxis*GetAxisInversion(), Space.Self);
                    }

                    rotationEuler = Mathf.Clamp(rotationEuler, minTweak, maxTweak);

                }
                else
                {
                    FixedMeshTransform.Rotate(rotateAxis*GetAxisInversion(), Space.Self);
                    transform.Rotate(-rotateAxis*GetAxisInversion(), Space.Self);
                }
            }

            if (translateJoint)
                Translate(1);
        }

        public void MoveLeft()
        {
            if (rotateJoint)
            {
                if ((rotationEuler != rotateMax && rotationEuler < maxTweak) ||
                    (rotationEuler != rotateMin && rotationEuler > minTweak))
                {
                    rotationEuler = rotationEuler + (1*GetAxisInversion());

                    if (part.name.Contains("IR.Rotatron.OffAxis"))
                    {
                        rotation = rotationEuler/0.7070f;
                    }
                    else
                    {
                        rotation = Mathf.Clamp(rotationEuler, minTweak, maxTweak);
                    }
                }
                if (rotateLimits || limitTweakableFlag)
                {
                    if ((rotationEuler < rotateMax && rotationEuler < maxTweak) &&
                            (rotationEuler > rotateMin && rotationEuler > minTweak) || (rotationEuler == 0))
                    {
                        FixedMeshTransform.Rotate(-rotateAxis*GetAxisInversion(), Space.Self);
                        transform.Rotate(rotateAxis*GetAxisInversion(), Space.Self);
                    }

                    rotationEuler = Mathf.Clamp(rotationEuler, minTweak, maxTweak);

                }
                else
                {
                    FixedMeshTransform.Rotate(-rotateAxis*GetAxisInversion(), Space.Self);
                    transform.Rotate(rotateAxis*GetAxisInversion(), Space.Self);
                }
            }

            if (translateJoint)
                Translate(-1);
        }
        //resets servo to 0 rotation/translation
        //very early version do not use for now
        public void MoveCenter()
        {
            //no ideas yet on how to do it
        }

        private void Translate(float direction)
        {
            float gantryCorrection = part.name.Contains("Gantry") ? -1f : 1f;
            float deltaPos = direction * SPEED * Time.deltaTime * GetAxisInversion();

            float limitPlus  = maxTweak;         // translateMin/Max or min/maxTweak? 
            float limitMinus = minTweak;
            if (translation + deltaPos > limitPlus)
                deltaPos = limitPlus - translation;
            else if (translation + deltaPos < limitMinus)
                deltaPos = limitMinus - translation;

            translation += deltaPos;
            transform.Translate(-translateAxis * gantryCorrection*deltaPos);
            FixedMeshTransform.Translate(translateAxis * gantryCorrection*deltaPos);
        }

        private void OnGUI()
        {
            if (InputLockManager.IsLocked(ControlTypes.LINEAR))
                return;
            if (ControlWinPos2.x == 0 && ControlWinPos2.y == 0)
            {
                //controlWinPos = new Rect(Screen.width - 510, 70, 10, 10);
                ControlWinPos2 = new Rect(260, 66, 10, 10);
            }
            if (ResetWin)
            {
                ControlWinPos2 = new Rect(ControlWinPos2.x, ControlWinPos2.y,
                    10, 10);
                ResetWin = false;
            }
            GUI.skin = DefaultSkinProvider.DefaultSkin;
            

        }

        internal void PositionLock(Boolean apply)
        {
            //only do this lock in the editor - no point elsewhere
            if (HighLogic.LoadedSceneIsEditor && apply)
            {
                //only add a new lock if there isnt already one there
                if (InputLockManager.GetControlLock("PositionEditor") != ControlTypes.EDITOR_LOCK)
                {
#if DEBUG
                    Debug.Log(String.Format("[IR GUI] AddingLock-{0}", "PositionEditor"));
#endif
                    InputLockManager.SetControlLock(ControlTypes.EDITOR_LOCK, "PositionEditor");
                }
            }
                //Otherwise make sure the lock is removed
            else
            {
                //Only try and remove it if there was one there in the first place
                if (InputLockManager.GetControlLock("PositionEditor") == ControlTypes.EDITOR_LOCK)
                {
#if DEBUG
                    Debug.Log(String.Format("[IR GUI] Removing-{0}", "PositionEditor"));
#endif
                    InputLockManager.RemoveControlLock("PositionEditor");
                }
            }
        }

        protected class ElectricChargeConstraintData
        {
            public ElectricChargeConstraintData(double availableCharge, float requiredCharge, float groupRequiredCharge)
            {
                Available = availableCharge > 0.01d;
                Enough = Available && (availableCharge >= groupRequiredCharge*0.1);
                float groupRatio = availableCharge >= groupRequiredCharge
                    ? 1f
                    : (float) availableCharge/groupRequiredCharge;
                Ratio = Enough ? groupRatio : 0f;
                ToConsume = requiredCharge*groupRatio;
                RotationDone = false;
                TranslationDone = false;
            }

            public float Ratio { get; set; }
            public float ToConsume { get; set; }
            public bool Available { get; set; }
            public bool RotationDone { get; set; }
            public bool TranslationDone { get; set; }
            public bool Enough { get; set; }
        }
    }
}