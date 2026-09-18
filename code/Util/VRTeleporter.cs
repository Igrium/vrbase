using System;
using System.Collections.Generic;

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

	/// <summary>
	/// A place the player could legally end up, and how they'd get there.
	/// </summary>
	private readonly record struct Candidate( Vector3 Pos, Vector3 From, bool IsLeap );

	[ConVar( "r_teleport_debug" )] public static bool DrawTeleportDebug { get; set; }

	[Property, Title( "Use Project Collision Rules" )]
	public bool UseCollisionRules { get; set; }

	[Property]
	[HideIf( nameof(UseCollisionRules), true )]
	public TagSet IgnoreLayers { get; set; } = new TagSet();

	/// <summary>
	/// The radius of the player for the sake of obstacle detection
	/// </summary>
	[Property, Category( "Player" )]
	public float Radius { get; set; } = 8f;

	/// <summary>
	/// The amount of available vertical space needed for the player to pass.
	/// </summary>
	[Property, Category( "Player" )] public float CrouchHeight = 32f;

	/// <summary>
	/// The amount of vertical space needed for the target position. Should be updated to match hmd height.
	/// </summary>
	[Property, Category( "Player" )] public float StandHeight = 73f;

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

	[Property, Category( "Movement" )] public float MaxDropHeight { get; set; } = 512;

	/// <summary>
	/// The maximum horizontal distance the player can travel in a leap.
	/// </summary>
	[Property, Category( "Movement" )]
	public float LeapDistance { get; set; } = 96f;

	[Property, Category( "Advanced" )] public float RaycastInterval { get; set; } = 16;

	[Property, Category( "Advanced" )] public float SidestepDistance { get; set; } = 64;

	/// <summary>
	/// How many heights to try for the top of a leap's arc. One means the leap can only cross at
	/// the height of its higher end; more lets it clear obstacles by rising up to MantleHeight.
	/// </summary>
	[Property, Category( "Advanced" )]
	public int LeapApexSamples { get; set; } = 3;

	public bool PruneLeapOrigins { get; set; } = true;

	public TeleportResult TryTeleport( in Vector3 targetPos )
	{
		return TryTeleport( WorldPosition, targetPos );
	}

	private const int MaxIterations = 4096;

	// Reused between calls so a held teleport doesn't allocate every frame.
	private readonly List<Vector3> _reachable = new();
	private readonly List<Candidate> _candidates = new();

	public TeleportResult TryTeleport( in Vector3 startPos, in Vector3 targetPos, float maxDist = 1024f )
	{
		Vector3 aim = targetPos;

		BBox bbox = BBox.FromHeightAndRadius( CrouchHeight, Radius );
		BBox standBbox = BBox.FromHeightAndRadius( StandHeight, Radius );
		float endError = RaycastInterval * 1.5f;

		_reachable.Clear();
		_candidates.Clear();

		// Walk/step/mantle.
		var marchCondition = MarchConnected( startPos, aim, maxDist, bbox, standBbox, _reachable, _candidates );

		bool hasBest = false;
		Candidate best = default;
		float bestScore = float.MaxValue;

		void Consider( in Candidate candidate )
		{
			float score = candidate.Pos.DistanceSquared( aim );
			if ( hasBest && score >= bestScore ) return;

			hasBest = true;
			best = candidate;
			bestScore = score;
		}

		// Score the connected destinations first: they're already computed, and a tight best score
		// makes the leap scan below cheap.
		foreach ( var candidate in _candidates )
			Consider( candidate );

		for ( int i = 0; i < _reachable.Count; i++ )
		{
			Vector3 origin = _reachable[i];
			if ( PruneLeapOrigins && hasBest && LeapLowerBoundScore( origin, aim ) >= bestScore )
				continue;

			int firstNew = _candidates.Count;
			CollectLeapLandings( origin, aim, bbox, standBbox, _candidates );
			for ( int k = firstNew; k < _candidates.Count; k++ )
				Consider( _candidates[k] );
		}

		Vector3 endPos = hasBest ? best.Pos : startPos;

		bool wasLeap = hasBest && best.IsLeap;

		EndCondition endCondition;
		if ( !hasBest )
			endCondition = EndCondition.Blocked;
		else if ( wasLeap || endPos.DistanceSquared( aim.WithZ( endPos.z ) ) < endError * endError )
			endCondition = EndCondition.Success;
		else
			endCondition = marchCondition;

		if ( DrawTeleportDebug )
		{
			if ( wasLeap )
				DebugOverlay.Line( best.From, best.Pos, Color.Cyan );

			DebugOverlay.Box( bbox + endPos, endCondition == EndCondition.Success ? Color.Green : Color.Red );
		}

		return new TeleportResult() { EndPos = endPos, EndCondition = endCondition };
	}

	/// <summary>
	/// March a hull toward the target with the connected moves (walk, step, mantle), recording every
	/// position it passes through.
	/// </summary>
	/// <returns>Why it stopped (used for reporting)</returns>
	private EndCondition MarchConnected( in Vector3 startPos, in Vector3 targetPos, float maxDist,
		in BBox bbox, in BBox standBbox, List<Vector3> reachable, List<Candidate> candidates )
	{
		Vector3 currentPos = startPos;
		Vector3 wishDir = (targetPos - startPos).WithZ( 0 ).Normal;

		Record( currentPos, currentPos, false, standBbox, reachable, candidates );

		if ( wishDir.LengthSquared < 0.5f )
			return EndCondition.Success;

		float bestDist = currentPos.Distance( targetPos.WithZ( currentPos.z ) );

		int i = 0;
		while ( true )
		{
			if ( i >= MaxIterations )
			{
				throw new InvalidOperationException( "Reached max tp iterations" );
			}

			if ( currentPos.DistanceSquared( in startPos ) > maxDist * maxDist )
				return EndCondition.MaxDist;

			var nextPos = currentPos + (wishDir * RaycastInterval);

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
				     && movePos.z - targetPos.z < 32 ) // Only go through with the mantle if we're aiming above it
				{
					didMantle = true;
					currentPos = movePos;
				}
				else
				{
					return cond;
				}
			}

			Record( currentPos, currentPos, false, standBbox, reachable, candidates );

			if ( DrawTeleportDebug )
			{
				DebugOverlay.Box( BBox.FromHeightAndRadius( 4f, Radius ) + currentPos,
					didMantle ? Color.Magenta : Color.White );
			}

			// Stop once we stop getting closer
			float dist = currentPos.Distance( targetPos.WithZ( currentPos.z ) );
			if ( dist > bestDist - 0.1f )
				return EndCondition.Success;

			bestDist = dist;
			i++;
		}
	}

	private void Record( in Vector3 pos, in Vector3 from, bool isLeap, in BBox standBbox,
		List<Vector3> reachable, List<Candidate> candidates )
	{
		reachable.Add( pos );

		// Somewhere we can pass through but not stand is a waypoint, not a destination.
		if ( HasStandClearance( pos, standBbox ) )
			candidates.Add( new Candidate( pos, from, isLeap ) );
	}

	/// <summary>
	/// Collect every spot the player could legally leap to from <paramref name="origin"/>.
	/// </summary>
	private void CollectLeapLandings( in Vector3 origin, in Vector3 targetPos, in BBox bbox, in BBox standBbox,
		List<Candidate> candidates )
	{
		Vector3 wishDir = (targetPos - origin).WithZ( 0 ).Normal;
		if ( wishDir.LengthSquared < 0.5f )
			return;

		for ( float dist = RaycastInterval; dist <= LeapDistance; dist += RaycastInterval )
		{
			Vector3 column = origin + wishDir * dist;

			foreach ( var trace in BuildTrace( column + Vector3.Up * MantleHeight,
				         column + Vector3.Down * MaxDropHeight, bbox ).RunAll() )
			{
				if ( !trace.Hit ) continue;

				Vector3 landing = column.WithZ( trace.HitPosition.z + 1 );

				if ( landing.z - origin.z > MantleHeight ) continue;
				if ( origin.z - landing.z > MaxDropHeight ) continue;
				if ( BuildTrace( landing, landing, bbox ).Run().StartedSolid ) continue;
				if ( !HasStandClearance( landing, standBbox ) ) continue;
				if ( !FlightPathClear( origin, landing, bbox ) ) continue;

				// No early return - every legal landing competes, and the winner is picked once.
				candidates.Add( new Candidate( landing, origin, true ) );
			}
		}
	}

	/// <summary>
	/// Check the flight of a leap: rise, cross, fall.
	/// </summary>
	private bool FlightPathClear( in Vector3 origin, in Vector3 landing, in BBox bbox )
	{
		float baseApex = MathF.Max( origin.z, landing.z );
		float maxApex = MathF.Max( origin.z + MantleHeight, baseApex );
		int samples = Math.Max( 1, LeapApexSamples );

		for ( int s = 0; s < samples; s++ )
		{
			float apexZ = samples == 1
				? baseApex
				: baseApex + (maxApex - baseApex) * (s / (float)(samples - 1));

			// Rise at the origin. If we can't get this high here, nothing higher will work either.
			if ( apexZ > origin.z && BuildTrace( origin, origin.WithZ( apexZ ), bbox ).Run().Hit )
				return false;

			if ( BuildTrace( origin.WithZ( apexZ ), landing.WithZ( apexZ ), bbox ).Run().Hit )
				continue;

			if ( apexZ > landing.z && BuildTrace( landing.WithZ( apexZ ), landing, bbox ).Run().Hit )
				continue;

			return true;
		}

		return false;
	}

	/// <summary>
	/// The best score any leap from <paramref name="origin"/> could possibly achieve - the distance
	/// from the aim to the nearest point of the region leaps can reach. A lower bound, so pruning on
	/// it can never discard a landing that would have won.
	/// </summary>
	private float LeapLowerBoundScore( in Vector3 origin, in Vector3 targetPos )
	{
		float horizontal = MathF.Max( 0f, (targetPos - origin).WithZ( 0 ).Length - LeapDistance );
		float nearestZ = Math.Clamp( targetPos.z, origin.z - MaxDropHeight, origin.z + MantleHeight );
		float vertical = targetPos.z - nearestZ;

		return horizontal * horizontal + vertical * vertical;
	}

	private EndCondition TryNormalMove( Vector3 currentPos, Vector3 nextPos, BBox bbox, out Vector3 outPos )
	{
		var dirTrace = BuildTrace( currentPos, nextPos, bbox ).Run();
		var dirTrace2 = BuildTrace( dirTrace.EndPosition, dirTrace.EndPosition + Vector3.Down * MaxDropHeight, bbox )
			.Run();

		var bbox2d = bbox;
		bbox2d.Maxs.z = bbox2d.Mins.z;

		var stepTrace1 = BuildTrace( currentPos, currentPos + Vector3.Up * StepHeight, bbox ).Run();
		var stepTrace2 = BuildTrace( stepTrace1.EndPosition, nextPos + Vector3.Up * StepHeight, bbox ).Run();
		var stepTrace3 =
			BuildTrace( stepTrace2.EndPosition, stepTrace2.EndPosition + Vector3.Down * MaxDropHeight, bbox ).Run();

		// Take the step trace or the direct trace based on which one got farther.
		float dirTraceDist = currentPos.DistanceSquared( dirTrace2.EndPosition.WithZ( currentPos.z ) );
		float stepTraceDist = currentPos.DistanceSquared( stepTrace3.EndPosition.WithZ( currentPos.z ) );

		if ( stepTraceDist > dirTraceDist )
		{
			outPos = stepTrace3.EndPosition;
			if ( !stepTrace3.Hit )
				return EndCondition.Fell;
			else return stepTrace2.Hit ? EndCondition.Blocked : EndCondition.Success;
		}
		else
		{
			outPos = dirTrace2.EndPosition;
			if ( !dirTrace2.Hit )
				return EndCondition.Fell;
			else return dirTrace.Hit ? EndCondition.Blocked : EndCondition.Success;
		}
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
		}

		return false;
	}

	private bool HasStandClearance( in Vector3 pos, in BBox standBbox )
	{
		return !BuildTrace( pos, pos, standBbox ).Run().StartedSolid;
	}

	private SceneTrace BuildTrace( in Vector3 from, in Vector3 to, in BBox? bbox = null )
	{
		SceneTrace trace = Scene.Trace.Ray( from, to ).IgnoreGameObjectHierarchy( this.GameObject );
		if ( bbox.HasValue )
			trace = trace.Size( bbox.Value );
		return UseCollisionRules ? trace.WithCollisionRules( Tags ) : trace.WithoutTags( IgnoreLayers );
	}
}
