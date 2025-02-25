﻿using Sandbox.UI;

namespace VRBase;

/// <summary>
/// Uses makes a rigid body follow an object using a proportional-derivative controller.
/// </summary>
[Title("PID Controller")]
public class PIDController : Component
{
	/// <summary>
	/// The gameobject to follow.
	/// </summary>
	[Property]
	public GameObject? Target { get; set; }

	//if the target is unset, then use this rotation and position instead
	public Vector3 targetPos {get; set;}
	public Rotation targetRot {get; set;}

	/// <summary>
	/// The rigid body that will follow it.
	/// </summary>
	[Property]
	public Rigidbody? Rigidbody { get; set; }

	/// <summary>
	/// The actual physics body created by the rigid body.
	/// </summary>
	public PhysicsBody? PhysicsBody => Rigidbody?.PhysicsBody;

	[Property]
	public float PosKp { get; set; } = 300;

	[Property]
	public float PosKi { get; set; } = 0;

	[Property]
	public float PosKd { get; set; } = 120;

	[Property]
	public float RotKp { get; set; } = 600;

	[Property]
	public float RotKd { get; set; } = 240;

	private Vector3 prevRotError = Vector3.Zero;
	private Vector3 posI = Vector3.Zero;
	private Vector3 prevPosError = Vector3.Zero;

	/// <summary>
	/// Reset the PID values of this controller. Call after a teleport.
	/// </summary>
	public virtual void ResetPID()
	{
		prevPosError = Vector3.Zero;
		posI = Vector3.Zero;
		prevPosError = Vector3.Zero;
	}

	protected override void OnEnabled()
	{
		base.OnEnabled();
		Reset();
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		PhysicsBody? body = PhysicsBody;
		GameObject? target = Target;

		Vector3 targetPos = this.targetPos;
		Rotation targetRot = this.targetRot;
		if(target.IsValid())
		{
			targetPos = target.WorldPosition;
			targetRot = target.WorldRotation;
		}

		if ( body.IsValid() )
		{
			Vector3 force = PositionPID( body.Position, targetPos );
			body.ApplyForce( force );

			Vector3 torque = RotationPD( body.Rotation, targetRot );
			body.ApplyTorque( torque );
		}
	}

	private Vector3 PositionPID( in Vector3 currentPos, in Vector3 targetPos )
	{
		Vector3 p = targetPos - currentPos;
		p = p.ClampLength(9);
		posI += p * Time.Delta;
		posI = posI.ClampLength( 1000 );
		Vector3 d = (p - prevPosError) / Time.Delta;
		prevPosError = p.ClampLength(5);

		return p * PosKp + posI * PosKi + d * PosKd;
	}

	private Vector3 RotationPD( in Rotation currentRotation, in Rotation targetRotation )
	{
		Rotation rot = targetRotation * currentRotation.Inverse;
		// = rot.Angles().AsVector3();
		Vector3 p = new Vector3( rot.x, rot.y, rot.z ) * rot.w;
		p.ClampLength(9);
		Vector3 d = (p - prevRotError) / Time.Delta;
		prevRotError = p.ClampLength(5);
		return p * RotKp + d * RotKd;
	}
}
