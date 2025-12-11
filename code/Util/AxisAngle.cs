using System;
namespace VRBase.Util;

public record struct AxisAngle
{
	public Vector3 Axis;
	public float Angle;

	public Rotation ToRotation()
	{
		return Rotation.FromAxis( Axis, Angle );
	}
}

public static class AxisAngleExt
{
	public static AxisAngle ToAxisAngle( this Rotation rot )
	{
		if ( rot.w > 1 )
			rot = rot.Normal;

		AxisAngle val = default;
		val.Angle = MathX.RadianToDegree((float)(2 * Math.Acos( rot.w )));
		float s = MathF.Sqrt( 1 - rot.w * rot.w );
		if ( s < 0.001f ) // Avoid divide by zero.
		{
			val.Axis.x = rot.x;
			val.Axis.y = rot.y;
			val.Axis.z = rot.z;
		}
		else
		{
			val.Axis.x = rot.x / s;
			val.Axis.y = rot.y / s;
			val.Axis.z = rot.z / s;
			val.Axis = val.Axis.Normal;
		}
		return val;
	}
}
