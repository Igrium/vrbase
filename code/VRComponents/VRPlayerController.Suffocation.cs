using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRBase;

public partial class VRPlayerController
{

	/// <summary>
	/// Whether to do camera clip prevention
	/// </summary>
	[Property]
	[Category( "Suffocation" )]
	public bool EnableSuffocation { get; set; } = true;

	[Property]
	[Category( "Suffocation" )]
	public float SuffocationRadius { get; set; } = 3f;

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
	/// The world-space normal of the wall causing us to suffocate.
	/// </summary>
	public Vector3 SuffocationNormal { get; private set; }

	/// <summary>
	/// The world-space position where we entered the wall we're suffocating in.
	/// </summary>
 	public Vector3 SuffocationPos { get; private set; }

	private bool shouldSuffocate;

	/// <summary>
	/// Keep last valid head pos in room space so that an unexpected fake move doesn't cause us to start suffocating because the trace is blocked.
	/// </summary>
	private Vector3 lastValidHeadPosition;

	protected virtual void TickSuffocation()
	{

		if ( EnableSuffocation && !IsSuffocating && ShouldSuffocate() )
		{
			StartSuffocating();
		}
		else if ( IsSuffocating && MayStopSuffocating() )
		{
			StopSuffocating();
		}

		if ( !IsSuffocating )
		{
			lastValidHeadPosition = LocalEyePos;
		}
	}

	protected virtual void StartSuffocating()
	{
		// Trace to suffocation to find the face causing it to start.
		Vector3 lastEyePos = WorldTransform.PointToWorld( lastValidHeadPosition );
		Vector3 eyePos = WorldEyePos;

		SuffocationPos = (eyePos + lastEyePos) / 2;
		SuffocationNormal = (lastEyePos - WorldEyePos).Normal;
		IsSuffocating = true;

		var mesh = SuffocationMeshComponent;
		if ( mesh.IsValid() )
		{
			mesh.Enabled = true;
		}
	}

	protected virtual void StopSuffocating()
	{
		IsSuffocating = false;
		var mesh = SuffocationMeshComponent;
		if ( mesh.IsValid() )
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
		return !ShouldSuffocate();
	}


	/// <summary>
	/// Check if we should be suffocating given our current head position.
	/// </summary>
	/// <returns>Should we suffocate?</returns>
	private bool ShouldSuffocate()
	{
		Vector3 eyePos = WorldEyePos;
		var trace = BuildHeadTrace( WorldTransform.PointToWorld( lastValidHeadPosition ), eyePos, SuffocationRadius ).Run();
		// If the trace started solid, we obviously have an incorrect last valid head pos, so don't use it.
		if ( trace.StartedSolid )
		{
			trace = BuildHeadTrace( eyePos, eyePos, SuffocationRadius ).Run();
		}
		return trace.Hit;
	}

	private SceneTrace BuildHeadTrace( in Vector3 from, in Vector3 to, float? radius = null )
	{
		return BuildHeadTrace( Scene.Trace.Ray( from, to ), radius );
	}

	private SceneTrace BuildHeadTrace( in SceneTrace source, float? radius = null )
	{
		SceneTrace trace = source
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( SuffocationIgnores );

		if (radius.HasValue)
		{
			trace = trace.Radius( radius.Value );
		}

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
