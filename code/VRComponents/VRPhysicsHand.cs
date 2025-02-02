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
		}
		else
		{
			pdController.Enabled = true;
			pdController.Target = TrackingHand;
			pdController.Rigidbody = Rigidbody;
		}
	}
}
