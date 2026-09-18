using System;
using Sandbox.Movement;

namespace VRBase.Player;

public sealed class NoclipMoveMode : MoveMode
{
	[Property]
	public float RunSpeed { get; set; } = 600;

	[Property]
	public float WalkSpeed { get; set; } = 200;

	public override int Score( PlayerController controller )
	{
		return 1000;
	}

	public override void UpdateRigidBody( Rigidbody body )
	{
		body.Gravity = false;
		body.LinearDamping = 0f;
		body.AngularDamping = 1f;
	}

	public override void AddVelocity()
	{
		var body = Controller.Body;
		var target = Controller.WishVelocity;
		const float responseTime = 0.075f;
		var fraction = 1f - MathF.Exp( -Time.Delta / responseTime );
		var velocity = Vector3.Lerp( body.Velocity, target, fraction );

		body.Velocity = (velocity - target).IsNearlyZero( 0.01f ) ? target : velocity;
	}

	public override void OnModeBegin()
	{
		Controller.IsClimbing = true;
		Controller.Body.Gravity = false;
		Controller.ColliderObject.Enabled = false;
	}

	public override void OnModeEnd( MoveMode next )
	{
		Controller.IsClimbing = false;
		Controller.Body.Velocity = Controller.Body.Velocity.ClampLength( Controller.RunSpeed );
		Controller.ColliderObject.Enabled = true;
	}

	public override Vector3 UpdateMove( Rotation eyes, Vector3 input )
	{
		input = input.ClampLength( 1 );

		var direction = eyes * input;

		bool run = Input.Down( Controller.AltMoveButton );
		if ( Controller.RunByDefault ) run = !run;

		var velocity = run ? RunSpeed * 2.0f : RunSpeed;

		if ( Input.Down( "walk" ) || Input.Down( "duck" ) ) velocity = WalkSpeed;

		if ( direction.IsNearlyZero( 0.1f ) )
		{
			direction = 0;
		}

		if ( Input.Down( "jump" ) ) direction += Vector3.Up;

		return direction * velocity;
	}
}