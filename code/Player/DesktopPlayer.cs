using VRBase.Util;
namespace VRBase.Player;

public class DesktopPlayer : Component
{
	[Property, RequireComponent]
	public VRTeleporter VrTeleporter { get; set; } = null!;

	private Vector3 _teleportDest;
	
	protected override void OnUpdate()
	{
		if ( !IsProxy && Input.Down( "teleport" ) )
		{
			var forward = Scene.Camera.WorldRotation.Forward;
			var dest = Scene.Trace.Ray( new Ray( Scene.Camera.WorldPosition, forward ), 512 )
				.IgnoreGameObjectHierarchy(GameObject).Run().EndPosition;
			
			DebugOverlay.Sphere(new Sphere(dest, 4));

			 _teleportDest = VrTeleporter.TryTeleport( GameObject.WorldPosition, dest, 1024f ).EndPos;
		}
		if ( !IsProxy && Input.Released( "teleport" ) )
		{
			GameObject.WorldPosition = _teleportDest;
		}
		base.OnUpdate();
	}
}
