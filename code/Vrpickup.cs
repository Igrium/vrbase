using System;
using System.Diagnostics;
using Sandbox;
using Sandbox.Diagnostics;

namespace VRBase;

public sealed class Vrpickup : Component
{
	[Property]
	public Boolean leftHand = true;

	[Property]
	public float gripAmount = 0.7f;

	[Property]
	private Vector3 grabZoneCenter;

	[Property]
	public float grabZonePickupRadius = 1;

	private Boolean grabIsPressed = false;

	private PickupData? heldObject;
	private Logger log = new Logger("VRpickup: ");

	protected override void OnUpdate()
	{
		//a visualizer for the sphere, except this method was broken by s&box and now crashes every time...
		//DebugOverlay.Sphere(new Sphere(getGrabZoneCenter(), grabZonePickupRadius));

		//if we are in VR, s&box currently does not have any way good to specify input for controllers except to hardcode it. That's what this is
		if(Game.IsRunningInVR)
		{
			float currentGripValue;
			if(leftHand)
			{
				currentGripValue = Input.VR.LeftHand.Grip.Value;
			}
			else
			{
				currentGripValue = Input.VR.RightHand.Grip.Value;
			}
			if(grabIsPressed)
			{
				if(currentGripValue < gripAmount)
				{
					grabIsPressed = false;
					onGrabReleased();
				}
			}
			else
			{
				if(currentGripValue >= gripAmount)
				{
					grabIsPressed = true;
					onGrabPressed();
				}
			}
		}
		//2D, debug purposes
		else
		{
			if( Input.Pressed("use") )
			{
				onGrabPressed();
			}
			if( Input.Released("use") )
			{
				onGrabReleased();
			}
		}
	}

	public PickupData? GetHeldObject()
	{
		return this.heldObject;
	}

	//updates our held object, making sure to drop our current held object
	private void dropAndSetHeldObject(PickupData? pickup)
	{
		//store it in a variable. This is done to make all the functions related to drop call eachother to have a less chaotic mess
		PickupData? oldHeldObject = heldObject;

		
		heldObject = pickup;
		if(oldHeldObject != null && oldHeldObject.Value.Object.IsValid())
		{
			oldHeldObject.Value.Object.drop(this);
		}
	}

	//get the center of our grab sphere in world cordinates
	public Vector3 getGrabZoneCenter()
	{
		return this.WorldTransform.PointToWorld(grabZoneCenter);
	}

	//makes this hand drops any held object
	public void dropHeldObject()
	{
		dropAndSetHeldObject(null);
	}

	//a function for when the "grab button" is pressed
	public void onGrabPressed()
	{
		//find all pickupable objects in our pickupsphere, and try to pick them up
		SceneTrace trace = Scene.Trace.Sphere(grabZonePickupRadius, getGrabZoneCenter(), getGrabZoneCenter());
		foreach(SceneTraceResult result in trace.RunAll())
		{
			Pickupable pickup = result.Body.GetGameObject().GetComponent<Pickupable>();
			if(pickup != null)
			{
				dropAndSetHeldObject(pickup.pickup(this));
				break;
			}
		}
	}

	//a function for when our "grab button" is released
	public void onGrabReleased()
	{
		dropHeldObject();
	}
}

//A simple data structure that holds our objects origin, and our objects rotation, relative to the hand that is holding us to be applied every PID calculation, and to be rendered to the visual hand for asthetics
public struct PickupData
{
	public Pickupable Object { get; }
	public Vector3 RelativePos { get; }
    public Rotation RelativeRot { get; }
    public PickupData(Pickupable pickupObject, Vector3 relativePos, Rotation relativeRot)
    {
		Object = pickupObject;
        RelativePos = relativePos;
        RelativeRot = relativeRot;
    }

    public override string ToString() => $"({Object.GameObject.Name}, {RelativePos}, {RelativeRot})";
}
