using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRBase;

/// <summary>
/// Manages VR player movement and collision.
/// </summary>
[Title( "VR Character Controller" )]
public class VRCharacterController : Component
{

	[ConVar("r_vr_debug_gismos")]
	public static bool VRDebugGismos { get; set; } = false;

	/// <summary>
	/// The gameobject being tracked to the player's HMD. Should be a direct child of this gameobject.
	/// </summary>
	[Property]
	public GameObject? HMD { get; set; }

	[Property]
	public float Radius { get; set; } = 16f;

	[Property]
	public float StepHeight { get; set; } = 18f;

	[Property]
	public float GroundAngle { get; set; } = 45f;

	/// <summary>
	/// The height of the HMD off the ground.
	/// </summary>
	public float Height => HMD != null ? HMD.LocalPosition.z : 0;

	/// <summary>
	/// The position of the player's feet relative to the roomspace root (aka this gameobject).
	/// </summary>
	public Vector3 LocalFeetPos => HMD != null ? HMD.LocalPosition.WithZ( 0 ) : Vector3.Zero;

	/// <summary>
	/// The position of the player's feet in world space.
	/// </summary>
	public Vector3 WorldFeetPos
	{
		get => this.WorldPosition + LocalFeetPos;
		set { WorldPosition = value - LocalFeetPos; }
	}

	/// <summary>
	/// The bounding box of the character of the correct size, based on 0,0,0.
	/// </summary>
	public BBox BoundingBox => new BBox( new Vector3( 0f - Radius, 0f - Radius, 0f ), new Vector3( Radius, Radius, Height ) );

	/// <summary>
	/// A bounding box with the bottom slightly raised to account for step height.
	/// </summary>
	public BBox StepBBox => new BBox( new Vector3( 0f - Radius, 0f - Radius, StepHeight ), new Vector3( Radius, Radius, Height ) );

	[Sync]
	public Vector3 Velocity { get; set; }

	[Property]
	[Title("Use Project Collision Rules")]
	public bool UseCollisionRules { get; set; }

	[Property]
	[HideIf( "UseCollisionRules", true )]
	public TagSet IgnoreLayers { get; set; } = new TagSet();

	/// <summary>
	/// The most-recent feet position that was valid.
	/// </summary>
	private Vector3 lastValidPosition;

	protected override void OnUpdate()
	{
		//if ( IsProxy ) return;

		Vector3 input = Input.AnalogMove;
		if ( input.IsNearlyZero() ) return;

		if ( ValidatePosition() )
		{
			Velocity = input * 100;
			Move( true );
		}

		var feetPos = this.WorldFeetPos;
		if ( IsPosValid( feetPos, true ) )
		{
			lastValidPosition = feetPos;
		}

		if ( VRDebugGismos )
		{
			DrawDebugGizmos();
		}

	}

	protected virtual void DrawDebugGizmos()
	{
		BBox box = this.BoundingBox;
		Transform transform = new Transform();
		transform.Position = WorldFeetPos;
		DebugOverlay.Box( box, transform: transform );

		if (transform.Position != lastValidPosition)
		{
			transform.Position = lastValidPosition;
			DebugOverlay.Box(box, Color.Red, transform: transform);
		}
	}

	/// <summary>
	/// Check if the current position of the player is valid according to the world's collisions.
	/// Can be invalid if the player roomscale-walked into a wall.
	/// </summary>
	/// <param name="allowStep">If set, raise the bottom of the box slightly to account for step height.</param>
	/// <returns><code>true</code> if the player is not currently inside a wall.</returns>
	public bool IsPosValid( bool allowStep = false )
	{
		return IsPosValid( WorldFeetPos, allowStep );
	}

	/// <summary>
	/// Check if a given position would be valid for the player to stand in, given the current bounding-box radius and height.
	/// </summary>
	/// <param name="feetPos">The world-space coordinates of the spot to check.</param>
	/// <param name="allowStep">If set, raise the bottom of the box slightly to account for step height.</param>
	/// <returns>If the player could stand here.</returns>
	public bool IsPosValid( in Vector3 feetPos, bool allowStep = false )
	{
		return !BuildTrace(feetPos, feetPos, allowStep).Run().StartedSolid;
	}

	private SceneTrace BuildTrace( in Vector3 from, in Vector3 to, bool allowStep = false )
	{
		return BuildTrace( base.Scene.Trace.Ray( in from, in to ), allowStep );
	}

	private SceneTrace BuildTrace( SceneTrace source, bool allowStep = false )
	{
		BBox hull = allowStep ? StepBBox : BoundingBox;
		SceneTrace sceneTrace = source.Size( in hull ).IgnoreGameObjectHierarchy( this.GameObject );
		if ( UseCollisionRules )
		{
			return sceneTrace.WithCollisionRules( this.Tags );
		}
		else
		{
			return sceneTrace.WithoutTags( IgnoreLayers );
		}
	}

	/// <summary>
	/// Called whenever the player starts fake moving to make sure they didn't move into a wall or something.
	/// If they did, TP them out.
	/// </summary>
	/// <returns>If we were able to move the player into a valid position.</returns>
	private bool ValidatePosition()
	{
		Vector3 worldPos = WorldFeetPos;
		if ( IsPosValid( worldPos ) )
		{
			return true;
		}
		else if ( ValidatePosWithStep( ref worldPos ) )
		{
			WorldFeetPos = worldPos;
			return true;
		}

		worldPos = lastValidPosition;
		if ( IsPosValid( worldPos ) || ValidatePosWithStep( ref worldPos ) )
		{
			WorldFeetPos = worldPos;
			return true;
		}
		else
		{
			return !TryUnstuck();
		}
	}

	private bool ValidatePosWithStep(ref Vector3 pos)
	{
		Vector3 startPos = pos.WithZ( pos.z + StepHeight );
		var trace = BuildTrace( startPos, pos ).Run();
		if (trace.StartedSolid)
		{
			return false;
		}
		else
		{
			pos = trace.EndPosition;
			return true;
		}
		//CharacterControllerHelper character = new CharacterControllerHelper(BuildTrace(pos, pos), pos, Vector3.Zero);
		//character.TraceMove( Vector3.Up * StepHeight );

	}

	private void Move(bool step)
	{
		if (Velocity.Length < 0.001f)
		{
			Velocity = Vector3.Zero;
			return;
		}

		Vector3 worldPos = WorldFeetPos;
		CharacterControllerHelper controller = new CharacterControllerHelper( BuildTrace( worldPos, worldPos ), worldPos, Velocity );
		controller.MaxStandableAngle = GroundAngle;
		if (step)
		{
			controller.TryMoveWithStep( Time.Delta, StepHeight );
		}
		else
		{
			controller.TryMove( Time.Delta );
		}

		this.WorldFeetPos = controller.Position;
		this.Velocity = controller.Velocity;
	}

	private int _stuckTries;
	
	// I copied this from CharacterController and I honestly have no idea how it works lol
	private bool TryUnstuck()
	{
		if (IsPosValid(WorldFeetPos))
		{
			_stuckTries = 0;
			return false;
		}

		int num = 20;
		for (int i = 0; i < num; i++)
		{
			Vector3 vec = WorldFeetPos + Vector3.Random.Normal * (_stuckTries / 2f);
			if (i == 0)
			{
				vec = WorldFeetPos + Vector3.Up * 2f;
			}

			if (IsPosValid(vec))
			{
				WorldFeetPos = vec;
				return false;
			}
		}
		_stuckTries++;
		return true;
	}
}
