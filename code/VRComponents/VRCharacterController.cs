using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace VRBase;

/// <summary>
/// Manages VR player movement and collision.
/// </summary>
[Title( "VR Character Controller" )]
public partial class VRCharacterController : Component
{

	[ConVar("r_vr_debug_gismos")]
	public static bool VRDebugGismos { get; set; } = false;

	/// <summary>
	/// The gameobject being tracked to the player's HMD. Should be a direct child of this gameobject.
	/// </summary>
	[Property]
	public GameObject? HMD { get; set; }

	/// <summary>
	/// The GameObject that joystick movement will be evaluated relative to. If null, use the HMD.
	/// </summary>
	[Property]
	[Category("Movement")]
	public GameObject? MovementRoot { get; set; }

	[Property]
	[Category("Movement")]
	public float Radius { get; set; } = 8f;

	[Property]
	[Category("Movement")]
	public float StepHeight { get; set; } = 18f;

	[Property]
	[Category("Movement")]
	public float GroundAngle { get; set; } = 45f;

	/// <summary>
	/// The amount of acceleration to apply while falling, in units per second.
	/// </summary>
	[Property]
	[Category("Movement")]
	public float FallAcceleration { get; set; } = 385.827f; // 9.8 m/s

	/// <summary>
	/// When falling, stop adding acceleration when we reach this velocity.
	/// -1 to disable terminal velocity.
	/// </summary>
	[Property]
	[Category("Movement")]
	public float TerminalVelocity { get; set; } = -1;


	/// <summary>
	/// The height of the HMD off the ground.
	/// </summary>
	public float Height => HMD != null ? HMD.LocalPosition.z + SuffocationRadius : 0;

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
	/// The position of the playere's eyes relative to the roomspace root (aka this gameobject).
	/// </summary>
	public Vector3 LocalEyePos => HMD != null ? HMD.LocalPosition : Vector3.Zero;

	/// <summary>
	/// The position of the playere's eyes in world space.
	/// </summary>
	public Vector3 WorldEyePos
	{
		get => this.WorldPosition + LocalEyePos;
		set { WorldPosition = value - LocalEyePos; }
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

	/// <summary>
	/// Whether the VR character fake-moved this frame.
	/// </summary>
	public bool IsMoving { get; private set; }

	/// <summary>
	/// Whether the character is currently falling using fake movement.
	/// Note: it's possible to not be on the ground yet not falling.
	/// </summary>
	public bool IsFalling { get; private set; }

	public GameObject? GroundObject { get; private set; }
	public Collider? GroundCollider { get; private set; }

	[Title("Use Project Collision Rules")]
	[Category("Movement")]
	public bool UseCollisionRules { get; set; }

	[Property]
	[HideIf( "UseCollisionRules", true )]
	[Category( "Movement" )]
	public TagSet IgnoreLayers { get; set; } = new TagSet();


	/// <summary>
	/// The most-recent feet position that was valid.
	/// </summary>
	private Vector3 lastValidPosition;

	protected virtual void TickMovement()
	{
		IsMoving = false;

		//Vector3 input = new Vector3( Input.VR.LeftHand.Joystick.Value, 0 );
		Vector3 input = Input.AnalogMove;

		// WALKING
		if ( input.Length >= .1 )
		{
			Rotation joystickRot = WorldRotation;
			if ( MovementRoot != null )
			{
				joystickRot = MovementRoot.WorldRotation;
			}
			else if ( HMD != null )
			{
				joystickRot = HMD.WorldRotation;
			}

			Vector3 rotInput = (new Vector3( input.y, -input.x, 0 ) * joystickRot).WithZ( 0 ).Normal;
			rotInput *= input.Length;

			if ( ValidatePosition() )
			{
				StopSuffocating();
				Velocity = (rotInput * 100).WithZ( Velocity.z );
				Move( true );
				IsMoving = true;
			}

		}

		// FALLING
		UpdateGroundObject();
		if ( GroundObject != null )
		{
			IsFalling = false;
			Velocity = Velocity.WithZ( 0f );
		}

		// Only start falling if we're allowed to fake move
		if ( input.Length >= .1 && GroundObject == null && FallAcceleration > 0 )
		{
			IsFalling = true;
		}

		if ( IsFalling )
		{
			if ( TerminalVelocity <= 0 || MathF.Abs( Velocity.z ) < TerminalVelocity )
			{
				Velocity = Velocity.WithZ( Velocity.z - MathF.Abs( FallAcceleration ) * Time.Delta );
			}

			// Make sure we have room to fall.
			Vector3 pos = WorldFeetPos;
			float fallAmount = Velocity.z * Time.Delta;
			SceneTraceResult trace = BuildTrace( pos, pos - Vector3.Down * fallAmount ).Run();

			WorldFeetPos = trace.EndPosition;

			IsMoving = true;
		}

		// POS VALIATION
		var feetPos = this.WorldFeetPos;
		if ( IsPosValid( feetPos, true ) )
		{
			lastValidPosition = feetPos;
		}
	}


	protected override void OnUpdate()
	{
		
		if (!IsProxy)
		{
			TickMovement();
			TickSuffocation();
		}

		if ( VRDebugGismos )
		{
			DrawDebugGizmos();
		}

	}

	protected virtual void UpdateGroundObject()
	{
		Vector3 from = this.WorldFeetPos;
		Vector3 to = from + Vector3.Down * 2f;

		SceneTraceResult trace = BuildTrace( from, to ).Run();
		if (trace.Hit)
		{
			GroundObject = trace.GameObject;
			GroundCollider = trace.Shape?.Collider as Collider;
		}
		else
		{
			GroundObject = null;
			GroundCollider = null;
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

		GameObject? hmd = HMD;
		if (hmd.IsValid())
		{
			Sphere suffocation = new Sphere( hmd.WorldPosition, SuffocationRadius );
			DebugOverlay.Sphere( suffocation, Color.Green );
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

	private SceneTrace BuildTrace( in SceneTrace source, bool allowStep = false )
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
	/// Check if the player is allowed to teleport somewhere based on if there's a valid route, etc.
	/// Note: this doesn't check if the target position is actually valid; just if we can get there.
	/// </summary>
	/// <param name="target">Position to check.</param>
	/// <param name="from">The position we're teleporting from. If absent, use the player's current position.</param>
	/// <returns>If we can teleport there.</returns>
	public virtual bool CanReach( in Vector3 target, in Vector3? from = null )
	{
		// TODO: actually check if we can reach this.
		// Implementation should be somewhat leniant,
		// as this is used for teleport validation AND ensuring player has re-entered valid space in a legal maner.
		return true;
	}

	/// <summary>
	/// Called whenever the player starts fake moving to make sure they didn't move into a wall or something.
	/// If they did, TP them out.
	/// </summary>
	/// <returns>If we were able to move the player into a valid position.</returns>
	private bool ValidatePosition()
	{
		Vector3 worldPos = WorldFeetPos;

		// Could you have gotten here legally?
		if ( CanReach( worldPos, lastValidPosition ) )
		{
			if ( IsPosValid( worldPos ) )
			{
				return true;
			}
			else if ( ValidatePosWithStep( ref worldPos ) )
			{
				WorldFeetPos = worldPos;
				return true;
			}
		}

		worldPos = lastValidPosition;
		// Last valid position might not be valid anymore.
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

		int num = 10;
		for (int i = 0; i < num; i++)
		{
			Vector3 vec = WorldFeetPos + Vector3.Random.Normal * (_stuckTries / 12f);
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
