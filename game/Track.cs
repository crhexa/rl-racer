using Godot;
using System;
using System.Linq;

public partial class Track : Node
{
	[Export] public bool agentControl = true;
	[Export] public int humanControl = 0;
	[Export] public int numberCars = 1;
	[Export] public Vector2 startOffset = new Vector2(400f, 560f);
	[Export] public Vector2 freezeOffset = new Vector2(-20000f, -20000f);

	private PackedScene carScene;
	private Car[] cars;
	private bool[] carState;
	private TrackCurve track;
	private Node sync;

	public override void _Ready()
	{
		cars = new Car[numberCars];
		carState = new bool[numberCars];
		carScene = GD.Load<PackedScene>("res://game/car.tscn");

		sync = GetNode<Node>("Sync");
		track = GetNode<TrackCurve>("TrackCurve");
		cars[0] = GetNode<Car>("Car_0");

		Vector2 start = cars[0].Position;
		cars[0].startingPosition = start;

		for (int i = 0; i < numberCars; i++) {
			if (i > 0) {
				cars[i] = carScene.Instantiate<Car>();
				PlaceCar(cars[i], start, i);
				AddChild(cars[i]);
			}
			carState[i] = false;
			cars[i].SetTrack(track, this);
			cars[i].carID = i;
		}

		MoveChild(sync, -1);
		if (!agentControl || (humanControl > -1 && numberCars < 2)) {
			sync.Set("disabled", true);
		}
	}

	/*
		Read input from keyboard
	*/
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

	/*
		Places cars in staggered formation

		Parameters:
			Car car 	  	: the car to be placed
			Vector2 start 	: the starting position of the first car
	*/
	private void PlaceCar(Car car, Vector2 start, float i) {
		if (i % 2 == 0) {
			car.startingPosition = new(start.X, start.Y + (0.5f * i * startOffset.Y));

		} else {
			car.startingPosition = new(start.X + startOffset.X, start.Y + (0.5f * i * startOffset.Y));
		}
		car.Position = car.startingPosition;
	}

	/*
		Raises the done flags of every car
	*/
	public void ResetCars() {
		for (int i = 0; i < numberCars; i++) {
			cars[i].needsRestart = true;
			cars[i].controller.done = true;
			cars[i].controller.Subscribe();
		}
	}


	public void FreezeCar(int id) {
		if (carState[id]) return;

		Car car = cars[id];
		carState[id] = true;

		car.ProcessMode = ProcessModeEnum.Disabled;
		if (carState.All(x => x)) {
			ResetCars();
			return;
		}

		// Move the car out of the map and disable it
		car.Rotation = 0f;
		car.Position = car.startingPosition + freezeOffset;
		car.controller.Unsubscribe();
	}


	public void FinishedRestarting(int id) {
		carState[id] = false;
	}
}
