using System;
using System.Diagnostics;
using Sandbox;

namespace VRBase;

public sealed class Vrpickup : Component
{
	[Property]
	public Boolean leftHand = true;

	[Property]
	public float gripAmount = 0.7f;

	private Boolean grabIsPressed = false;

	protected override void OnUpdate()
	{
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

	public void onGrabPressed()
	{
		
	}

	public void onGrabReleased()
	{

	}
}
