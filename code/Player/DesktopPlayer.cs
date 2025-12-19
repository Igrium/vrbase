using VRBase.Util;
namespace VRBase.Player;

public class DesktopPlayer : Component
{
	[Property, RequireComponent]
	public VRTeleporter VrTeleporter { get; set; } = null!;

	protected override void OnUpdate()
	{
		if ( !IsProxy && Input.Down( "teleport" ) )
		{
			var forward = Scene.Camera.WorldRotation.Forward;
			var dest = Scene.Trace.Ray( new Ray( Scene.Camera.WorldPosition, forward ), 512 )
				.IgnoreGameObjectHierarchy(GameObject).Run().EndPosition;
			
			DebugOverlay.Sphere(new Sphere(dest, 4));

			VrTeleporter.TryTeleport( GameObject.WorldPosition, dest, 1024f );
		}
		base.OnUpdate();
	}
}
