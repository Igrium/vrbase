using System.Linq;
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

	private void ToggleNoclip()
	{
		var noclip = GetComponent<NoclipMoveMode>( true );
		if ( noclip is null )
		{
			Log.Warning( $"{GameObject.Name} has no {nameof( NoclipMoveMode )} component." );
			return;
		}

		noclip.Enabled = !noclip.Enabled;
	}

	[ConCmd( "noclip" )]
	public static void Noclip()
	{
		var player = Game.ActiveScene?.GetAllComponents<DesktopPlayer>().FirstOrDefault( p => !p.IsProxy );
		if ( player is null )
		{
			Log.Warning( "No local player found to toggle noclip on." );
			return;
		}

		player.ToggleNoclip();
	}
}
