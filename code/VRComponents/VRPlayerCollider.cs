namespace VRBase;

public sealed class VRPlayerCollider : Component
{
	[Property, RequireComponent]
	public CapsuleCollider? Collider { get; set; }

	[Property]
	public GameObject? HMD { get; set; }

	public Vector3 LastValidWorldPos { get; private set; }

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if ( Collider != null && HMD != null )
		{
			// Move the collider
			Vector3 targetPos = HMD.WorldPosition;
			Vector3 localTarget = HMD.LocalPosition;
			targetPos.z = Collider.WorldPosition.z;

			Collider.WorldPosition = targetPos;

			Collider.End = new Vector3( 0, 0, localTarget.z );

			if ( !Collider.Touching.Any() )
			{
				LastValidWorldPos = Collider.WorldPosition;
			}
			//Collider.g
		}

	}
}
