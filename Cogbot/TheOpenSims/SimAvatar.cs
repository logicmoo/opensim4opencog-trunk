using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using cogbot.Listeners;
using cogbot.Utilities;
using OpenMetaverse;
using PathSystem3D.Navigation;
using Random=System.Random;
using UUID=OpenMetaverse.UUID;
using System.Drawing;

/// Complex outcomes may be a result of simple causes, or they may just be complex by nature. 
/// Those complexities that turn out to have simple causes can be simulated and studied, 
/// thus increasing our knowledge without needing direct observation.

namespace cogbot.TheOpenSims
{
    public partial class SimAvatarImpl : SimObjectImpl, SimMover, SimAvatar, SimActor
    {
        private Avatar.AvatarProperties _profileProperties;
        public Avatar.AvatarProperties ProfileProperties
        {
            get
            {
                if (_Prim0 != null)
                {
                    if (!string.IsNullOrEmpty(((Avatar)_Prim0).ProfileProperties.BornOn))
                    {
                        _profileProperties = ((Avatar)_Prim0).ProfileProperties;
                    }
                }
                return _profileProperties;
            }
            set
            {
                if (_Prim0 != null)
                {
                    if (string.IsNullOrEmpty(((Avatar)_Prim0).ProfileProperties.BornOn))
                    {
                        ((Avatar)_Prim0).ProfileProperties = value;
                    }
                }
                _profileProperties = value;
                AddInfoMap(value, "ProfileProperties");
            }
        }

        private Avatar.Interests _AvatarInterests;
        public Avatar.Interests AvatarInterests
        {
            get
            {
                if (_Prim0 != null)
                {
                    if (!string.IsNullOrEmpty(((Avatar)_Prim0).ProfileInterests.LanguagesText))
                    {
                        _AvatarInterests = ((Avatar)_Prim0).ProfileInterests;
                    }
                }
                return _AvatarInterests;
            }
            set
            {
                if (_Prim0 != null)
                {
                    if (string.IsNullOrEmpty(((Avatar)_Prim0).ProfileInterests.LanguagesText))
                    {
                        ((Avatar)_Prim0).ProfileInterests = value;
                    }
                }
                _AvatarInterests = value;
                AddInfoMap(value, "AvatarInterests");
            }
        }
        private List<UUID> _AvatarGroups;
        public List<UUID> AvatarGroups
        {
            get
            {
                if (_Prim0 != null)
                {
                    if (((Avatar)_Prim0).Groups!=null)
                    {
                        _AvatarGroups = ((Avatar)_Prim0).Groups;
                    }
                }
                return _AvatarGroups;
            }
            set
            {
                if (_Prim0 != null)
                {
                    if (((Avatar)_Prim0).Groups == null)
                    {
                        ((Avatar)_Prim0).Groups = value;
                    }
                }
                _AvatarGroups = value;
            }
        }
        public override bool IsKilled
        {
            ///  get { return WasKilled; }
            set
            {
                if (!WasKilled) /// already
                {
                    IEnumerable<SimObject> AttachedChildren0 = GetChildren();
                    lock (AttachedChildren0)
                        foreach (SimObject C in AttachedChildren0)
                        {
                            C.IsKilled = true;
                        }
                    ///  RemoveCollisions();
                }
                WasKilled = value;
            }
        }


        public override void ThreadJump()
        {
            ///  all the anims here are mainly so we can see what the bot is doing
            (new Thread(
                WithAnim(Animations.SHRUG,
                         WithAnim(Animations.WORRY, () =>
                                                        {
                                                            //Client.Self.Fly(false);
                                                            Thread.Sleep(10);
                                                            Client.Self.Jump(true);
                                                            Thread.Sleep(500);
                                                            Client.Self.Jump(false);
                                                        })))).Start();
        }

        public ThreadStart WithAnim(UUID uUID, ThreadStart threadStart)
        {
            return WithAnim(SimAssetStore.FindOrCreateAsset(uUID, AssetType.Animation), threadStart);
        }

        public override bool OpenNearbyClosedPassages()
        {
            bool changed = false;
            foreach (var s in GetNearByObjects(20, false))
            {
                if (s.UpdateOccupied()) changed = true;
            }
            if (!changed)
                foreach (var s in GetNearByObjects(40, false))
                {
                    if (s.UpdateOccupied()) changed = true;
                }
            //WithAnim(Animations.AFRAID,()=> base.OpenNearbyClosedPassages()).Invoke();
            return  base.OpenNearbyClosedPassages() || changed;
        }

        private SimMoverState old;
        protected override void OnMoverStateChange(SimMoverState obj)
        {
            UUID THINK_AMIN = Animations.EXPRESS_TOOTHSMILE;
            
            if (old==SimMoverState.THINKING)
            {
                Client.Self.AnimationStop(THINK_AMIN, true);
            }
            //Client.Self.AnimationStop(Animations.SHRUG, true);
            //Client.Self.AnimationStop(Animations.SURPRISE, true);
            switch (obj)
            {
                case SimMoverState.PAUSED:
                    break;
                case SimMoverState.MOVING:
                    break;
                case SimMoverState.BLOCKED:
                    break;
                case SimMoverState.COMPLETE:
                    break;
                case SimMoverState.TRYAGAIN:
                    break;
                case SimMoverState.THINKING:
                    Client.Self.AnimationStart(THINK_AMIN, true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("obj");
            }
            old = obj;
        }

        public float ZHeading
        {
            get
            {
                Vector3 v3 = Vector3.Transform(Vector3.UnitX, Matrix4.CreateFromQuaternion(GetSimRotation()));
                return (float)(Math.Atan2(-v3.X, -v3.Y) + Math.PI); // 2Pi= N, 1/2Pi = E
            }
        }

        public SimHeading GetHeading()
        {
            if (!IsRegionAttached() && !ReferenceEquals(_Prim0,null))
            {
                WorldSystem.ReSelectObject(_Prim0);
                Simulator sim = WorldSystem.GetSimulator(RegionHandle);
                EnsureParentRequested(sim);
            }
            return new SimHeading(this);
        }


        public override string DebugInfo()
        {

            string s = String.Format("{0} {1} {2}", GetName(), ID, GetHeading());
            lock (ActionEventQueue) foreach (SimObjectEvent s1 in ActionEventQueue)
            {
                s += "\n " + s1;
                
            }
            return s;
                
        }                    
        //public override bool OnEffect(string effectType, object t, object p, float duration, UUID id)
        //{
        //    bool noteable = LogEvent(new SimObjectEvent(effectType, SimEventType.EFFECT, SimEventStatus.Once, this, t, p, duration, id));
        //    //todo
        //    if (noteable) WorldSystem.SendNewEvent("on-effect", effectType, this, t, p, duration, id);
        //    return noteable;
        //    //throw new NotImplementedException();
        //}

        public override bool LogEvent(SimObjectEvent SE)
        {
            string typeUse = SE.Verb;
            object[] args1_N = SE.GetArgs();
            // does this backwards to the first argument is the most reliavant object
            for (int i = args1_N.Length-1; i >= 0 ; i--)
            {
                object o = args1_N[i];
                if (o == this) continue; //skip self
                if (o is SimObject) KnownSimObjects.Add((SimObject)o);
            }
            _knownTypeUsages.AddTo(SimTypeSystem.CreateTypeUsage(typeUse));
            bool noteable = base.LogEvent(SE);
            if (noteable)
                //if (theAvatar.Name.Contains("rael"))
                {
                    Console.WriteLine(SE);
                }
            return noteable;
        }

        public override void AddCanBeTargetOf(int argN, SimObjectEvent evt)
        {
            base.AddCanBeTargetOf(argN, evt);
        }

        public Avatar theAvatar
        {
            get { return (Avatar) Prim; }
        }

        //public SimAvatar InDialogWith { get; set; }


        private double _SightRange = 260.0;

        public double SightRange
        {
            get { return _SightRange; }
            set
            {
                if (value > _SightRange)
                {
                    GetNearByObjects(value, false);
                }
                _SightRange = value;
            }
        }

       
        /// <summary>
        /// things the bot cycles through mentally
        /// </summary>
        public readonly ListAsSet<SimObject> KnownSimObjects = new ListAsSet<SimObject>();

        /// <summary>
        /// Returns hopefully at least three objects sorted by distance
        /// </summary>
        /// <returns></returns>
        public List<SimObject> GetKnownObjects()
        {
            ScanNewObjects(3, SightRange, false);
            lock (KnownSimObjects) SortByDistance(KnownSimObjects);
            return KnownSimObjects;
        }

        public override List<SimObject> GetNearByObjects(double maxDistance, bool rootOnly)
        {
            List<SimObject> near = base.GetNearByObjects(maxDistance, rootOnly);
            AddKnowns(near);
            return near;
        }

        /// <summary>
        ///  Action template stubs 
        /// </summary>
        private readonly ListAsSet<SimTypeUsage> _knownTypeUsages;

        private BotAction _currentAction;
        private Thread actionThread = null;
        private readonly object actionLock = new object();
        public BotAction LastAction { get; set; }

        /// <summary>
        ///  Current action 
        /// </summary>       
        public BotAction CurrentAction
        {
            get
            {
                lock (actionLock)
                {
                    if (_currentAction == null) return null;
                    return _currentAction;
                }
            }
            set
            {
                try
                {
                    lock (actionLock)
                    {
                        if (Object.ReferenceEquals(_currentAction, value)) return;
                        if (_currentAction != null)
                        {
                            LastAction = _currentAction;
                            try
                            {
                                _currentAction = null;
                                LastAction.Abort();

                            }
                            catch (Exception)
                            {
                            }
                            _currentAction = value;
                        }
                        _currentAction = value;
                        if (actionThread != null)
                        {
                            try
                            {
                                actionThread.Abort();
                            }
                            catch (Exception)
                            {
                            }
                            finally
                            {
                                actionThread = null;
                            }
                        }
                        if (value != null)
                        {
                            actionThread = new Thread(() =>
                                                          {
                                                              try
                                                              {
                                                                  value.InvokeReal();
                                                              }
                                                              catch (Exception e)
                                                              {
                                                                 // Debug("InvokeReal: " + e);
                                                                  //  throw e;
                                                              }
                                                              finally
                                                              {
                                                                      //lock (actionLock)
                                                                      {
                                                                          if (_currentAction == value)
                                                                          {
                                                                              LastAction = value;
                                                                              _currentAction = null;
                                                                          }
                                                                      }
                                                              }
                                                          });
                            actionThread.Name = value.ToString();
                            actionThread.Start();
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("" + e);
                }
            }
        }

        public override sealed bool MakeEnterable(SimMover actor)
        {
            return false;
        }

        internal string AspectName;

        public SimAvatarImpl(UUID id, WorldObjects objectSystem, Simulator reg)
            : base(id, objectSystem, reg)
        {
            _knownTypeUsages = new ListAsSet<SimTypeUsage>();
            WorldObjects.SimAvatars.Add(this);
            ObjectType.SuperType.Add(SimTypeSystem.GetObjectType("Avatar"));
            //try
            //{
            //    AspectName = slAvatar.Name;
            //}
            //catch (Exception)
            //{
            //    AspectName += objectSystem.client + "_Avatar_" + slAvatar.LocalID;
            //}
            MakeEnterable(this);
        }

        public override bool RestoreEnterable(SimMover agent)
        {
            return false; ///  base.RestoreEnterable(this);
        }

        public override bool IsRoot
        {
            get { return theAvatar == null || theAvatar.ParentID == 0; }
        }

        ///  public override ISimObject Parent {  get { return this; }   }

        public bool IsSitting
        {
            get
            {
                if (IsControllable)
                {
                    if (Client.Self.SittingOn != 0) return true;
                    if (Client.Self.Movement.SitOnGround) return true;
                }
                Dictionary<UUID, int> anims = ExpectedCurrentAnims.Dictionary;
                if (SimAssetStore.Matches(anims.Keys,"sit").Count>0) return true;
                return theAvatar.ParentID != 0;
            }
            set
            {
                if (IsSitting == value) return;
                if (IsControllable)
                {
                    if (value)
                    {
                        SitOnGround();
                    }
                    else
                    {
                        StandUp();
                    }
                }
                Debug("Uncontroled IsStting=" + value);
            }
        }

        public override bool IsControllable
        {
            get
            {
                if (Client == null) return false;
                AgentManager ClientSelf = Client.Self;
                if (ReferenceEquals(null, _Prim0)) return false;
                return ClientSelf.AgentID == theAvatar.ID; //|| ClientSelf.LocalID == theAvatar.LocalID;
            }
        }


        public override Simulator GetSimulator()
        {
           // if (Client!=null && Client.Self.AgentID == Prim.ID) return Client.Network.CurrentSim;
            return GetSimRegion().TheSimulator;
        }

        public override Vector3 GetSimPosition()
        {
            //if (Client!=null && Client.Self.AgentID == Prim.ID)
            //{
            //    if (Client.Settings.OBJECT_TRACKING)
            //        return GetSimPosition();
            //}
            Primitive thisPrim = this.Prim;
            if (Object.ReferenceEquals(_Prim0, null) || thisPrim == null)
            {
                return LastKnownSimPos;
            }
            Vector3 thisPos = thisPrim.Position;
            while (thisPrim.ParentID != 0)
            {
                Primitive outerPrim = GetParentPrim(thisPrim);

                if (outerPrim == null)
                {
                    if (LastKnownSimPos != default(Vector3)) return LastKnownSimPos;
                    Debug("Unknown parent");
                    throw Error("GetSimRotation !IsRegionAttached: " + this);
                    return thisPrim.Position;
                }
                if (outerPrim == thisPrim || outerPrim == this.Prim)
                {
                    throw Error("GetSimPosition Loop: " + this);
                }
                thisPos = outerPrim.Position +
                          Vector3.Transform(thisPos, Matrix4.CreateFromQuaternion(outerPrim.Rotation));
                thisPrim = outerPrim;
            }
            if (BadLocation(thisPos))
            {
                Debug("-------------------------" + this + " shouldnt be at " + thisPos);
                //   WorldSystem.DeletePrim(thePrim);
            }
            return LastKnownSimPos = thisPos;
        }

        public override Vector3d GetWorldPosition()
        {
            /// if (Client.Self.AgentID == Prim.ID)
            /// {
            ///     if (Client.Settings.OBJECT_TRACKING)
            ///         return Client.Self.GlobalPosition;

            /// }
            return base.GetWorldPosition();
            //return GetSimRegion().LocalToGlobal(GetSimPosition());
        }

        public override SimRegion GetSimRegion()
        {
            return base.GetSimRegion();            
            //if (Prim == null)
            //{
            //    if (_CurrentRegion == null)
            //        if (IsControllable)
            //        {
            //            _CurrentRegion = SimRegion.GetRegion(Client.Network.CurrentSim);
            //        }
            //    return _CurrentRegion;
            //}

            //if (Prim.RegionHandle == 0)
            //{
            //    Console.WriteLine("Don't know the region for " + this);
            //}

            //if (_CurrentRegion == null)
            //{
            //    if (Prim.RegionHandle==0)
            //    {
            //        Console.WriteLine("Dont know the region for " + this);
            //    }
            //    _CurrentRegion = SimRegion.GetRegion(Prim.RegionHandle);
            //    Debug("out of date _CurrentRegion ");
            //}
            //if (theAvatar.RegionHandle != _CurrentRegion.RegionHandle)
            //{
            //    /// Debug("out of date RegionHandle ");
            //    /// _CurrentRegion = null;
            //}
            //return _CurrentRegion;
        }


        public override Quaternion GetSimRotation()
        {
            //if (IsControllable)
            //{
            //    if (Client.Settings.OBJECT_TRACKING)
            //        return Client.Self.SimRotation;
            //}
            /// lock (Prim)
            {
                //if (theAvatar.RegionHandle != _CurrentRegion.RegionHandle)
                //{
                //    Debug("out of date RegionHandle ");
                //}
                return base.GetSimRotation();
            }
        }
        /// public void AddGrass(Simulator simulator, Vector3 scale, Quaternion rotation, Vector3 position, Grass grassType, UUID groupOwner)
        /// {
        /// }
        /// public void AddPrim(Simulator simulator, Primitive.ConstructionData prim, UUID groupID, Vector3 position, Vector3 scale, Quaternion rotation)
        /// {
        /// }
        /// public void AddTree(Simulator simulator, Vector3 scale, Quaternion rotation, Vector3 position, Tree treeType, UUID groupOwner, bool newTree)
        /// {
        /// }
        /// public void AttachObject(Simulator simulator, uint localID, AttachmentPoint attachPoint, Quaternion rotation)
        /// {
        /// }

        /// public static Primitive.ConstructionData BuildBasicShape(PrimType type)
        /// {
        /// }

        /// public ISimObject RezObjectType(ISimObject copyOf)
        /// {
        ///     string treeName = args[0].Trim(new char[] { ' ' });
        ///     Tree tree = (Tree)Enum.Parse(typeof(Tree), treeName);

        ///     Vector3 treePosition = ClientSelf.SimPosition;
        ///     treePosition.Z += 3.0f;

        ///     Client.Objects.AddTree(Client.Network.CurrentSim, new Vector3(0.5f, 0.5f, 0.5f),
        ///         Quaternion.Identity, treePosition, tree, Client.GroupID, false);

        ///     /// ClientSelf.
        ///     return copyOf;
        /// }

        /// public void SortActs(List<SimUsage> acts)
        /// {
        ///     acts.Sort(CompareUsage);
        /// }

      
        public void Do(SimTypeUsage use, SimObject someObject)
        {
            CurrentAction = new BotObjectAction(this, new SimObjectUsage(use, someObject));
        }


        /// <summary>
        ///  
        /// </summary>
        /// <param name="minimum"></param>
        /// <param name="sightRange"></param>
        public void ScanNewObjects(int minimum, double sightRange, bool rootOnly)
        {
            List<SimObject> objects = GetNearByObjects(sightRange, rootOnly);
            ///  ill do this for us: AddKnowns(objects);
            if (KnownSimObjects.Count < minimum)
            {
                if (sightRange < 255)
                    ScanNewObjects(minimum, sightRange + 10, false);
            }
        }

        /// <summary>
        ///  
        /// </summary>
        /// <param name="objects"></param>
        private void AddKnowns(IEnumerable<SimObject> objects)
        {
            lock (objects)
            {
                foreach (SimObject obj in objects)
                {
                    if (obj != this)
                        if (obj.IsRoot || obj.IsTyped)
                        {
                            lock (KnownSimObjects)
                                if (!KnownSimObjects.Contains(obj))
                                {
                                    KnownSimObjects.Add(obj);
                                    IList<SimTypeUsage> uses = obj.GetTypeUsages();
                                    foreach (SimTypeUsage use in uses)
                                    {
                                        lock (_knownTypeUsages)
                                            if (!_knownTypeUsages.Contains(use))
                                            {
                                                _knownTypeUsages.Add(use);
                                            }
                                    }
                                }
                        }
                }
            }
        }

        /// <summary>
        ///  
        /// </summary>
        /// <param name="regionHandle"></param>
        public override void ResetRegion(ulong regionHandle)
        {
            bool changed = (regionHandle != RegionHandle);
            base.ResetRegion(regionHandle);
            if (changed)
            {
                lock (KnownSimObjects) KnownSimObjects.Clear();
                GetKnownObjects();
            }
        }


        /// <summary>
        ///   Avatars approach distance
        /// </summary>
        /// <returns></returns>
        public override float GetSizeDistance()
        {
            return 2f;
        }


        public BotClient GetGridClient()
        {
            /// if (Client != null) return Client;
            /// BotClient Client = WorldSystem.client;
            /// if (theAvatar.ID != ClientSelf.AgentID)
            /// {
            ///     throw new Exception("This avatar " + theAvatar + " has no GridClient");
            /// }
            return Client;
        }

        public void TalkTo(SimAvatar avatar, String talkAbout)
        {
            //SimAvatar avatarWasInDialogWith = ((SimAvatarImpl) avatar).InDialogWith;
            //SimAvatar wasInDialogWith = InDialogWith;
            //try
            {
                SimObject InDialogWith = avatar;
                BotClient Client = GetGridClient();
                AgentManager ClientSelf = Client.Self;
                AgentManager.AgentMovement ClientMovement = ClientSelf.Movement;
                TurnToward(InDialogWith);
                ClientSelf.AnimationStop(Animations.TALK, true);
                ClientSelf.AnimationStart(Animations.TALK, true);
                Client.Talk(InDialogWith + ": " + talkAbout);
                Thread.Sleep(3000);
                ClientSelf.AnimationStop(Animations.TALK, true);
            }
            //finally
            //{
            //    InDialogWith = wasInDialogWith;
            //    //avatar.InDialogWith = avatarWasInDialogWith;
            //}
        }

        public void TalkTo(SimAvatar avatar, BotMentalAspect talkAbout)
        {
            ///  TODO find a better text representation (a thought bubble maybe?)
            TalkTo(avatar, "" + talkAbout);
        }

        public override void Debug(string p, params object[] args)
        {
            if (Client != null)
            {
                string str = String.Format(p, args);
                Client.WorldSystem.WriteLine(str);
                if (Client.TheRadegastInstance!=null)
                {
                    Client.TheRadegastInstance.TabConsole.DisplayNotificationInChat(str);
                }
            }
            else
            {
                WorldSystem.WriteLine(String.Format(p, args));                
            }
        }

        public void Eat(SimObject target)
        {
            Debug("!!! EAT " + target);
        }

        public ThreadStart WithSitOn(SimObject obj, ThreadStart closure)
        {
            bool CanUseSit = WorldObjects.CanUseSit;
            return () =>
                       {
                           bool SattedUpon = false;
                           if (CanUseSit)
                           {
                              SattedUpon = SitOn(obj);
                           }

                           try
                           {
                               closure.Invoke();
                           }
                           finally
                           {
                               if (CanUseSit)
                               {
                                   if (SattedUpon) StandUp();
                               }
                           }
                       };
        }

        private void StopAllAnimations()
        {
            Dictionary<UUID, bool> animations = new Dictionary<UUID, bool>();
            foreach (UUID animation in GetCurrentAnims())
            {
                animations[animation] = false;
            }
            //foreach (UUID animation in AddedAnims.Keys)
            //{
            //    animations[animation] = false;
            //}
            //foreach (UUID animation in RemovedAnims.Keys)
            //{
            //    animations[animation] = false;
            //}
            Client.Self.Animate(animations, true);
        }

        public ThreadStart WithGrabAt(SimObject obj, ThreadStart closure)
        {
            BotClient Client = GetGridClient();
            return () =>
                       {
                           Primitive targetPrim = obj.Prim;
                           uint objectLocalID = targetPrim.LocalID;
                           AgentManager ClientSelf = Client.Self;
                           try
                           {
                               ClientSelf.Grab(objectLocalID);
                               closure.Invoke();
                           }
                           finally
                           {
                               ClientSelf.DeGrab(objectLocalID);
                           }
                       };
        }

        public ThreadStart WithAnim(SimAsset anim, ThreadStart closure)
        {
            BotClient Client = GetGridClient();
            AssetThread assetThread = new AssetThread(Client.Self, anim);
            return () =>
                       {
                           try
                           {
                               assetThread.Start();
                               closure.Invoke();
                           }
                           finally
                           {
                               assetThread.Stop();
                           }
                       };
        }

        public void ExecuteLisp(SimObjectUsage botObjectAction, Object lisp)
        {
            if (lisp==null) return;
            if (lisp is SimTypeUsage)
            {
                SimTypeUsage u = (SimTypeUsage) lisp;
                if (u.LispScript!=null)
                {
                    ExecuteLisp(botObjectAction, u.LispScript);
                }
                foreach (SimObjectType ot in botObjectAction.Target.ObjectType.SuperType)
                {
                    
                }
                return;
            }

            BotClient Client = GetGridClient();
            {
                Client.Intern("TheBot", this);
                Client.Intern("TheTarget", botObjectAction.Target);
                Client.Intern("TheAction", botObjectAction);
                Client.evalLispCode(lisp);
            }
        }


        public override bool IsFloating
        {
            get
            {
                if (IsControllable)
                    return Client.Self.Movement.Fly;
                return false;
            }
            set
            {
                if (IsFloating != value)
                {
                    if (IsControllable) Client.Self.Fly(value);
                }
            }
        }

        public override bool KilledPrim(Primitive prim, Simulator simulator)
        {
            //Debug("AVATAR KilledPrim " + prim);
            return base.KilledPrim(prim, simulator);
        }

        public override void ResetPrim(Primitive prim, BotClient bc, Simulator sim)
        {
            //if (!ReferenceEquals(_Prim0,prim))
            //{
            //    Debug("AVATAR ResetPrim " + prim);
            //}
            base.ResetPrim(prim, bc, sim);
        }

        public override void SetFirstPrim(Primitive prim)
        {
            if (prim is Avatar)
            {
                Avatar av = (Avatar)prim;
                if (!string.IsNullOrEmpty(av.Name))
                {
                    AspectName = av.Name;
                }
            }
            base.SetFirstPrim(prim);
          //  Debug("AVATAR SetFirstPrim " + prim);
        }
        public override string GetName()
        {
            if (!string.IsNullOrEmpty(AspectName)) return AspectName;
            if (Object.ReferenceEquals(_Prim0,null))
            {
                return null;
            }
            if (!string.IsNullOrEmpty(theAvatar.Name))
                return AspectName = theAvatar.Name;
            return _Prim0.ToString();
        }

        public override string ToString()
        {
            return GetName();
        }

        private BotClient Client;

        public void SetClient(BotClient Client)
        {
            lock (Client)
            {
                this.Client = Client;
                Client.Intern("TheBot", this);
                ///   WorldSystem = Client.WorldSystem;
                /// if (Client.Self.AgentID == Prim.ID)
                {
                    ///                    EnsureTrackerRunning();
                }
            }
            /// WorldSystem.AddTracking(this,Client);
        }

        public SimObject FindSimObject(SimObjectType pUse, double maxXYDistance, double maxZDist)
        {
            double myZ = GetWorldPosition().Z;
            IList<SimObject> objects = GetKnownObjects();
            lock (objects) foreach (SimObject O in objects)
            {
                if (O.Distance(this) > maxXYDistance) continue;
                if (Math.Abs(O.GetWorldPosition().Z - myZ) > maxZDist) continue;
                if (O.IsTypeOf(pUse) != null) return O;
            }
            return null;
        }

        public override bool Matches(string name)
        {
            String n = GetName();
            if (string.IsNullOrEmpty(n)) return false;
            return SimTypeSystem.MatchString(base.ToString(), name)
                   || SimTypeSystem.MatchString(ToString(), name);
        }

        public SimObject StandUp()
        {
            BotClient Client = GetGridClient();
            AgentManager ClientSelf = Client.Self;
            bool SAU = Client.Settings.SEND_AGENT_UPDATES;
            try
            {
                Client.Settings.SEND_AGENT_UPDATES = true;
                SimObject UnPhantom = null;
                AgentManager.AgentMovement ClientMovement = ClientSelf.Movement;
                uint sit = ClientSelf.SittingOn;
                if (sit != 0)
                {
                    Simulator simu = GetSimulator();
                    UnPhantom = WorldSystem.GetSimObject(WorldSystem.GetPrimitive(sit, simu), simu);
                    UnPhantom.MakeEnterable(this);
                }
                Client.Self.Crouch(false);
                ClientSelf.AnimationStart(Animations.STANDUP, true);
                ClientSelf.Stand();
                StopAllAnimations();
                return UnPhantom;
            }
            finally
            {
                Client.Settings.SEND_AGENT_UPDATES = SAU;
            }
        }


        public override void UpdateObject(ObjectUpdate objectUpdate, ObjectUpdate objectUpdateDiff)
        {
        }

        public override void Touch(SimObject simObject)
        {
            if (IsControllable)
            {
                Client.Self.Touch(simObject.Prim.LocalID);
            }
            else
            {
                Debug("Cant touch !Client.Self.AgentID == Prim.ID " + simObject);
            }
        }

        public void RemoveObject(SimObject O)
        {
            KnownSimObjects.Remove(O);
        }

        /// public override SimWaypoint GetWaypoint()
        /// {
        ///     Vector3 v3 = GetWorldPosition();
        ///     SimRegion PathStore = GetSimRegion();
        ///     SimWaypoint swp = PathStore.CreateClosestWaypoint(v3);
        ///     double dist = Vector3.Distance(v3, swp.GetWorldPosition());
        ///     if (!swp.Passable)
        ///     {
        ///         WorldSystem.WriteLine("CreateClosestWaypoint: " + v3 + " <- " + dist + " -> " + swp + " " + this);
        ///     }
        ///     return swp;
        /// }

        public override void StopMoving()
        {
            lock (TrackerLoopLock)
            {
                ApproachPosition = null;
                lastDistance = float.MaxValue;
                ApproachVector3D = Vector3d.Zero;
            }
            AgentManager ClientSelf = Client.Self;
            ClientSelf.AutoPilotCancel();
            AgentManager.AgentMovement ClientMovement = ClientSelf.Movement;
            ///   ClientMovement. AlwaysRun = false;
            ClientMovement.AtNeg = false;
            ClientMovement.AtPos = false;
            /// ClientMovement.AutoResetControls = true;
            ///    ClientMovement. Away = false;
            ClientMovement.FastAt = false;
            ClientMovement.FastLeft = false;
            ClientMovement.FastUp = false;
            ClientMovement.FinishAnim = true;
            ///   ClientMovement. Fly = false;
            ClientMovement.LButtonDown = false;
            ClientMovement.LButtonUp = false;
            ClientMovement.LeftNeg = false;
            ClientMovement.LeftPos = false;
            ClientMovement.MLButtonDown = false;
            ClientMovement.MLButtonUp = false;
            ///  ClientMovement. Mouselook = false;
            ClientMovement.NudgeAtNeg = false;
            ClientMovement.NudgeAtPos = false;
            ClientMovement.NudgeLeftNeg = false;
            ClientMovement.NudgeLeftPos = false;
            ClientMovement.NudgeUpNeg = false;
            ClientMovement.NudgeUpPos = false;
            ClientMovement.PitchNeg = false;
            ClientMovement.PitchPos = false;
            /// ClientMovement. SitOnGround = false;
            /// ClientMovement. StandUp = false;
            ClientMovement.Stop = true;
            ClientMovement.TurnLeft = false;
            ClientMovement.TurnRight = false;
            //ClientMovement.UpdateInterval = 0;
            ClientMovement.UpNeg = false;
            ClientMovement.UpPos = false;
            ClientMovement.YawNeg = false;
            ClientMovement.YawPos = false;

            ClientMovement.SendUpdate();
        }


        /// <summary>
        ///  
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="maxDistance"></param>
        /// <returns></returns>
        public double Approach(SimObject obj, double maxDistance)
        {
            BotClient Client = GetGridClient();
            ///  stand up first
            SimObject UnPhantom = StandUp();
            ///  make sure it not going somewhere
            ///  set the new target
            ApproachDistance = obj.GetSizeDistance() + 0.5f;
            string str = "Approaching " + obj + " " + DistanceVectorString(obj) + " to get " + ApproachDistance;
            Debug(str);
            obj.MakeEnterable(this);
            ///  if (!MoveTo(obj.GetWorldPosition(), obj.GetSizeDistance() + 0.5f, 12))
            {
                GotoTarget(obj);
                TurnToward(obj);
            }
            if (UnPhantom != null)
                UnPhantom.RestoreEnterable(this);

            return (double) Distance(obj);
        }

        private readonly object TrackerLoopLock = new object();

        private bool IsBlocked = false;
        private double lastDistance = float.MaxValue;

        private void TrackerLoop()
        {
            Random MyRandom = new Random(DateTime.Now.Millisecond);
            Boolean stopNext = false;
            //Client.Settings.DISABLE_AGENT_UPDATE_DUPLICATE_CHECK = false;
            //Client.Self.Movement.AutoResetControls = true;
            //Client.Self.Movement.UpdateInterval = 10;

            while (true)
            {
                Vector3d targetPosition = ApproachVector3D;
                lock (TrackerLoopLock)
                {
                    ///  Debug("TrackerLoop: " + Thread.CurrentThread);
                    if (ApproachVector3D == Vector3d.Zero)
                    {
                        if (ApproachPosition == null)
                        {
                            lastDistance = float.MaxValue;
                            Thread.Sleep(100);
                            continue;
                        }
                    }
                    if (ApproachPosition != null)
                    {
                        targetPosition = ApproachPosition.GetWorldPosition();
                    }
                }
                double realTargetZ = targetPosition.Z;
                Vector3d worldPosition = GetWorldPosition();
                /// ApproachDistance = ApproachPosition.GetSizeDistance();
                try
                {
                    AgentManager ClientSelf = Client.Self;
                    AgentManager.AgentMovement ClientMovement = ClientSelf.Movement;
                    //ClientMovement.AutoResetControls = false;
                    //ClientMovement.UpdateInterval = 0; /// 100
                    SimRegion R = GetSimRegion();
                    float WaterHeight = R.WaterHeight();
                    double selfZ = worldPosition.Z;
                    double UpDown = realTargetZ - selfZ;

                    double ZDist = Math.Abs(UpDown);
                    ///  Like water areas
                    bool swimming = WaterHeight > selfZ;

                    if (UpDown > 5f)
                    {
                        targetPosition.Z = realTargetZ; ///  selfZ + 0.2f; ///  incline upward
                        if (!ClientMovement.Fly)
                        {
                            Debug("Starting to fly");
                            ClientMovement.Fly = true;
                            SendUpdate(10);
                            ///   selfZ = realTargetZ;
                            TurnToward(targetPosition);
                            ClientMovement.AtPos = true;
                            SendUpdate(10);
                            ApproachVector3D = targetPosition;
///                             continue;
                        }
                    }
                    else
                    {
                        ClientMovement.Fly = false;
                        targetPosition.Z = selfZ;
                    }


                    ///  Reset previous Z 
                    ClientMovement.FastUp = false;
                    ClientMovement.UpPos = false;
                    ClientMovement.UpNeg = false;
                    ClientMovement.NudgeUpPos = false;
                    ClientMovement.NudgeUpNeg = false;

                    double curXYDist = Vector3d.Distance(worldPosition,
                                                         new Vector3d(targetPosition.X, targetPosition.Y, selfZ));

                    double curDist = Vector3d.Distance(worldPosition, targetPosition);
                    IsBlocked = false;
                    if (lastDistance <= curDist)
                    {
                        if (!ReferenceEquals(_Prim0,null) && Prim.Velocity == Vector3.Zero)
                            IsBlocked = true;
                        if (ApproachPosition!=null)
                        {
                            // follower should pass by.. but on way point navigation would be ok
                            ClientMovement.Stop = true;
                            ClientMovement.FinishAnim = true;
                            SendUpdate(100);
                            ClientMovement.Stop = false;
                            lastDistance = curDist;
                            continue;
                        }
                    }
                    lastDistance = curDist;


                    if (swimming)
                    {
                        ///  WaterHeight = WaterHeight - 1f;
                        /// if (!ClientMovement.Fly)
                        /// {
                        ///     ClientMovement.Fly = false;
                        /// }

                        bool nudgeUpDownMoves = true;

                        if (selfZ > WaterHeight - 0.5)
                        {
                            ///  Bob downward
                            if (nudgeUpDownMoves)
                                ClientMovement.NudgeUpNeg = true;
                            else
                                ClientMovement.UpNeg = true;
                            SendUpdate(10);
                            /// 
                            ///   continue; /// to keep going up
                        }
                        else
                        {
                            ///   nudge = !nudge;
                            if (selfZ < WaterHeight - 2.5)
                            {
                                ///  Bob upward
                                if (nudgeUpDownMoves)
                                    ClientMovement.NudgeUpPos = true;
                                else
                                    ClientMovement.UpPos = true;
                                SendUpdate(10);
                                ///    continue; /// to keep going up
                            }
                        }
                        targetPosition.Z = WaterHeight - 0.25f;
                    }

                    /// if ()/// ClientMovement.Fly = swimming;///  todo ||  GetPathStore().IsFlyZone(SimPathStore.GlobalToLocal(worldPosition));

                    if (swimming)
                    {
                        ClientMovement.Fly = true;
                        ///  Reset previous Z 
                        ClientMovement.FastUp = false;
                        ClientMovement.UpPos = false;
                        ClientMovement.UpNeg = false;
                        ClientMovement.NudgeUpPos = false;
                        ClientMovement.NudgeUpNeg = false;
                        SendUpdate(10);
                    }

                    /// //  Little Jumps
                    if (ZDist*2 > curDist)
                    {
                        if (!ClientMovement.Fly)
                        {
                            if (UpDown > 0)
                            {
                                ClientMovement.NudgeUpPos = true;
                                SendUpdate(10);
                                ClientMovement.NudgeUpPos = false;
                            }
                        }
                    }
                    /// else
                    /// {
                    ///     if (ClientMovement.Fly)
                    ///     {
                    ///         ClientMovement.NudgeUpPos = false;
                    ///         ///  ClientSelf.Fly(false);
                    ///         ClientMovement.Fly = false;
                    ///     }
                    /// }

                    ///  Far away
                    if (curXYDist > ApproachDistance)
                    {
                        ClientMovement.Stop = false;
                        ClientMovement.FinishAnim = false;
                        TurnToward(targetPosition);
                        ///  getting close though
                        if (curDist < (ApproachDistance*1.25))
                        {
                            /// ClientMovement.AtPos = true;
                            /// SendUpdate(125);
                            /// ClientMovement.Stop = true;
                            ClientMovement.AtPos = false;
                            ClientMovement.NudgeAtPos = true;
                            SendUpdate(100);
                            ClientMovement.NudgeAtPos = false;
                            SendUpdate(100);
                            TurnToward(targetPosition);
                            stopNext = true;
                            continue;
                        }
                        else
                        {
                            TurnToward(targetPosition);
                            ClientMovement.AtPos = true;
                           // ClientMovement.UpdateInterval = 0;
                            SendUpdate(MyRandom.Next(25, 100));
                            ///   TurnToward(targetPosition);
                            /// (int)(25 * (1 + (curDist / followDist)))
                            ///    MoveFast(ApproachPosition);
                            ///     if (ApproachPosition!=null) MoveSlow(ApproachPosition);
                            stopNext = true;
                            continue;
                        }
                    }
                    else
                    {
                        if (stopNext)
                        {
                            ///                             TurnToward(targetPosition);
                            ///  ClientMovement.ResetControlFlags();
                            ClientMovement.AtPos = false;
                            //ClientMovement.UpdateInterval = 0;
                            /// ClientMovement.StandUp = true;
                            /// ClientMovement.SendUpdate();
                            ClientMovement.FinishAnim = true;
                            ClientMovement.Stop = true;
                            SendUpdate(25);
                            ///  WorldSystem.TheSimAvatar.StopMoving();
                            stopNext = false;
                            continue;
                        }
                        else
                        {
                            Thread.Sleep(100);
                            continue;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug("" + e);
                }
            }
        }


        /// <summary>
        ///  
        /// </summary>
        /// <param name="finalTarget"></param>
        /// <param name="maxDistance"></param>
        /// <param name="maxSeconds"></param>
        /// <returns></returns>
        public override bool MoveTo(Vector3d finalTarget, double maxDistance, float maxSeconds)
        {
            TurnToward(finalTarget);
            int blockCount = 0;
            IsBlocked = false;
            double currentDist = Vector3d.Distance(finalTarget, GetWorldPosition());
            ///  if (currentDist < maxDistance) return true;
            lock (TrackerLoopLock)
            {
                ///   SimWaypoint P = SimWaypointImpl.CreateGlobal(finalTarget);
                SetMoveTarget(finalTarget);
                ApproachDistance = maxDistance;
            }
            bool IsKnownMoving = false;
            double lastDistance = currentDist;
            long endTick = Environment.TickCount + (int) (maxSeconds*1000);
            while (Environment.TickCount < endTick)
            {
                currentDist = Vector3d.Distance(finalTarget, GetWorldPosition());
                if (Prim==null)
                {
                    Debug("Where is my body? ");
                    return false;
                }
                if (!IsKnownMoving)
                {
                    if (Prim.Velocity != Vector3.Zero) IsKnownMoving = true;
                }
                else
                {
                    if (currentDist < maxDistance) return true;
                    if (Prim.Velocity == Vector3.Zero)
                    {
                        Console.Write("!");
                        if (IsBlocked)
                        {
                            blockCount++;
                            if (blockCount > 2)
                            {
                                StopMoving();
                                Debug("BLOCKED!");
                                return false;
                            }
                        }
                    }
                }
                if (currentDist > lastDistance)
                {
                    ///  Console.Write("=");
                    if (currentDist < maxDistance) return true;
                    StopMoving();
                    if (IsBlocked)
                    {
                        Console.Write("=");
                        return false;
                    }
                    return true;
                }
                if (currentDist > maxDistance)
                {
                    Thread.Sleep(40);
                    /// Application.DoEvents();
                    lastDistance = currentDist;
                    continue;
                }
                else
                {
                    Console.Write("+");
                    ///  StopMoving();
                    return true;
                }
            }
            StopMoving();
            Console.Write("-");
            return false;
        }

        public override void SendUpdate(int ms)
        {
            /// Client.Self.Movement.AutoResetControls = true;
            Client.Self.Movement.SendUpdate(true);
            if (ms > 0) Thread.Sleep(ms);
        }

        public override void TeleportTo(SimRegion R, Vector3 local)
        {
            if (!IsControllable)
            {
                throw Error("GotoTarget !Client.Self.AgentID == Prim.ID");
            }
            Client.ExecuteCommand("teleport " + R.RegionName + "/" + local.X + "/" + local.Y + "/" + local.Z);
          //  Client.Self.Teleport(R.RegionName, local);
        }

        public override void SetMoveTarget(SimPosition target, double maxDist)
        {
            if (target == null)
            {
                ApproachVector3D = Vector3d.Zero;
                ApproachPosition = null;
                lastDistance = float.MaxValue;
                return;
            }
            if (ApproachPosition != target)
            {
                lastDistance = float.MaxValue;
                SetMoveTarget(target.GetWorldPosition());
                ApproachDistance = maxDist;
            }
        }


        public void SetMoveTarget(Vector3d target)
        {
            lock (TrackerLoopLock)
            {
                if (target != ApproachVector3D)
                {
                    /// ApproachPosition = target;
                    ApproachVector3D = target;
                    if (target != Vector3d.Zero)
                    {
                        EnsureTrackerRunning();
                    }
                    else
                    {
                        StopMoving();
                    }
                }
            }
        }

        private void EnsureTrackerRunning()
        {
            lock (TrackerLoopLock)
            {
                if (ApproachThread == null)
                {
                    WorldSystem.SetSimAvatar(this);
                    ApproachThread = new Thread(TrackerLoop);
                    ApproachThread.Name = "TrackerLoop for " + Client;
                    ApproachThread.Priority = ThreadPriority.Normal;
                    ApproachThread.Start();
                }
            }
        }

        private double ApproachDistance = 2f;

        public SimPosition ApproachPosition { get; set; }
        public Vector3d ApproachVector3D { get; set; }

        /// <summary>
        ///  Action template stubs 
        /// </summary>
        public IEnumerable<SimTypeUsage> KnownTypeUsages
        {
            get { return _knownTypeUsages; }
        }

        public bool SitOn(SimObject someObject)
        {
            if (someObject == null) return SitOnGround();

            AgentManager ClientSelf = Client.Self;
            uint local = someObject.Prim.LocalID;

            //already sitting on 
            if (theAvatar.ParentID == local || Client.Self.SittingOn == local)
            {
                Debug("todo should we stand once first? " + someObject);
                return true;
            }

            AutoResetEvent are = new AutoResetEvent(false);
            ObjectManager.AvatarSitChanged OnSitChanged =
                (simulator, avatar, sittingon, oldseat) =>
                {
                    if (avatar == theAvatar)
                    {
                        are.Set();
                    }
                };
            Client.Objects.OnAvatarSitChanged += OnSitChanged;
            try
            {
                ClientSelf.RequestSit(someObject.Prim.ID, Vector3.Zero);
                ClientSelf.Sit();
                if (!are.WaitOne(10000))
                {
                   // return false;
                }
                return local == ClientSelf.SittingOn;
            }
            finally
            {
                Client.Objects.OnAvatarSitChanged -= OnSitChanged;
            }
        }

        public bool SitOnGround()
        {
            //Client.Self.Movement.AutoResetControls = false;
            //Client.Settings.DISABLE_AGENT_UPDATE_DUPLICATE_CHECK = true;
            //Client.Self.Movement.UpdateInterval = 0;

            AgentManager ClientSelf = Client.Self;
            AutoResetEvent are = new AutoResetEvent(false);
            ObjectManager.AvatarSitChanged OnSitChanged =
                (simulator, avatar, sittingon, oldseat) =>
                {
                    if (avatar == theAvatar)
                    {
                        are.Set();
                    }
                };
            AvatarManager.AvatarAnimationCallback OnAnimsChanged =
                (UUID avatarID, InternalDictionary<UUID, int> anims) =>
                    {
                        if (avatarID == theAvatar.ID)
                            if (SimAssetStore.IsSitAnim(anims.Dictionary.Keys))
                        {
                            //int seq = anims[Animations.SIT_GROUND];
                            are.Set();
                        }
                    };
            Client.Objects.OnAvatarSitChanged += OnSitChanged;
            Client.Avatars.OnAvatarAnimation += OnAnimsChanged;
            try
            {
                ClientSelf.SitOnGround();
                if (!are.WaitOne(10000))
                {
                    return false;
                }
                return 0 == ClientSelf.SittingOn;
            }
            finally
            {
                Client.Objects.OnAvatarSitChanged -= OnSitChanged;
                Client.Avatars.OnAvatarAnimation -= OnAnimsChanged;
            }
        }

        private Thread ApproachThread; /// = new Thread(TrackerLoop);

        public override bool SetObjectRotation(Quaternion localPos)
        {
            if (!IsRoot)
            {
                Quaternion start = GetSimRotation();
                Quaternion offset = localPos / start;
                SimObject p = Parent;
                return p.SetObjectRotation(p.GetSimRotation() * offset);
            }
            WorldSystem.SetObjectRotation(Prim, localPos);
            return true;
        }

        private static int InTurn = 0;
        public override bool TurnToward(Vector3 target)
        {
            bool prev = Client.Settings.DISABLE_AGENT_UPDATE_DUPLICATE_CHECK;
            int time = Client.Self.Movement.UpdateInterval;
            bool uen = Client.Self.Movement.AutoResetControls;
            if (InTurn>0)
            {
               //throw new InvalidActivityException("two TurnTowards?"); 
            }
            try
            {
                Client.Settings.SEND_AGENT_UPDATES = true;
                Client.Settings.DISABLE_AGENT_UPDATE_DUPLICATE_CHECK = false;
                Client.Self.Movement.UpdateInterval = 0;
                Client.Self.Movement.AutoResetControls = false;
                InTurn++;
                return TurnToward0(target);
            }
            finally
            {
                Client.Self.Movement.UpdateInterval = time;
                Client.Self.Movement.AutoResetControls = uen;
                Client.Settings.DISABLE_AGENT_UPDATE_DUPLICATE_CHECK = prev;
                InTurn--;

            }
        }

        public bool TurnToward0(Vector3 target)
        {
            //my philosophy is if the body and Head roation is facing hood ornimant.. the controls right/left are applicable .. is someone screws up and makes my avatar stand on it head backwards.. thats still fine i can still locate the hood ornimant.. i just have to hit the opposite directions left/right.. there is no guesswork.. just math.. sometimes whne i want my avatar to ratote left.. and hes in a car.. the only choice i have is the buttons.. the head roation is useless.. so basically libomv declares.. if your sitting on something.. dont expect stuff to work. what bothrs me is why does it do what i want and probably anyone else who wants a bot to dirve a prim wants.. .. whats all this trouble about sitting ona  prim and gettign world position
            if (!IsControllable)
            {
                Debug("Cannot COntrol TurnToward " + target);
                return false;
            }

            bool changed = false;
            AgentManager.AgentMovement ClientMovement = Client.Self.Movement;

            if (true)
            {
                bool prev = Client.Settings.DISABLE_AGENT_UPDATE_DUPLICATE_CHECK;
                try
                {
                    Client.Settings.DISABLE_AGENT_UPDATE_DUPLICATE_CHECK = true;
                    ClientMovement.AutoResetControls = false;
                    ClientMovement.TurnToward(target);
                    ClientMovement.SendUpdate(true,Client.Network.CurrentSim);
                    //ClientMovement.SendUpdate(true);
                }
                finally
                {
                    Client.Settings.DISABLE_AGENT_UPDATE_DUPLICATE_CHECK = prev;
                }
                return true;
            }

            Quaternion parentRot = Quaternion.Identity;

            Avatar Prim = theAvatar;
            if (Prim.ParentID != 0)
            {
                Primitive parent = null;
                if (_Parent!=null)
                {
                    parent = _Parent.Prim;
                }
                if (parent == null)
                {
                    parent = WorldSystem.GetPrimitive(Prim.ParentID, Client.Network.CurrentSim);
                }
                if (parent == null)
                {
                    Logger.Log("Attempted TurnToward but parent prim is not in dictionary", Helpers.LogLevel.Warning,
                               Client);
                    parent = WorldSystem.GetPrimitive(Prim.ParentID, Client.Network.CurrentSim);
                }
                if (parent == null) Debug("cant get parrent ");
                else
                    parentRot = parent.Rotation;
            }

            {
                int tries = 1;// int.MaxValue;
                Vector3 lp = GetSimPosition();
                bool needsTurn = true;
                while (needsTurn && tries-- > 0)
                {
//                    ClientMovement.ResetControlFlags();
                    double ZDir = ZHeading;
                    Vector3 dif = target - lp;
                    double Wanted = (Math.Atan2(-dif.X, -dif.Y) + Math.PI); // 2Pi= N, 1/2Pi = E
                    double lr = (ZDir - Wanted)*SimPathStore.RAD2DEG;
                    while (lr > 180) lr -= 360;
                    while (lr < -180) lr += 360;
                    if (lr < -20)
                    {
                        //ClientMovement.ResetControlFlags();
                        ClientMovement.TurnRight = true;
                        ClientMovement.YawNeg = true;
                        ClientMovement.SendUpdate(true);
                        Thread.Sleep(200);
                        ClientMovement.TurnRight = false;
                        ClientMovement.YawNeg = false;
                        ClientMovement.SendUpdate(true);
                        //ClientMovement.YawNeg = true;
                        //ClientMovement.SendUpdate(true);
                        //ClientMovement.TurnRight = true;
                       // Thread.Sleep(10);
                        double az = (ZHeading * SimPathStore.RAD2DEG + 30) / SimPathStore.RAD2DEG;
                        float xmul = (float)Math.Cos(az);
                        float ymul = (float)Math.Sin(az);
                        //target = new Vector3(lp.X + xmul, lp.Y - ymul, lp.Z);
                       // Debug("Need to turn " + lr + " for " + target);
                    }
                    else if ( lr > 20)
                    {
                        //ClientMovement.ResetControlFlags();
                        ClientMovement.YawPos = true;
                        ClientMovement.TurnLeft = true;
                        ClientMovement.SendUpdate(true);
                        Thread.Sleep(200);
                        ClientMovement.YawPos = false;
                        ClientMovement.TurnLeft = false;
                        ClientMovement.SendUpdate(true);
                        // ClientMovement.YawPos = true;
                       //// ClientMovement.SendUpdate(true);
                       // ClientMovement.TurnLeft = true;
                       //// Thread.Sleep(10);
                        double az = (ZHeading * SimPathStore.RAD2DEG - 30) / SimPathStore.RAD2DEG;
                        float xmul = (float)Math.Cos(az);
                        float ymul = (float)Math.Sin(az);
                       // target = new Vector3(lp.X + xmul, lp.Y - ymul, lp.Z);
                       // Debug("Need to turn " + lr + " for " + target);
                    } else 
                    {
                        needsTurn = false;
                    }
                    if (lr < -170 || lr > 170)
                    {
                        if(false && IsDrivingVehical)
                        {
                            bool atPos = ClientMovement.AtPos;
                            bool nudgeAtPos = ClientMovement.NudgeAtPos;

                            // hit reverse for a moment
                            ClientMovement.AtPos = false;
                            ClientMovement.NudgeAtPos = false;
                            ClientMovement.AtNeg = true;
                            ClientMovement.SendUpdate(true);
                            Thread.Sleep(200);
                            ClientMovement.AtNeg = false;
                            ClientMovement.SendUpdate(true);

                            ClientMovement.AtPos = atPos;
                            ClientMovement.NudgeAtPos = nudgeAtPos;
                        }

                    }
                }

                Quaternion between = Vector3.RotationBetween(Vector3.UnitX,
                                             Vector3.Normalize(target - GetSimPosition()));
                Quaternion rot = between * (Quaternion.Identity / parentRot);

                Quaternion br = ClientMovement.BodyRotation;
                Quaternion hr = ClientMovement.HeadRotation;

                changed = true;
                ClientMovement.BodyRotation = rot;
                ClientMovement.HeadRotation = rot;
                ClientMovement.Camera.LookAt(GetSimPosition(), target);

                bool prev = Client.Settings.DISABLE_AGENT_UPDATE_DUPLICATE_CHECK;
                try
                {
                    Client.Settings.DISABLE_AGENT_UPDATE_DUPLICATE_CHECK = true;
                    ClientMovement.TurnToward(target);
                    //ClientMovement.SendUpdate(true);
                }
                finally
                {
                    Client.Settings.DISABLE_AGENT_UPDATE_DUPLICATE_CHECK = prev;
                }
            }

            return changed;
        }

        protected bool IsDrivingVehical
        {
            get { return Prim.ParentID != 0; }        
        }

        public override bool UpdateOccupied()
        {
            ///  Vector3 pos = GetSimPosition();
            ///  if (SimPathStore.OutOfRegion(pos)) return;
            /// don't change this spot
            /// GetPathStore().SetPassable(pos.X, pos.Y, pos.Z);
            return false;
        }

        #region SimAvatar Members

        public bool IsWalking
        {
            get
            {
                return Overlaps(GetCurrentAnims(), SimAssetStore.MeaningUUIDs("LocomotionProcess-Animal"));
            }

        }
        public bool IsFlying
        {
            get
            {
                return Overlaps(GetCurrentAnims(), SimAssetStore.MeaningUUIDs("Flying"));
            }
        }

        public bool IsStanding
        {
            get
            {
                return Overlaps(GetCurrentAnims(), SimAssetStore.MeaningUUIDs("StandingStill"));
            }
        }

        public bool IsSleeping
        {
            get
            {
                return Overlaps(GetCurrentAnims(), SimAssetStore.MeaningUUIDs("Sleeping"));
            }
        }

        public ICollection<UUID> GetCurrentAnims()
        {
            return ExpectedCurrentAnims.Dictionary.Keys;
        }

        public IDictionary<UUID,int> GetCurrentAnimDict()
        {
            return ExpectedCurrentAnims.Dictionary;
        }

        private readonly InternalDictionary<UUID, int> ExpectedCurrentAnims = new InternalDictionary<UUID, int>();



        public static bool mergeEvents = true;
        private int AnimSequenceNumber = 0;
        /// public UUID CurrentAmin = UUID.Zero;
        /// <summary>
        ///  Nephrael Rae: [on-object-animation '(avatar "Candie Brooks") "TALK"][on-object-animation '(avatar "Candie Brooks") "STAND_1"][on-object-animation '(avatar "Candie Brooks") "e45fbdc9-af8f-9408-f742-fcb8c341d2c8"]
        /// </summary>
        /// <param name="anims"></param>
        public void OnAvatarAnimations(InternalDictionary<UUID, int> anims)
        {
            Dictionary<UUID, int> RemovedAnims = new Dictionary<UUID, int>();
            Dictionary<UUID, int> AddedAnims = new Dictionary<UUID, int>();
            bool SendAnimEvent = !WorldObjects.UseNewEventSystem;
            //if (!theAvatar.Name.Contains("rael")) return;
            lock (ExpectedCurrentAnims)
            {
                int mostCurrentSequence = int.MinValue;
                int leastCurrentSequence = int.MaxValue;
                GetSequenceNumbers(ExpectedCurrentAnims.Dictionary, ref leastCurrentSequence, ref mostCurrentSequence);

                ///  first time so find the lowest number
                int mostCurrentSequence1 = int.MinValue;
                int leastCurrentSequence1 = int.MaxValue;
                GetSequenceNumbers(anims.Dictionary, ref leastCurrentSequence1, ref mostCurrentSequence1);



                //UUID mostCurrentAnim;// = UUID.Zero;
                ///  List<String> names = new List<String>();
                Dictionary<UUID, int> RemovedThisEvent = new Dictionary<UUID, int>(ExpectedCurrentAnims.Dictionary);
                anims.ForEach(delegate(UUID key)
                                  {
                                      RemovedThisEvent.Remove(key);
                                      int newAnimNumber;
                                      anims.TryGetValue(key, out newAnimNumber);
                                      if (newAnimNumber >= mostCurrentSequence)
                                      {
                                          mostCurrentSequence = newAnimNumber;
                                          WorldObjects.GridMaster.RequestAsset(key, AssetType.Animation, true);
                                          //mostCurrentAnim = key;
                                      }
                                      if (ExpectedCurrentAnims.ContainsKey(key))
                                      {
                                          int oldAnimNumber;
                                          ExpectedCurrentAnims.TryGetValue(key, out oldAnimNumber);
                                          if (oldAnimNumber == newAnimNumber)
                                              ///  still the same
                                          {
                                              AddedAnims.Remove(key);
                                              RemovedAnims.Remove(key);
                                              return;
                                          }
                                          if (oldAnimNumber > newAnimNumber)
                                          {
                                            //  Debug("error oldAnimNumber > newAnimNumber");
                                          }
                                          ///      else
                                          {
                                              if (oldAnimNumber + 1 != newAnimNumber)
                                              {
                                                  RemovedAnims[key] = oldAnimNumber + 1;
                                                  AddedAnims[key] = newAnimNumber;
                                              }
                                          }
                                          return;
                                      }
                                      AddedAnims[key] = newAnimNumber; /// AddedAnims.Add(key, newAnimNumber);
                                      RemovedAnims.Remove(key);
                                      /// int whenSeq = newAnimNumber + 1;
                                      /// if (!RemovedAnimsWhen.ContainsKey(whenSeq))
                                      /// {
                                      ///     RemovedAnimsWhen[whenSeq] = new List<UUID>();
                                      /// }
                                      /// RemovedAnimsWhen[whenSeq].Add(key);
                                  });
                List<UUID> shownRemoved = new List<UUID>();
                List<UUID> showAdded = new List<UUID>();

                Dictionary<UUID, int> AddedThisEvent = new Dictionary<UUID, int>(AddedAnims);

                foreach (UUID key in RemovedThisEvent.Keys)
                {
                    ExpectedCurrentAnims.Dictionary.Remove(key);
                }

                foreach (UUID list in RemovedAnims.Keys)
                {
                    if (RemovedThisEvent.ContainsKey(list))
                    {
                        RemovedThisEvent.Remove(list);
                    }
                }


                List<SimObjectEvent> startStops = new List<SimObjectEvent>();

                //if (SimAnimationStore.IsSitAnim(RemovedThisEvent)) {
                //    if (!SimAnimationStore.IsSitAnim(AddedThisEvent))
                //    {
                //        LogEvent(new SimObjectEvent("StandUp", SimEventType.ANIM, this, ZHeading * SimPathStore.RAD2DEG, GetSimulator().Name, GetSimPosition()));
                //    }
                //}
                //start or stop moving
                Dictionary<UUID, int> RemovedThisEventUnsent = new Dictionary<UUID, int>(RemovedThisEvent);
                Dictionary<UUID, int> AddedThisEventUnsent = new Dictionary<UUID, int>(AddedThisEvent);
                if (mergeEvents)
                {
                    StartOrStopAnimEvent(RemovedThisEventUnsent, AddedThisEventUnsent, "Movement-TranslationProcess",
                                         startStops);

                    StartOrStopAnimEvent(RemovedThisEventUnsent, AddedThisEventUnsent, "Jumping", startStops);

                    StartOrStopAnimEvent(RemovedThisEventUnsent, AddedThisEventUnsent, "SittingDown", startStops);

                    StartOrStopAnimEvent(RemovedThisEventUnsent, AddedThisEventUnsent, "StandingStill", startStops);
                    // start or stop flying
                    StartOrStopAnimEvent(RemovedThisEventUnsent, AddedThisEventUnsent, "Flying", startStops);
                    //start or stop sleeping
                    StartOrStopAnimEvent(RemovedThisEventUnsent, AddedThisEventUnsent, "Lying-Physical", startStops);


                    //start or stop talking
                    //StartOrStopAnimEvent(RemovedThisEvent, AddedThisEvent, SimAnimationStore.IsCommunationAnim, "Commuincation", SimEventType.ANIM, startStops);

                    // StartOrStopAnimEvent(RemovedThisEvent, AddedThisEvent, "OtherAnim", startStops);

                    foreach (SimObjectEvent evt in startStops)
                    {
                        if (evt.Verb == "Flying")
                        {
                            LastEventByName[evt.EventName] = evt;
                            if (evt.EventStatus != SimEventStatus.Start) continue;
                        }
                        if (evt.Verb == "StandingStill")
                        {
                            LastEventByName[evt.EventName] = evt;
                            //continue;
                        }
                        if (evt.Verb == "SittingDown")
                        {
                            LastEventByName[evt.EventName] = evt;
                            //continue;
                        }
                        if (evt.EventName == "Movement-TranslationProcess-Start")
                        {
                            LastEventByName[evt.EventName] = evt;
                            //lastEvent = evt;
                            //continue;
                        }
                        //if (evt.EventName == "MovingStop")
                        //{
                        //    object old = GetLastEvent("MovingStart", 2);
                        //    evt.Verb = "MoveTo";
                        //}
                        if (SimEventStatus.Start == evt.EventStatus)
                        {
                            SetPosture(evt);
                        }
                        //  LogEvent(evt);
                    }
                }

                for (int seq = leastCurrentSequence; seq <= mostCurrentSequence; seq++)
                {
                    if (RemovedAnims.Count > 0)
                    {
                        foreach (KeyValuePair<UUID, int> uuid in RemovedAnims)
                        {
                            if (seq == uuid.Value)
                            {
                                if (RemovedThisEventUnsent.ContainsKey(uuid.Key))
                                {
                                    LogEvent(AnimEvent(uuid.Key, SimEventStatus.Stop, seq));
                                    shownRemoved.Add(uuid.Key);
                                }
                            }
                        }
                    }
                    if (AddedAnims.Count > 0)
                    {
                        foreach (KeyValuePair<UUID, int> uuid in AddedAnims)
                        {
                            if (seq == uuid.Value)
                            {
                                if (AddedThisEventUnsent.ContainsKey(uuid.Key))
                                {
                                    LogEvent(AnimEvent(uuid.Key, SimEventStatus.Start, seq));
                                    showAdded.Add(uuid.Key);
                                }
                            }
                        }
                    }
                }
                leastCurrentSequence = mostCurrentSequence;

                foreach (UUID key in shownRemoved)
                {
                    RemovedThisEvent.Remove(key);
                    RemovedAnims.Remove(key);
                }
                if (SimAssetStore.IsSitAnim(showAdded))
                {

                }
                foreach (UUID key in showAdded)
                {
                    AddedAnims.Remove(key);
                }
            }

            //ExpectedCurrentAnims.Dictionary.Clear();
            ExpectedCurrentAnims.Dictionary = anims.Dictionary;
            /// String newName = WorldSystem.GetAnimationName(mostCurrentAnim);
            /// {
            ///     if (oldAnim != mostCurrentAnim)
            ///     {
            ///         String oldName = WorldSystem.GetAnimationName(oldAnim);
            ///         if (oldName.Length > 4 && newName.Length > 4 &&
            ///             oldName.Substring(0, 5) == newName.Substring(0, 5))
            ///         {
            ///         }
            ///         else
            ///             WorldSystem.SendNewEvent("On-Object-Start-Animation", this, newName);
            ///     }
            /// }
            /// else
            /// {
            ///     WorldSystem.SendNewEvent("On-Object-Start-Animation", this, newName);
            /// }

            /// CurrentAmin = mostCurrentAnim;
            /// SendNewEvent("On-Avatar-Animation", avatar, names);
        }

        private SimObjectEvent AnimEvent(UUID uuid,SimEventStatus status, int serial)
        {
            SimAsset a = SimAssetStore.FindOrCreateAsset(uuid, AssetType.Animation);

            string headingString;
            switch (status)
            {
                case SimEventStatus.Start:
                    {
                        headingString = "eventOccursAt";
                        break;
                    }
                case SimEventStatus.Stop:
                    {
                        headingString = "toLocation";
                        break;
                    }
                default:
                    {
                        headingString = "eventOccursAt";
                        break;
                    }
            }
            object m = a.GetMeaning();
            SimObjectEvent oe = new SimObjectEvent(status, "OnAnim", SimEventType.ANIM, SimEventClass.REGIONAL,
                                      WorldObjects.ToParameter("doneBy", this),
                                      WorldObjects.ToParameter("isa", a),
                                      WorldObjects.ToParameter(headingString, GetHeading()));
            oe.serial = serial;
            if (m != null) oe.AddParam("isa", m);
            return oe;
        }

        private string PostureType;
        private SimObjectEvent LastPostureEvent;
        readonly private object postureLock  = new object();
        public Vector3 SimPosition
        {
            set
            {
                LastKnownSimPos = value;   
            }          
            get
            {
                return GetSimPosition();
            }
        }

        private void SetPosture(SimObjectEvent evt)
        {
            lock (postureLock)
            {
                if (PostureType != null)
                {
                    // was the same 
                    if (PostureType == evt.Verb) return;
                    SimObjectEvent ending = new SimObjectEvent(
                        SimEventStatus.Stop,
                        PostureType + (IsFlying ? "-Flying" : ""),
                        SimEventType.ANIM, SimEventClass.REGIONAL, evt.Parameters) { serial = LastPostureEvent.serial };
                    LogEvent(ending);
                    PostureType = evt.Verb;
                    SimObjectEvent starting = new SimObjectEvent(
                        SimEventStatus.Start,
                        PostureType + (IsFlying ? "-Flying" : ""),
                        SimEventType.ANIM, SimEventClass.REGIONAL, evt.Parameters) { serial = evt.serial };
                    LogEvent(starting);
                }
                PostureType = evt.Verb;
                LastPostureEvent = evt;
            }
        }

        private static void GetSequenceNumbers(IEnumerable<KeyValuePair<UUID, int>> anims, ref int leastCurrentSequence,ref int mostCurrentSequence)
        {
            lock (anims)
            {
                int mostCurrentSequence0 = mostCurrentSequence;
                int leastCurrentSequence0 = leastCurrentSequence;
                foreach (KeyValuePair<UUID, int> i in anims)
                {
                    int newAnimNumber = i.Value;
                    if (newAnimNumber < leastCurrentSequence0)
                    {
                        leastCurrentSequence0 = newAnimNumber;
                    }
                    if (newAnimNumber > mostCurrentSequence0)
                    {
                        mostCurrentSequence0 = newAnimNumber;
                    }
                    WorldObjects.GridMaster.RequestAsset(i.Key, AssetType.Animation, true);

                }
                leastCurrentSequence = leastCurrentSequence0;
                mostCurrentSequence = mostCurrentSequence0;
            }
        }

        //private delegate bool AnimationTest(ICollection<UUID> thisEvent);

        private void StartOrStopAnimEvent(IDictionary<UUID, int> RemovedThisEvent, IDictionary<UUID, int> AddedThisEvent, string name, IList<SimObjectEvent> startStops)
        {
            int wasStarted = 0;
            int wasStopped = 0;
            List<UUID> e = SimAssetStore.MeaningUUIDs(name);
            //  if (e.Count==0) throw new NoSuchElementException(name);
            foreach (UUID list in e)
            {
                if (AddedThisEvent.ContainsKey(list))
                {
                    wasStarted = AddedThisEvent[list];
                    AddedThisEvent.Remove(list);
                }
            }
            foreach (UUID list in e)
            {
                if (RemovedThisEvent.ContainsKey(list))
                {
                    wasStopped = RemovedThisEvent[list];
                    RemovedThisEvent.Remove(list);
                }
            }
            if (wasStarted != 0 && wasStopped != 0) return;
            if (wasStarted!=0)
            {
                SimObjectEvent simEvent = new SimObjectEvent(SimEventStatus.Start, name, SimEventType.ANIM, SimEventClass.REGIONAL,
                                                             WorldObjects.ToParameter("doneBy", this),
                                                             WorldObjects.ToParameter("eventOccursAt", GetHeading()));
                simEvent.serial = wasStarted;
                startStops.Add(simEvent);
            }
            if (wasStopped!=0)
            {
                SimObjectEvent simEvent = new SimObjectEvent(SimEventStatus.Stop, name, SimEventType.ANIM, SimEventClass.REGIONAL,
                                                             WorldObjects.ToParameter("doneBy", this),
                                                             WorldObjects.ToParameter("toLocation", GetHeading()));
                simEvent.serial = wasStopped;
                startStops.Insert(0, simEvent);
            }
        }

        static bool Overlaps(IEnumerable<UUID> c1, IEnumerable<UUID> c2)
        {
            foreach (var uuid in c2)
            {
                foreach (var uuid1 in c1)
                {
                    if (uuid == uuid1) return true;
                }
            }
            return false;
        }


        public object GetLastEvent(String name, int arg)
        {
            return LastEventByName.ContainsKey(name) ? LastEventByName[name].GetArg(arg) : null;
        }

        #endregion
    }


    public interface SimActor : SimAvatar, SimMover
    {
        new SimPosition ApproachPosition { get; set; }
        double Approach(SimObject obj, double maxDistance);
        cogbot.TheOpenSims.BotAction CurrentAction { get; set; }
        void Do(SimTypeUsage use, SimObject someObject);
        void Eat(SimObject target);
        void ExecuteLisp(SimObjectUsage botObjectAction, object lisp);
        SimRegion GetSimRegion();
        bool SitOnGround();
        SimObject StandUp();
        //void StopMoving();
        void TalkTo(SimAvatar avatar, BotMentalAspect talkAbout);
        void TalkTo(SimAvatar avatar, string talkAbout);
        ThreadStart WithAnim(SimAsset anim, ThreadStart closure);
        ThreadStart WithAnim(UUID animID, ThreadStart closure);
        ThreadStart WithGrabAt(SimObject obj, ThreadStart closure);
        ThreadStart WithSitOn(SimObject obj, ThreadStart closure);
        //ICollection<BotAction> GetPossibleActions(double maxXYDistance, double maxZDist);
//        List<BotAction> ScanNewPossibleActions(double maxXYDistance, double maxZDist);
        void SetClient(BotClient Client);
        //BotClient GetGridClient();
        new bool IsSitting { get; set; }
        BotAction LastAction { get; set; }
        //IEnumerable<SimTypeUsage> KnownTypeUsages { get; }
        bool SitOn(SimObject o);

        BotMentalAspect GetObject(string name);
    }


    public interface SimAvatar : SimObject, PathSystem3D.Navigation.SimMover
    {
        void RemoveObject(SimObject O);
        SimObject FindSimObject(SimObjectType pUse, double maxXYDistance, double maxZDist);
        /// <summary>
        /// Returns hopefully at least three objects sorted by distance
        /// </summary>
        /// <returns></returns>
        List<SimObject> GetKnownObjects();
        void ScanNewObjects(int minimum, double sightRange,bool rootOnly);
        double SightRange { get; set; }
        SimPosition ApproachPosition { get; }
        //BotNeeds CurrentNeeds { get; }
        Avatar theAvatar { get; }
        bool IsSitting { get; }
        float ZHeading { get; }
        IEnumerable<SimTypeUsage> KnownTypeUsages { get; }
        BotAction LastAction { get; }
        Avatar.AvatarProperties ProfileProperties { get; set; }
        Avatar.Interests AvatarInterests { get; set; }
        void OnAvatarAnimations(InternalDictionary<UUID, int> anims);

        ICollection<UUID> GetCurrentAnims();
        IDictionary<UUID, int> GetCurrentAnimDict();

        BotClient GetGridClient();
    }
}