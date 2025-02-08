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
	private Logger log = new Logger("VRpickup: ");

	protected override void OnUpdate()
	{
		//a visualizer for the sphere, except this method crashes s&bbox every time...
		//DebugOverlay.Sphere(new Sphere(getGrabZoneCenter(), grabZonePickupRadius));

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

	public Vector3 getGrabZoneCenter()
	{
		return this.WorldPosition+getGrabZoneCenter();
	}

	public void onGrabPressed()
	{
		SceneTrace trace = Scene.Trace.Sphere(grabZonePickupRadius, grabZoneCenter, grabZoneCenter);
		foreach(SceneTraceResult result in trace.RunAll())
		{
			log.Info("AGGsGG: " + result.Body.GetGameObject().Name);
			Pickupable pickup = result.Body.GetGameObject().GetComponent<Pickupable>();
			if(pickup != null)
			{
				pickup.GameObject.AddComponent<PIDController>();
				break;
			}
		}
	}

	public void onGrabReleased()
	{

	}
}
