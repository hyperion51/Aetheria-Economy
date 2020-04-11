

using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class LauncherData : IBehaviorData
{
    [InspectablePrefab, JsonProperty("missile"), Key(9)]  
    public string MissilePrefab;

    [InspectableAnimationCurve, JsonProperty("guidance"), Key(10)]  
    public float4[] GuidanceCurve;

    [InspectableAnimationCurve, JsonProperty("thrustCurve"), Key(11)]  
    public float4[] ThrustCurve;

    [InspectableAnimationCurve, JsonProperty("liftCurve"), Key(12)]  
    public float4[] LiftCurve;

    [InspectableField, JsonProperty("thrust"), Key(13)]  
    public PerformanceStat Thrust = new PerformanceStat();

    [InspectableField, JsonProperty("lockAngle"), Key(14)]  
    public float LockAngle;

    [InspectableField, JsonProperty("frequency"), Key(15)]  
    public float DodgeFrequency;

    [InspectableField, JsonProperty("launchSpeed"), Key(16)]  
    public PerformanceStat LaunchSpeed = new PerformanceStat();

    [InspectableField, JsonProperty("missileSpeed"), Key(17)]  
    public PerformanceStat MissileSpeed = new PerformanceStat();

    [InspectableField, JsonProperty("lockOnTime"), Key(18)]
    public PerformanceStat LockOnTime = new PerformanceStat();

    public IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new Launcher(context, this, entity, item);
    }
}

public class Launcher : IActivatedBehavior
{
    private bool _locking;
    private float _lockingTimer;
    private float _cooldown; // normalized
    private bool _locked;
    
    private LauncherData _launcher;
    public Entity Entity { get; }
    public Gear Item { get; }
    public GameContext Context { get; }
    private float _firingVisibility;

    public Launcher(GameContext context, LauncherData m, Entity entity, Gear item)
    {
        Context = context;
        _launcher  = m;
        Entity = entity;
        Item = item;
        // _audio = hp.Proxy.GetComponent<AudioSource>();
        // if(_audio==null)
        //     _audio = hp.Proxy.gameObject.AddComponent<AudioSource>();
        // _audio.clip = _launcher.BeepSound;
        // _audio.loop = true;
        // _audio.volume = .5f;
    }
    
    public void Activate()
    {
        _locking = true;
        _lockingTimer = 1;
    }

    private void Fire(long b)
    {
        
        // _firingVisibility += _launcher.Visibility.Evaluate(Hardpoint);
        // Hardpoint.Temperature += _launcher.Heat.Evaluate(Hardpoint) / Hardpoint.HeatCapacity;
        // var inst = GameObject.Instantiate(_launcher.MissilePrefab).transform;
        // var missile = inst.GetComponent<Bullet>();
        // missile.Target = Hardpoint.Ship.Ship.Target;
        // missile.Source = Hardpoint.Ship.Ship.Hitpoints;
        // missile.Lifetime = _launcher.Range.Evaluate(Hardpoint) / _launcher.MissileSpeed.Evaluate(Hardpoint);
        // missile.Damage = _launcher.Damage.Evaluate(Hardpoint);
        // inst.position = Hardpoint.Proxy.position;
        // inst.rotation = Hardpoint.Proxy.rotation;
        // inst.GetComponent<Rigidbody>().velocity = _launcher.LaunchSpeed.Evaluate(Hardpoint) * Hardpoint.Proxy.forward;
        // Physics.IgnoreCollision(Hardpoint.Ship.Ship.GetComponent<Collider>(), inst.GetComponent<Collider>());
        // var proj = inst.gameObject.AddComponent<GuidedProjectile>();
        // proj.GuidanceCurve = _launcher.GuidanceCurve;
        // proj.ThrustCurve = _launcher.ThrustCurve;
        // proj.LiftCurve = _launcher.LiftCurve;
        // proj.TopSpeed = _launcher.MissileSpeed.Evaluate(Hardpoint);
        // proj.Frequency = _launcher.DodgeFrequency;
        // proj.Target = Hardpoint.Ship.Ship.Target;
        // proj.Source = Hardpoint.Ship.Ship.Hitpoints;
        // proj.Thrust = _launcher.Thrust.Evaluate(Hardpoint);
        // _audio.PlayOneShot(_launcher.Sounds.RandomElement());
//        Debug.Log($"Firing bullet {b}");
    }

    public void Deactivate()
    {
        // if (_lockingTimer < 0)
        // {
        //     Fire(0);
        //     if(_launcher.BurstCount>1)
        //         Observable.Interval(TimeSpan.FromSeconds(_launcher.BurstTime.Evaluate(Hardpoint) / (_launcher.BurstCount-1))).Take(_launcher.BurstCount-1)
        //             .Subscribe(l => Fire(l+1));
        //     _lockingTimer = _cooldown = 1;
        //     _locking = _locked = false;
        //     _audio.Stop();
        // }
        // _locking = false;
        // _lockingTimer = 1;
    }

    public void Initialize()
    {
    }

    public void Update(float delta)
    {
        // if (Ship.Target == null)
        //     return;
        //
        // if (_locking && _cooldown < 0)
        // { 
        //     //if the crosshair stays under a certain LockOnBuffer distance for a certain amount of time (LockOnTime)
        //     if (acos(Vector2.Dot(Ship.Direction, (Ship.Target.transform.position-Ship.transform.position).normalized)) * Mathf.Rad2Deg < _launcher.LockOnDegrees)
        //     {
        //         var d = delta / _launcher.LockOnTime.Evaluate(Hardpoint);
        //         _lockingTimer -= d;
        //         if (!_locked && _lockingTimer < 0)
        //         {
        //             _locked = true;
        //             _audio.Play();
        //         } else if(Mathf.Repeat(_lockingTimer,1.0f/(_launcher.Beeps+1))-d<0)
        //             GameManager.Instance.InterfaceAudio.PlayOneShot(_launcher.BeepSound);
        //     }
        //     else
        //     {
        //         _lockingTimer = 1;
        //         _audio.Stop();
        //     }     
        // }
        // else
        // {
        //      _cooldown -= delta / _launcher.Cooldown.Evaluate(Hardpoint);
        // }
        //  
        //
        // if (_firingVisibility > 0.01f)
        // {
        //     Hardpoint.Ship.Visibility += _firingVisibility;
        //     _firingVisibility = 0;
        // }
    }
    
    public IBehaviorData Data => _launcher;
}