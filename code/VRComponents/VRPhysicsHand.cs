

using Sandbox.Physics;
using FixedJoint = Sandbox.Physics.FixedJoint;

namespace VRBase;

public sealed class VRPhysicsHand : Component
{
	[Property]
	public GameObject? TrackingHand { get; set; }

	[Property, RequireComponent]
	public Rigidbody? Rigidbody { get; set; }

	public VRCharacterController? Controller => GameObject.GetComponentInParent<VRCharacterController?>();

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
			VRCharacterController? controller = Controller;
			if (controller.IsValid() && controller.IsMoving)
			{
				Rigidbody.WorldPosition = GetProjectedTransform();
				Rigidbody.WorldRotation = TrackingHand.WorldRotation;

				Rigidbody.Velocity = Vector3.Zero;
				Rigidbody.AngularVelocity = Vector3.Zero;
			}
			else
			{
				Vector3 force = PositionPD( GameObject.WorldPosition, TrackingHand.WorldPosition );
				//force = force.ClampLength( 1000f );
				Rigidbody.PhysicsBody.ApplyForce( force );

				Vector3 torque = RotationPD( GameObject.WorldRotation, TrackingHand.WorldRotation );
				//torque.ClampLength( rotMagnitudeClamp );
				Rigidbody.PhysicsBody.ApplyTorque( torque );
			}

		}
	}

	private Vector3 GetProjectedTransform()
	{
		VRCharacterController? controller = Controller;
		GameObject? HMD = controller?.HMD;
		if ( controller == null || HMD == null || TrackingHand == null )
		{
			return WorldPosition;
		}

		Vector3 from = HMD.WorldPosition;
		Vector3 to = TrackingHand.WorldPosition;

		var res = Scene.Trace.Ray(from, to).Radius(3f).IgnoreGameObjectHierarchy(GameObject.Parent).Run();
		return res.EndPosition;
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
