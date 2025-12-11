using VRBase.Util;
namespace VRBase;

[Title("VR Physics Hand")]
public sealed class VRPhysicsHand : Component
{
	[Property]
	public GameObject? TrackingHand { get; set; }

	public Rigidbody Rigidbody => GetOrAddComponent<Rigidbody>();

	public VRPlayerController? Player => GameObject.GetComponentInParent<VRPlayerController?>();
	
	private bool _doPhysUpdate;
	
	protected override void OnUpdate()
	{
		base.OnUpdate();
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		var body = Rigidbody;
		var trackingHand = TrackingHand;
		if ( trackingHand == null )
			return;
		if ( Player.IsValid() && Player.IsMoving )
		{
			Rigidbody.WorldPosition = GetProjectedTransform();
			Rigidbody.WorldRotation = trackingHand.WorldRotation;
			body.Velocity = Vector3.Zero;
			body.AngularVelocity = Vector3.Zero;
		}
		else
		{
			body.Velocity = PController.GetPosVelocity( body.WorldPosition, trackingHand.WorldPosition, 3000 );
			body.AngularVelocity = PController.GetRotVelocity( body.WorldRotation, trackingHand.WorldRotation, 20 );
		}
	}

	private Vector3 GetProjectedTransform()
	{
		GameObject? HMD = Player?.HMD;
		if ( HMD == null || TrackingHand == null )
		{
			return WorldPosition;
		}

		Vector3 from = HMD.WorldPosition;
		from.z -= 12;
		Vector3 to = TrackingHand.WorldPosition;

		var res = Scene.Trace.Ray( from, to ).Radius( 3f ).IgnoreGameObjectHierarchy( GameObject.Parent ).Run();
		return res.EndPosition;
	}
}
