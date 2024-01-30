using Godot;
using System;

public partial class Car : CharacterBody2D
{
	private Wheels wheels;
	
	[Export] public float slipSpeed = 2000f;
	[Export] public float enginePower = 1000f;
	[Export] public float brakePower = -10f;
	[Export] public float fastTraction = 2f;
	[Export] public float slowTraction = 6f;
	[Export] public float friction = -20f;
	[Export] public float drag = -0.001f;

	public Vector2 acceleration = Vector2.Zero;

	public override void _Ready()
	{
		wheels = GetNode<Wheels>("Wheels");
	}

	public override void _PhysicsProcess(double delta)
	{
		acceleration = GetAcceleration();
		if (acceleration == Vector2.Zero && Velocity.Length() < 5) {
			Velocity = Vector2.Zero;

		} else {
			acceleration += GetFriction((float) delta);
		}

		SteerCar((float) delta);
		Velocity += acceleration * (float) delta;
		MoveAndSlide();
	}

	private Vector2 GetAcceleration() {
		if (Input.IsActionPressed("Accelerate")) {
			return -1 * Transform.Y * enginePower;

		} else {
			return Vector2.Zero;
		}
	}

	private Vector2 GetFriction(float delta) {
		// Add minimum braking friction
		float effectFriction = friction + (Input.IsActionPressed("Brake") ? brakePower : 0f);
		return Velocity * delta * (effectFriction + (Velocity.Length() * drag));
	}

	private void SteerCar(float delta) {
		const float offset = Mathf.Pi * 0.5f;

		Vector2 vector = new Vector2(0, wheels.wheelbase*0.5f).Rotated(Rotation);
		Vector2 front = Position - vector;
		Vector2 rear = Position + vector;

		front += Velocity.Rotated(wheels.steeringAngle) * delta;
		rear += Velocity * delta;
		
		Vector2 heading = rear.DirectionTo(front);
		float traction = Velocity.Length() > slipSpeed ? fastTraction : slowTraction;
		
		Velocity = Velocity.Lerp(heading * Velocity.Length(), traction * delta);
		Rotation = offset + heading.Angle();
	}
}
