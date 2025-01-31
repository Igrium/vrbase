namespace VRBase;

/// <summary>
/// Contains code needed in the VR Tracking hand.
/// </summary>
public sealed class VRTrackedHand : Component
{
	public PhysicsBody? PhysicsParent { get; private set; }

	protected override void OnAwake()
	{
		base.OnAwake();
		PhysicsParent = new PhysicsBody( Scene.PhysicsWorld );
		PhysicsParent.BodyType = PhysicsBodyType.Keyframed;
		PhysicsParent.SetComponentSource( this );
		PhysicsParent.UseController = true;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if ( PhysicsParent.IsValid() )
		{
			PhysicsParent.Transform = GameObject.Transform.World;
		}
	}
}
