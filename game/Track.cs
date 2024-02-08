using Godot;
using System;
using System.Threading;

public partial class Track : Node
{
	[Export] public int humanControl = 0;
	[Export] public int numberCars = 1;

	private PackedScene carScene;
	private Car[] cars = new Car[1];
	private TrackCurve track;
	private Node sync;

	public override void _Ready()
	{
		carScene = GD.Load<PackedScene>("res://game/car.tscn");

		sync = GetNode<Node>("Sync");
		track = GetNode<TrackCurve>("TrackCurve");
		cars[0] = GetNode<Car>("Car_0");

		foreach (Car car in cars) {
			car.SetTrack(track);
		}

		if (humanControl > -1 && numberCars < 2) {
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

}
