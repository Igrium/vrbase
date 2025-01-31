
HEADER
{
	Description = "The sphere shown around the player's head when they suffocate.";
}

FEATURES
{
	#include "common/features.hlsl"
}

MODES
{
	Forward();
	Depth();
	ToolsShadingComplexity( "tools_shading_complexity.shader" );
}

COMMON
{
	#ifndef S_ALPHA_TEST
	#define S_ALPHA_TEST 0
	#endif
	#ifndef S_TRANSLUCENT
	#define S_TRANSLUCENT 1
	#endif
	
	#include "common/shared.hlsl"
	#include "procedural.hlsl"

	#define S_UV2 1
	#define CUSTOM_MATERIAL_INPUTS
}

struct VertexInput
{
	#include "common/vertexinput.hlsl"
	float4 vColor : COLOR0 < Semantic( Color ); >;
};

struct PixelInput
{
	#include "common/pixelinput.hlsl"
	float3 vPositionOs : TEXCOORD14;
	float3 vNormalOs : TEXCOORD15;
	float4 vTangentUOs_flTangentVSign : TANGENT	< Semantic( TangentU_SignV ); >;
	float4 vColor : COLOR0;
	float4 vTintColor : COLOR1;
};

VS
{
	#include "common/vertex.hlsl"

	PixelInput MainVs( VertexInput v )
	{
		
		PixelInput i = ProcessVertex( v );
		i.vPositionOs = v.vPositionOs.xyz;
		i.vColor = v.vColor;
		
		ExtraShaderData_t extraShaderData = GetExtraPerInstanceShaderData( v );
		i.vTintColor = extraShaderData.vTint;
		
		VS_DecodeObjectSpaceNormalAndTangent( v, i.vNormalOs, i.vTangentUOs_flTangentVSign );
		return FinalizeVertex( i );
		
	}
}

PS
{
	#include "common/pixel.hlsl"
	
	float g_flNoiseAmount < UiType( Slider ); UiGroup( "Noise,0/,0/0" ); Default1( 0.1 ); Range1( 0, 1 ); >;
	float4 g_vBottomColor < UiType( Color ); UiGroup( "Color,0/,0/0" ); Default4( 0.35, 0.15, 0.00, 1.00 ); >;
	float4 g_vTopColor < UiType( Color ); UiGroup( "Color,0/,0/0" ); Default4( 1.00, 0.43, 0.00, 1.50 ); >;
	float g_flWallOffset < Attribute( "WallOffset" ); Default1( -12 ); >;
	
	float4 MainPs( PixelInput i ) : SV_Target0
	{
		float timeMultiplied = g_flTime * 48;

	// Calculate noise UV coordinates
	float2 noiseUV = float2(0, timeMultiplied);
	float2 noiseUV2 = TileAndOffsetUv(i.vTextureCoords.xy, float2(400, 100), noiseUV);
	float noise = Simplex2D(noiseUV2);

	// Pre-calculate noise adjustment values
	float halfNoiseAmount = g_flNoiseAmount / 2;
	float minNoise = 1 - halfNoiseAmount;
	float maxNoise = halfNoiseAmount + 1;
	float adjustedNoise = (noise / 1) * (maxNoise - minNoise) + minNoise;

	// Interpolate colors based on position
	float positionZ = i.vPositionOs.z;
	float colorFactor = saturate((positionZ + 16) / 28);
	float4 interpolatedColor = lerp(g_vBottomColor, g_vTopColor, colorFactor);

	// Combine noise and interpolated color
	float4 finalColor = float4(adjustedNoise, adjustedNoise, adjustedNoise, adjustedNoise) * interpolatedColor;

	// Calculate wall offset
	float offsetPositionZ = positionZ + g_flWallOffset;
	float wallFactor = saturate((offsetPositionZ - 1.1) / (-0.3 - 1.1));

	return float4(finalColor.xyz, wallFactor);
	}
}
