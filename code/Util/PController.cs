namespace VRBase.Util;

public static class PController
{
	public static Vector3 GetPosVelocity( Vector3 curPos, Vector3 targetPos, float factor = 1f )
	{
		Vector3 err = targetPos - curPos;
		return err * factor * Time.Delta;
	}

	public static Vector3 GetRotVelocity( Rotation curRot, Rotation targetRot, float factor = 50f )
	{
		AxisAngle err = (targetRot * curRot.Inverse).ToAxisAngle();
		if ( err.Angle > 180f )
			err.Angle -= 360f;

		return err.Angle * err.Axis * factor * Time.Delta;
	}
}
