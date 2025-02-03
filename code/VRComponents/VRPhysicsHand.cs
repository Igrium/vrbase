namespace VRBase;

[Title("VR Physics Hand")]
public sealed class VRPhysicsHand : Component
{
	[Property]
	public GameObject? TrackingHand { get; set; }

	public Rigidbody Rigidbody => GetOrAddComponent<Rigidbody>();

	public VRPlayerController? Player => GameObject.GetComponentInParent<VRPlayerController?>();

	public PIDController PDController => GetOrAddComponent<PIDController>();

	protected override void OnUpdate()
	{
		base.OnUpdate();

		var player = Player;
		var pdController = PDController;

		if (player.IsValid() && player.IsMoving)
		{
			pdController.Enabled = false;
			if (TrackingHand.IsValid()) {
				Rigidbody.WorldPosition = GetProjectedTransform();
				Rigidbody.WorldRotation = TrackingHand.WorldRotation;
			}

			Rigidbody.Velocity = Vector3.Zero;
			Rigidbody.AngularVelocity = Vector3.Zero;
		}
		else
		{
			pdController.Enabled = true;
			pdController.Target = TrackingHand;
			pdController.Rigidbody = Rigidbody;
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
