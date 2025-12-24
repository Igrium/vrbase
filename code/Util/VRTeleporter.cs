using System;
namespace VRBase.Util;

/// <summary>
/// Manages an (attempt) to teleport a player to a designated location, optionally drawing a teleport line.
/// </summary>
[Title( "VR Teleporter" )]
public class VRTeleporter : Component
{
	public enum EndCondition
	{
		Success,
		Blocked,
		Fell,
		Edge,
		MaxDist
	}

	public record struct TeleportResult
	{
		public Vector3 EndPos;
		public EndCondition EndCondition;
	}

	[ConVar( "r_teleport_debug" )]
	public static bool DrawTeleportDebug { get; set; }

	[Property, Title( "Use Project Collision Rules" )]
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
	/// The height at which the player can step up
	/// Cheaper than mantling, but easier to abuse.
	/// </summary>
	[Property, Category( "Movement" )]
	public float StepHeight { get; set; } = 24f;

	/// <summary>
	/// The vertical distance the player can climb up
	/// More expensive than stepping but more checks to make sure the spot is accessible
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

	[Property, Category( "Advanced" )]
	public float RaycastInterval { get; set; } = 16;

	[Property, Category( "Advanced" )]
	public float SidestepDistance { get; set; } = 64;

	public TeleportResult TryTeleport( in Vector3 targetPos )
	{
		return TryTeleport( WorldPosition, targetPos );
	}

	private const float MaxIterations = 4096f;

	public TeleportResult TryTeleport( in Vector3 startPos, in Vector3 targetPos, float maxDist = 1024f )
	{
		Vector3 currentPos = startPos;
		Vector3 wishDir = (targetPos - startPos).WithZ( 0 ).Normal;

		BBox bbox = BBox.FromHeightAndRadius( CrouchHeight, Radius );

		EndCondition endCondition;
		float endError = RaycastInterval * 1.5f;

		int i = 0;
		while ( true )
		{
			if ( i >= MaxIterations )
			{
				throw new InvalidOperationException( "Reached max tp iterations" );
			}

			if ( currentPos.DistanceSquared( targetPos.WithZ( currentPos.z ) ) < endError * endError )
			{
				endCondition = EndCondition.Success;
				currentPos = targetPos.WithZ( currentPos.z );
				break;
			}

			if ( currentPos.DistanceSquared( in startPos ) > maxDist * maxDist )
			{
				endCondition = EndCondition.MaxDist;
				break;
			}

			// Normal move with step
			var cond = TryNormalMove( currentPos, currentPos + (wishDir * RaycastInterval), bbox, out currentPos );

			if ( cond != EndCondition.Success )
			{
				endCondition = cond;
				break;
			}

			if ( DrawTeleportDebug )
			{
				DebugOverlay.Box( BBox.FromHeightAndRadius( 4f, Radius ) + currentPos, Color.White );
			}
			i++;
		}

		if ( DrawTeleportDebug )
		{
			DebugOverlay.Box( bbox + currentPos, endCondition == EndCondition.Success ? Color.Green : Color.Red );
			Log.Info(endCondition);
		}
		return new TeleportResult()
		{
			EndPos = currentPos, EndCondition = endCondition
		};
	}

	private EndCondition TryNormalMove( Vector3 currentPos, Vector3 nextPos, BBox bbox, out Vector3 outPos )
	{
		var trace1 = BuildTrace( currentPos, currentPos + Vector3.Up * StepHeight, bbox ).Run();
		// Log.Info($"Hit: {trace1.GameObject}, this: {GameObject}");
		// DebugOverlay.Line( [trace1.StartPosition, trace1.EndPosition], Color.Orange );
		var trace2 = BuildTrace( trace1.EndPosition, nextPos + Vector3.Up * StepHeight, bbox ).Run();
		// DebugOverlay.Line( [trace2.StartPosition, trace2.EndPosition], Color.Orange );
		var trace3 = BuildTrace( trace2.EndPosition, trace2.EndPosition + Vector3.Down * MaxDropHeight, bbox ).Run();
		// DebugOverlay.Line( [trace3.StartPosition, trace3.EndPosition], Color.Orange );
		outPos = trace3.EndPosition;

		// If trace2 hit something, it means we weren't able to fully traverse to the next position.
		if ( !trace3.Hit )
		{
			return EndCondition.Fell;
		}
		return trace2.Hit ? EndCondition.Blocked : EndCondition.Success;
	}

	private SceneTrace BuildTrace( in Vector3 from, in Vector3 to, in BBox bbox )
	{
		SceneTrace trace = Scene.Trace.Ray( from, to );
		trace = trace.Size( in bbox ).IgnoreGameObjectHierarchy( this.GameObject );
		return UseCollisionRules ? trace.WithCollisionRules( Tags ) : trace.WithoutTags( IgnoreLayers );
	}
}
