#nullable enable

using System.Runtime.CompilerServices;
using Sandbox.VR;

namespace VRBase;

/// <summary>
/// A crappy re-implementation of VRHand because VRHand messes with coordinate spaces.
/// </summary>
public sealed class VRHandAnimationController : Component
{
	[Property]
	VRHand.HandSources HandSide { get; set; } = VRHand.HandSources.Left;

	[Property, RequireComponent]
	SkinnedModelRenderer? Model { get; set; }

	private float[] curlClamps = new float[5] { 1f, 1f, 1f, 1f, 1f };

	public VRController TranslateHandSide()
	{
		switch (HandSide)
		{
			case VRHand.HandSources.Left:
				return Input.VR.LeftHand;
			case VRHand.HandSources.Right:
				return Input.VR.RightHand;
			default:
				return Input.VR.RightHand;
		}
	}

	private float ClampedFingerCurl( FingerValue finger )
	{
		float rawCurl = TranslateHandSide().GetFingerCurl( (int)finger );
		return MathX.Clamp( rawCurl, 0f, curlClamps[(int)finger] );
	}

	public void SetCurlClamp( FingerValue finger, float clamp )
	{
		curlClamps[(int)finger] = clamp;
	}

	public void ResetCurlClamps()
	{
		curlClamps = new float[5] { 1f, 1f, 1f, 1f, 1f };
	}

	public void SetCurlClamps( float[] clamps )
	{
		if ( curlClamps.Length == clamps.Length )
		{
			curlClamps = clamps;
		}
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if (Model.IsValid())
		{
			Model.Set( "Thumb", ClampedFingerCurl( FingerValue.ThumbCurl ) );
			Model.Set( "Index", ClampedFingerCurl( FingerValue.IndexCurl ) );
			Model.Set( "Middle", ClampedFingerCurl( FingerValue.MiddleCurl ) );
			Model.Set( "Ring", ClampedFingerCurl( FingerValue.RingCurl ) );
			Model.Set( "Pinky", ClampedFingerCurl( FingerValue.PinkyCurl ) );
		}

	}
}
