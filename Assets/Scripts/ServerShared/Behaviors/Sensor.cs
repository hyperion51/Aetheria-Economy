/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using MessagePack;
using Newtonsoft.Json;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn), RuntimeInspectable]
public class SensorData : BehaviorData
{
    [InspectableField, JsonProperty("sensitivity"), Key(3), RuntimeInspectable]  
    public PerformanceStat Sensitivity = new PerformanceStat();
    
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new Sensor(context, this, entity, item);
    }
}

public class Sensor : IBehavior
{
    private SensorData _data;

    private Entity Entity { get; }
    private EquippedItem Item { get; }
    private ItemManager Context { get; }

    public BehaviorData Data => _data;

    public Sensor(ItemManager context, SensorData data, Entity entity, EquippedItem item)
    {
        _data = data;
        Entity = entity;
        Item = item;
        Context = context;
    }

    public bool Execute(float delta)
    {
        // TODO: Handle Active Detection / Visibility From Reflected Radiance
        return true;
        // var ship = Hardpoint.Ship.Ship.transform;
        // var contacts =
        //     Physics.OverlapSphere(ship.position, _data.Range.Evaluate(Hardpoint)).Where(c=>c.attachedRigidbody?.GetComponent<Targetable>()!=null).Select(c=>c.attachedRigidbody.GetComponent<Targetable>());
        // foreach (var contact in contacts)
        // {
        //     var diff = (contact.transform.position - ship.position).Flatland();
        //     var angle = acos(dot(ship.forward.Flatland().normalized,
        //                     diff.normalized)) / (float)PI;
        //     var sens = _data.Sensitivity.Evaluate(Hardpoint) *
        //                Hardpoint.Ship.HullData.VisibilityCurve.Evaluate(saturate(angle));
        //     var vis = contact.Visibility / diff.sqrMagnitude;
        //     if(vis > 1/sens)
        //         Hardpoint.Ship.Contacts[contact] = Time.time;
        // }
        //
        // Hardpoint.Ship.VisibilitySources[this] = _data.Radiance.Evaluate(Hardpoint) / _data.RadianceMasking.Evaluate(Hardpoint);
    }
}