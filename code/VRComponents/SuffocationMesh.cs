using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRBase;

/// <summary>
/// Simple, component-level logic for the suffocation mesh.
/// </summary>
public sealed class SuffocationMesh : Component
{
	public VRCharacterController? Player => GetComponentInParent<VRCharacterController>();

	protected override void OnUpdate()
	{
		base.OnUpdate();
		var player = Player;
		if (player.IsValid() && player.IsSuffocating)
		{
			WorldRotation = Rotation.LookAt( player.SuffocationNormal ).RotateAroundAxis(Vector3.Right, -90);
		}
	}
}
