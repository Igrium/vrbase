using System;
using System.Collections.Generic;
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

	protected virtual void TickSuffocation()
	{
		if (EnableSuffocation && !IsSuffocating && ShouldSuffocate(out var res))
		{
			StartSuffocating(res);
		}

		if (IsSuffocating && MayStopSuffocating())
		{
			StopSuffocating();
		}
	}

	protected virtual void StartSuffocating(in SceneTraceResult res)
	{
		SuffocationNormal = res.Normal;
		SuffocationPos = res.HitPosition;
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
	/// <param name="traceResult">The trace result causing us to suffocate.</param>
	/// <returns>Should we suffocate?</returns>
	private bool ShouldSuffocate(out SceneTraceResult traceResult)
	{
		Vector3 eyePos = HMD.IsValid() ? HMD.WorldPosition : WorldPosition;
		SceneTrace trace = Scene.Trace.Ray( eyePos, eyePos )
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

		traceResult = trace.Run();
		return traceResult.StartedSolid;
	}

	private bool ShouldSuffocate()
	{
		return ShouldSuffocate( out SceneTraceResult res );
	}
}
