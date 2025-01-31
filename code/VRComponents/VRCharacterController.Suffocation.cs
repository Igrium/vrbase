using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRBase;

public partial class VRCharacterController
{

	/// <summary>
	/// Whether to do camera clip prevention
	/// </summary>
	[Property]
	[Category( "Suffocation" )]
	public bool EnableSuffocation { get; set; } = true;

	[Property]
	[Category( "Suffocation" )]
	public float SuffocationRadius { get; set; } = 2f;

	/// <summary>
	/// Colliders in this category will prevent movement but won't cause suffocation.
	/// </summary>
	[Property]
	[Category( "Suffocation" )]
	public TagSet SuffocationIgnores { get; set; } = new TagSet();

	/// <summary>
	/// A GameObject containing a mesh that's placed over the player's head when they suffocate.
	/// </summary>
	[Property]
	[Category( "Suffocation" )]
	public GameObject? SuffocationMesh { get; set; }

	protected ModelRenderer? SuffocationMeshComponent => SuffocationMesh?.GetComponent<ModelRenderer?>(true);

	public bool IsSuffocating { get; private set; }

	/// <summary>
	/// The normal of the wall causing us to suffocate.
	/// </summary>
	public Vector3 SuffocationNormal { get; private set; }

	/// <summary>
	/// The position where we entered the wall we're suffocating in.
	/// </summary>
 	public Vector3 SuffocationPos { get; private set; }

	private bool shouldSuffocate;
	private Vector3 lastValidHeadPosition;

	protected virtual void TickSuffocation()
	{
		shouldSuffocate = ShouldSuffocate();

		if ( !shouldSuffocate )
		{
			lastValidHeadPosition = WorldEyePos;
		}

		if ( EnableSuffocation && !IsSuffocating && shouldSuffocate )
		{
			StartSuffocating();
		}

		if ( IsSuffocating && MayStopSuffocating() )
		{
			StopSuffocating();
		}
	}

	protected virtual void StartSuffocating()
	{
		//SuffocationNormal = res.Normal;
		//SuffocationPos = res.HitPosition;

		// Trace to suffocation to find the face causing it to start.
		SceneTraceResult trace = BuildHeadTrace( lastValidHeadPosition, WorldEyePos ).Run();
		SuffocationPos = trace.EndPosition;
		SuffocationNormal = trace.Normal;
		Log.Info( trace );
		IsSuffocating = true;
		var mesh = SuffocationMeshComponent;
		Log.Info( mesh );

		if ( mesh.IsValid())
		{
			mesh.Enabled = true;
		}
	}

	protected virtual void StopSuffocating()
	{
		IsSuffocating = false;
		var mesh = SuffocationMeshComponent;
		if (mesh.IsValid())
		{
			mesh.Enabled = false;
		}
	}

	/// <summary>
	/// There might be other factors as to whether we can stop suffocation, other than whether we're out of the wall.
	/// This function checks all those.
	/// </summary>
	/// <returns>Whether we can stop suffocating.</returns>
	protected virtual bool MayStopSuffocating()
	{
		return !ShouldSuffocate() && CanReach( WorldFeetPos, lastValidPosition );
	}

	/// <summary>
	/// Check if we should be suffocating given our current head position.
	/// </summary>
	/// <returns>Should we suffocate?</returns>
	private bool ShouldSuffocate()
	{
		Vector3 eyePos = WorldEyePos;
		return BuildHeadTrace( eyePos, eyePos ).Run().StartedSolid;
	}

	private SceneTrace BuildHeadTrace( in Vector3 from, in Vector3 to )
	{
		return BuildHeadTrace( Scene.Trace.Ray( from, to ) );
	}

	private SceneTrace BuildHeadTrace( in SceneTrace source )
	{
		SceneTrace trace = source
			.Radius( SuffocationRadius )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( SuffocationIgnores );

		if ( UseCollisionRules )
		{
			trace = trace.WithCollisionRules( this.Tags );
		}
		else
		{
			trace = trace.WithoutTags( IgnoreLayers );
		}

		return trace;
	}


}
