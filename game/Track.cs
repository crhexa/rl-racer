using Godot;
using System;

public partial class Track : Node
{
	[Export] public bool agentControl = true;
	[Export] public int humanControl = 0;
	[Export] public int numberCars = 1;
	[Export] public float horizontalOffset = 280f;
	[Export] public float verticalOffset = 480f;

	private PackedScene carScene;
	private Car[] cars;
	private TrackCurve track;
	private Node sync;

	public override void _Ready()
	{
		cars = new Car[numberCars];
		carScene = GD.Load<PackedScene>("res://game/car.tscn");

		sync = GetNode<Node>("Sync");
		track = GetNode<TrackCurve>("TrackCurve");
		cars[0] = GetNode<Car>("Car_0");

		Vector2 start = cars[0].Position;
		cars[0].startingPosition = start;
		for (int i = 1; i < numberCars; i++) {
			cars[i] = carScene.Instantiate<Car>();
			PlaceCar(cars[i], start, i);
			AddChild(cars[i]);
		}
		MoveChild(sync, -1);

		foreach (Car car in cars) {
			car.SetTrack(track);
		}

		if (!agentControl || (humanControl > -1 && numberCars < 2)) {
			sync.Set("disabled", true);
		}
	}

	public override void _Process(double delta)
	{
		if (humanControl > -1) {
			float accel = 0f;
			float steer = 0f;

			if (Input.IsActionPressed("Accelerate")) {
				accel = 1f;

			} else if (Input.IsActionPressed("Brake")) {
				accel = -1f;
			}

			if (Input.IsActionPressed("Turn Left")) {
				steer = -1f;

			} else if (Input.IsActionPressed("Turn Right")) {
				steer = 1f;
			}

			cars[humanControl].SetActions(steer, accel);
		}
	}

	private void PlaceCar(Car car, Vector2 start, float i) {
		if (i % 2 == 0) {
			car.startingPosition = new(start.X, start.Y + (0.5f * i * verticalOffset));

		} else {
			car.startingPosition = new(start.X + horizontalOffset, start.Y + (0.5f * i * verticalOffset));
		}
		car.Position = car.startingPosition;
	}
}
