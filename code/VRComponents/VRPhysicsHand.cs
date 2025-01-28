

using Sandbox.Physics;
using FixedJoint = Sandbox.Physics.FixedJoint;

namespace VRBase;

public sealed class VRPhysicsHand : Component
{
	[Property]
	public GameObject? TrackingHand { get; private set; }

	[Property, RequireComponent]
	public Rigidbody? Rigidbody { get; private set; }

	public VRTrackedHand? TrackingHandComponent { get => TrackingHand?.GetComponent<VRTrackedHand>(); }

	private PhysicsJoint? joint;

	//protected override void OnUpdate()
	//{
	//	base.OnUpdate();
	//	if ( Rigidbody.IsValid() && TrackingHand.IsValid() )
	//	{
	//		Rigidbody.SmoothMove( TrackingHand.WorldTransform, 0, 0 );
	//	}
	//}

	//protected override void OnStart()
	//{
	//	base.OnStart();
	//	var trackingHand = TrackingHandComponent;
	//	if ( Rigidbody.IsValid() && trackingHand.IsValid() )
	//	{
	//		joint = PhysicsJoint.CreateFixed( new PhysicsPoint( trackingHand.PhysicsParent ), new PhysicsPoint( Rigidbody.PhysicsBody ) );
	//		Log.Info( joint.Point1.Body );
	//	}
	//}
	protected override void OnUpdate()
	{
		base.OnUpdate();
		if ( Rigidbody.IsValid() && TrackingHand.IsValid() )
		{
			Rigidbody.SmoothMove( TrackingHand.WorldTransform, .0001f, Time.Delta );
		}
	}
}
