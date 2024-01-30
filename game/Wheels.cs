using Godot;
using System;

public partial class Wheels : Node2D
{
	[Export] public float maxWheelAngle = Mathf.Pi * 0.2f;
	[Export] public float turningSpeed = Mathf.Pi / 4f;

	public Wheel[] wheels = new Wheel[4];

	public float wheelbase;
	public float track; 

	public float steeringAngle = 0f;


	public override void _Ready()
	{
		wheels[0] = GetNode<Wheel>("FLWheel");
		wheels[1] = GetNode<Wheel>("FRWheel");
		wheels[2] = GetNode<Wheel>("BLWheel");
		wheels[3] = GetNode<Wheel>("BRWheel");

		Vector2 dimensions = wheels[3].Position - wheels[0].Position;
		track = dimensions[0];
		wheelbase = dimensions[1];
	}

	public override void _PhysicsProcess(double delta)
	{
		// TEMPORARY

		if (Godot.Input.IsActionPressed("Turn Left")) {
			Steer(-1, (float) delta);

		} else if (Godot.Input.IsActionPressed("Turn Right")) {
			Steer(1, (float) delta);

		} else {
			Steer(0, (float) delta);
		}

		
	}


	public void Steer(float steeringFraction, float delta) {
		float desiredAngle = MathF.Min(steeringFraction, 1f) * maxWheelAngle;
		float direction = desiredAngle >= steeringAngle ? 1f : -1f;
		float changeAngle = direction * delta * turningSpeed;

		if (direction < 0) {
			SetWheelAngles(MathF.Max(changeAngle + steeringAngle, desiredAngle));

		} else {
			SetWheelAngles(MathF.Min(changeAngle + steeringAngle, desiredAngle));
		}
		
	}

	private void SetWheelAngles(float newAngle) {
		steeringAngle = newAngle;

		// Clockwise
		if (newAngle > 0) {
			wheels[1].Rotation = newAngle;
			wheels[0].Rotation = Ackermann(newAngle);

		// Counterclockwise
		} else {
			wheels[0].Rotation = newAngle;
			wheels[1].Rotation = -Ackermann(-newAngle);
		}
	}

	private static float Recip(float angle) {
		return (Mathf.Pi * 0.5f) - angle;
	}

	private float Ackermann(float angle) {
		return Recip(MathF.Atan(((wheelbase * MathF.Abs(MathF.Tan(Recip(angle)))) + track) / wheelbase));
	}
}
