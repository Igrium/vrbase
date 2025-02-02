using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRBase;

internal static class TraceUtils
{
	/// <summary>
	/// Check if any corner of a bounding box is visible from a given angle.
	/// </summary>
	/// <param name="source">A trace source with the proper collision params setup.</param>
	/// <param name="sourcePos">Position to trace from.</param>
	/// <param name="bbox">Box to trace against.</param>
	/// <returns>If the box is visible.</returns>
	public static bool IsBBoxVisible( SceneTrace source, in Vector3 sourcePos, in BBox bbox )
	{
		// TODO: Is there a cleaner way to do this?
		foreach ( var corner in bbox.Corners )
		{
			source = source.Ray( sourcePos, corner );
			if ( !source.Run().Hit ) return true;
		}

		return false;
	}
}
