using System;
using System.Diagnostics;
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
				// currentPos = targetPos.WithZ( currentPos.z );
				break;
			}

			if ( currentPos.DistanceSquared( in startPos ) > maxDist * maxDist )
			{
				endCondition = EndCondition.MaxDist;
				break;
			}

			var nextPos = currentPos + (wishDir * RaycastInterval);
			TryMantle( currentPos, nextPos, BBox.FromHeightAndRadius( 4f, Radius ), out _ );

			// Normal move with step
			var cond = TryNormalMove( currentPos, nextPos, bbox, out var movePos );
			bool didMantle = false;
			if ( cond == EndCondition.Success )
			{
				currentPos = movePos;
			}
			else
			{
				if ( TryMantle( currentPos, nextPos, bbox, out movePos ) 
					&& movePos.z - targetPos.z < 32 )// Only go through with the mantle if we're aiming above it
				{
					didMantle = true;
					currentPos = movePos;
				}
				else
				{
					endCondition = cond;
					break;
				}
			}

			if ( DrawTeleportDebug )
			{
				DebugOverlay.Box( BBox.FromHeightAndRadius( 4f, Radius ) + currentPos, didMantle ? Color.Magenta : Color.White );
			}
			i++;
		}

		if ( DrawTeleportDebug )
		{
			DebugOverlay.Box( bbox + currentPos, endCondition == EndCondition.Success ? Color.Green : Color.Red );
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

	private bool TryMantle( Vector3 currentPos, Vector3 targetPos, BBox bbox, out Vector3 outPos )
	{
		Vector3 targetCurrentZ = targetPos.WithZ( currentPos.z );
		outPos = currentPos;
		
		foreach ( var trace in BuildTrace( targetCurrentZ + Vector3.Up * MantleHeight, targetCurrentZ, bbox ).RunAll() )
		{
			// Make sure the trace is valid
			Vector3 hitOffset = trace.HitPosition + Vector3.Up * 1;
			var trace2 = BuildTrace( currentPos.WithZ( hitOffset.z ), hitOffset, bbox ).Run();
			if ( !trace2.Hit )
			{
				outPos = trace2.EndPosition;
				return true;
			}

			// DebugOverlay.Box( bbox + hitOffset, Color.Magenta );
			// var trace2 = BuildTrace( hitOffset, currentPos.WithZ( hitOffset.z ), bbox ).Run();
			// if (!trace2.StartedSolid)
			// DebugOverlay.Box( bbox + trace2.HitPosition, Color.Magenta );
			// DebugOverlay.Box( bbox + trace2.EndPosition, Color.Orange );
			// var trace2 = BuildTrace(trace.HitPosition + Vector3.Up * 1, currentPos.WithZ( trace.HitPosition.z + 1 ), bbox ).Run();
			// if (!trace2.StartedSolid)
			// 	DebugOverlay.Box(bbox + trace2.HitPosition, Color.Magenta );
		}
		return false;
	}

	private SceneTrace BuildTrace( in Vector3 from, in Vector3 to, in BBox? bbox = null )
	{
		SceneTrace trace = Scene.Trace.Ray( from, to ).IgnoreGameObjectHierarchy( this.GameObject );
		if (bbox.HasValue)
			trace = trace.Size( bbox.Value );
		return UseCollisionRules ? trace.WithCollisionRules( Tags ) : trace.WithoutTags( IgnoreLayers );
	}
}
