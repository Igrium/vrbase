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

	public ModelRenderer? Model => GetComponent<ModelRenderer>();

	protected override void OnUpdate()
	{
		base.OnUpdate();
		var player = Player;
		if (player.IsValid() && player.IsSuffocating)
		{
			WorldRotation = Rotation.LookAt( player.SuffocationNormal ).RotateAroundAxis(Vector3.Right, -90);

			ModelRenderer? model = Model;
			if ( model.IsValid() )
			{
				Plane suffocationPlane = new Plane( player.SuffocationPos, player.SuffocationNormal );
				float dist = MathF.Max(suffocationPlane.GetDistance( player.WorldEyePos ) * 5, -15.5f);

				model.SceneObject.Attributes.Set( "WallOffset", dist );
			}
		}
	}
}
