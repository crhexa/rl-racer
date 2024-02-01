using Godot;
using System;

public partial class Car : CharacterBody2D
{
	private Wheels wheels;
	private Label debug;
	
	[Export] public float slipSpeed = 200f;
	[Export] public float enginePower = 1000f;
	[Export] public float brakePower = -10f;
	[Export] public float fastTraction = 2f;
	[Export] public float slowTraction = 6f;
	[Export] public float friction = -20f;
	[Export] public float drag = -0.0005f;

	private Vector2 acceleration = Vector2.Zero;
	private float lateral = 0f;
	private float slip = 0f;
	private bool isSlipping = false;

	public override void _Ready()
	{
		wheels = GetNode<Wheels>("Wheels");
		debug = GetNode<Label>("Canvas/DebugText");
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
		SetDebugText();
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
		// Add tire particles on braking
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
		float traction = GetTraction(heading);
		wheels.ToggleTireParticles(isSlipping);
		
		Velocity = Velocity.Lerp(heading * Velocity.Length(), traction);
		Rotation = offset + heading.Angle();
	}

	private float GetTraction(Vector2 heading) {
		lateral = (Velocity - Velocity.Project(heading)).Length() / slipSpeed;

		if (isSlipping) {
			slip = Mathf.Clamp((1.09756f / (1 + Mathf.Exp(-6.13617f*lateral + 3.06809f))) - 0.0487788f, 0f, 1f);
			if (slip <= 0.1f) isSlipping = false;

		} else {
			slip = Mathf.Clamp(Mathf.Pow(lateral, 2.5f), 0f, 1f);
			if (slip >= 1f) isSlipping = true;
		}
		return 1 - slip;
	}

	private void SetDebugText() {
		debug.Text = $"Speed: {Velocity.Length():f2}\nLateral Velocity: {lateral:f2}\nSlip Fraction: {slip:f2}";
	}
}
