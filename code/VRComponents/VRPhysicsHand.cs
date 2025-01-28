

using Sandbox.Physics;
using FixedJoint = Sandbox.Physics.FixedJoint;

namespace VRBase;

public sealed class VRPhysicsHand : Component
{
	[Property]
	public GameObject? TrackingHand { get; private set; }

	[Property, RequireComponent]
	public Rigidbody? Rigidbody { get; private set; }

	[Property]
	public float posKp { get; set; } = 4000f;

	[Property]
	public float posKd { get; set; } = 400f;
	
	[Property]
	public float rotKp { get; set; } = 600000f;

	[Property]
	public float rotKd { get; set; } = 100000f;


	public float rotMagnitudeClamp = 100000f;

	[Property]
	public bool UseRealForce { get; set; } = true;

	private Vector3 prevRotDifference = Vector3.Zero;
	private Vector3 prevPosDifference = Vector3.Zero;

	public VRTrackedHand? TrackingHandComponent { get => TrackingHand?.GetComponent<VRTrackedHand>(); }


	//protected override void OnUpdate()
	//{
	//	base.OnUpdate();
	//	if ( Rigidbody.IsValid() && TrackingHand.IsValid() )
	//	{
	//		Rigidbody.SmoothMove( TrackingHand.WorldTransform, .0001f, Time.Delta );
	//	}

	//}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		if ( Rigidbody.IsValid() && TrackingHand.IsValid() )
		{
			Vector3 force = PositionPD( GameObject.WorldPosition, TrackingHand.WorldPosition );
			//force = force.ClampLength( 1000f );
			Rigidbody.PhysicsBody.ApplyForce( force );

			Vector3 torque = RotationPD( GameObject.WorldRotation, TrackingHand.WorldRotation );
			//torque.ClampLength( rotMagnitudeClamp );
			Rigidbody.PhysicsBody.ApplyTorque( torque );
		}
		
	}

	private Vector3 PositionPD(Vector3 currentPos, Vector3 targetPos)
	{
		Vector3 p = targetPos - currentPos;
		Vector3 d = (p - prevPosDifference) / Time.Delta;
		prevPosDifference = p;

		return p * posKp + d * posKd;
	}

	private Vector3 RotationPD( Rotation currentRotation, Rotation targetRotation )
	{
		Rotation rot = targetRotation * currentRotation.Inverse;
		Vector3 p = new Vector3( rot.x, rot.y, rot.z ) * rot.w * Time.Delta;
		Vector3 d = (p - prevRotDifference) / Time.Delta;
		prevRotDifference = p;
		return p * rotKp + d * rotKd;
	}
}
