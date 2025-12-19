namespace VRBase.Util;

/// <summary>
/// Manages an (attempt) to teleport a player to a designated location, optionally drawing a teleport line.
/// </summary>
[Title( "VR Teleporter" )]
public class VRTeleporter : Component
{
	public record struct TeleportResult
	{
		public Vector3 EndPos;
		public bool Completed;
	}

	[ConVar( "r_teleport_debug" )]
	public static bool DrawTeleportDebug { get; set; }

	[Property, Title("Use Project Collision Rules")]
	public bool UseCollisionRules { get; set; }
	
	[Property]
	[HideIf( nameof( UseCollisionRules ), true )]
	public TagSet IgnoreLayers { get; set; } = new TagSet();

	/// <summary>
	/// The radius of the player for the sake of obstacle detection
	/// </summary>
	[Property, Category( "Player" )]
	public float Radius { get; set; } = 8f;

	/// <summary>
	/// The amount of available vertical space needed for the player to pass.
	/// </summary>
	[Property, Category( "Player" )]
	public float CrouchHeight = 32f;

	/// <summary>
	/// The amount of vertical space needed for the target position. Should be updated to match hmd height.
	/// </summary>
	[Property, Category( "Player" )]
	public float StandHeight = 73f;

	/// <summary>
	/// The vertical distance the player can climb up
	/// </summary>
	[Property, Category( "Movement" )]
	public float MantleHeight { get; set; } = 48f;

	[Property, Category( "Movement" )]
	public float MaxDropHeight { get; set; } = 512;

	/// <summary>
	/// The maximum horizontal distance the player can travel in a leap.
	/// </summary>
	[Property, Category( "Movement" )]
	public float LeapDistance { get; set; } = 96f;

	[Property, Category("Advanced")]
	public float RaycastInterval { get; set; } = 16;
	
	[Property, Category( "Advanced" )]
	public float SidestepDistance { get; set; } = 64;

	public TeleportResult TryTeleport( in Vector3 targetPos )
	{
		return TryTeleport( WorldPosition, targetPos );
	}
	
	public TeleportResult TryTeleport( in Vector3 startPos, in Vector3 targetPos, float maxDist = 1024f )
	{
		Vector3 currentPos = startPos;
		Vector3 wishDir = (targetPos - startPos).WithZ( 0 ).Normal;
		BBox traceBBox = new BBox( new Vector3( -Radius, -Radius, 0 ), new Vector3( Radius, Radius, 4 ) );
		
		while ( currentPos.Distance( in targetPos ) > RaycastInterval * 1.5 )
		{
			if ( currentPos.DistanceSquared( targetPos ) > maxDist * maxDist )
			{
				return new TeleportResult()
				{
					EndPos = currentPos, Completed = false
				};
			}
			
			Vector3 nextPos = currentPos + wishDir * RaycastInterval;
			var trace = BuildTrace( nextPos + Vector3.Up * 18f, nextPos + Vector3.Down * MaxDropHeight, traceBBox ).Run();

			bool success = !trace.StartedSolid && trace.Hit;

			if ( DrawTeleportDebug )
			{
				DebugOverlay.Box(traceBBox + trace.EndPosition, success ? Color.White : Color.Red, overlay:true);
			}
			
			if ( !success )
			{
				return new TeleportResult()
				{
					EndPos = currentPos, Completed = false
				};
			}
			currentPos = trace.EndPosition;
		}
		if ( DrawTeleportDebug )
		{
			DebugOverlay.Box(traceBBox + currentPos, Color.Green, overlay:true);
		}
		return new TeleportResult()
		{
			EndPos = currentPos, Completed = true
		};
	}

	private SceneTrace BuildTrace( in Vector3 from, in Vector3 to, in BBox bbox )
	{
		SceneTrace trace = Scene.Trace.Ray( from, to );
		trace.Size( in bbox ).IgnoreGameObjectHierarchy( this.GameObject );
		return UseCollisionRules ? trace.WithCollisionRules( Tags ) : trace.WithoutTags( IgnoreLayers );
	}
}
