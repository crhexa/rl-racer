using Godot;
using System;

/*
	Code adapted from: https://kidscancode.org/godot_recipes/3.x/2d/multi_target_camera/
*/

public partial class MultiCamera2D : Camera2D
{
	[Export] public Vector2 margin = new(1200, 1200);
	[Export] public float moveSpeed = 0.1f;
	[Export] public float zoomSpeed = 0.02f;
	[Export] public float minZoom = 0.1f;
	[Export] public float maxZoom = 0.5f;

	private Vector2 screenSize;
	private float screenAspect;
	private Car[] cars;
	private Track track;
	private int numberCars;


	public override void _Ready()
	{
		screenSize = GetViewportRect().Size;
		screenAspect = screenSize.Aspect();
	}

	
	public override void _PhysicsProcess(double delta)
	{
		MoveCamera(track.carState);
	}


	public void SetCars(Car[] cars, Track track) {
		this.cars = cars;
		this.track = track;
		numberCars = cars.Length;
	}


	private void MoveCamera(bool[] invalid) {
		Vector2 newPosition = Vector2.Zero;
		Rect2 box = new(Position, Vector2.One);
		float z;
		int n = 0;

		for (int i = 0; i < numberCars; i++) {
			if (!invalid[i]) {
				newPosition += cars[i].Position;
				box = box.Expand(cars[i].Position);
				n++;
			}
		}
		newPosition /= n;

		if (n != 0) {
			Position = Position.Lerp(newPosition, moveSpeed);

		} else {
			return;
		}

		// Set the camera zoom to fit all targets
		box.GrowIndividual(margin.X, margin.Y, margin.X, margin.Y);
		z = box.Size.X > (box.Size.Y * screenAspect) ?
			Mathf.Clamp(screenSize.X / (1.8f*box.Size.X), minZoom, maxZoom):
			Mathf.Clamp(screenSize.Y / (1.8f*box.Size.Y), minZoom, maxZoom);

		Zoom = Zoom.Lerp(Vector2.One * z, zoomSpeed);
	}
}
