using Sandbox.UI;

namespace VRBase;

/// <summary>
/// Uses makes a rigid body follow an object using a proportional-derivative controller.
/// </summary>
[Title("PD Controller")]
public class PDController : Component
{
	/// <summary>
	/// The gameobject to follow.
	/// </summary>
	[Property]
	public GameObject? Target { get; set; }

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
	public float PosKp { get; set; } = 4000f;

	[Property]
	public float PosKd { get; set; } = 400f;

	[Property]
	public float RotKp { get; set; } = 600000f;

	[Property]
	public float RotKd { get; set; } = 100000f;

	private Vector3 prevRotDifference = Vector3.Zero;
	private Vector3 prevPosDifference = Vector3.Zero;

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		PhysicsBody? body = PhysicsBody;
		GameObject? target = Target;

		if ( body.IsValid() && target.IsValid() )
		{
			Vector3 force = PositionPD( body.Position, target.WorldPosition );
			body.ApplyForce( force );

			Vector3 torque = RotationPD( body.Rotation, target.WorldRotation );
			body.ApplyTorque( torque );
		}
	}

	private Vector3 PositionPD( in Vector3 currentPos, in Vector3 targetPos )
	{
		Vector3 p = targetPos - currentPos;
		Vector3 d = (p - prevPosDifference) / Time.Delta;
		prevPosDifference = p;

		return p * PosKp + d * PosKd;
	}

	private Vector3 RotationPD( in Rotation currentRotation, in Rotation targetRotation )
	{
		Rotation rot = targetRotation * currentRotation.Inverse;
		Vector3 p = new Vector3( rot.x, rot.y, rot.z ) * rot.w * Time.Delta;
		Vector3 d = (p - prevRotDifference) / Time.Delta;
		prevRotDifference = p;
		return p * RotKp + d * RotKd;
	}
}
