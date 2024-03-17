using Godot;
using System;

public partial class Car : CharacterBody2D
{
	public const int M = 17;	// Feature space dimensions

	// Node related variables
	public int carID;
	private Wheels wheels;
	private Label debug;
	private Track env;
	private TrackCurve track;
	private Proximity proximity;
	public Controller controller;
	
	// Editor variables
	[Export] public Vector2 startingPosition = Vector2.Zero;
	[Export] public bool hasFocus = false;
	[Export] public float slipSpeed = 180f;
	[Export] public float enginePower = 1000f;
	[Export] public float brakePower = -10f;
	[Export] public float upFriction = -20f;
	[Export] public float sideFriction = -25f;
	[Export] public float drag = -0.0005f;
	[Export] public float maxDistanceFromTrack = 1600f;

	// State variables
	private Vector2 acceleration = Vector2.Zero;
	private Vector2 heading;
	private float lateral = 0f;
	private float slip = 0f;
	private bool isSlipping = false;
	private bool onTrack = true;
	private Vector2 point = Vector2.Zero;
	private Vector2 normal = Vector2.Up;
	private Vector2 direct;

	// Agent related variables
	public bool needsRestart = false;
	public float reward = 0f;
	public float totalReward = 0f;
	private float[] features;
	private Vector2 orthonormal;
	private Vector2 lateralVector;
	private float side;
	private float direction;
	private float lateralOffset = 0f;
	private float steeringFraction = 0f;
	private float accelerationFraction = 0f;


	public override void _Ready()
	{
		wheels = GetNode<Wheels>("Wheels");
		proximity = GetNode<Proximity>("Proximity");
		controller = GetNode<Controller>("Controller");
		debug = hasFocus ? GetNode<Label>("Canvas/DebugText") : null;

		heading = Vector2.Up.Rotated(Rotation);
		features = new float[M];
		controller.init(this);
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float) delta;
		wheels.Steer(steeringFraction, dt);

		// If the car isn't accelerating and the velocity is below a certain threshold, then stop the car
		acceleration = GetAcceleration();
		if (acceleration == Vector2.Zero && Velocity.Length() < 5) {
			Velocity = Vector2.Zero;

		// Otherwise apply friction
		} else {
			acceleration += GetFriction(dt);
		}

		// Apply steering, acceleration, velocity, and collisions
		SteerCar(dt);
		Velocity += acceleration * dt;
		CheckCollisions(MoveAndCollide(Velocity * dt));

		// Recalculate features/rewards and update the debug display
		CalculateFeatures();
		SetReward(dt);
		if (hasFocus) SetDebugText();

		if (CarNeedsReset()) {
			env.FreezeCar(carID);
			return;
		}
	}
	
	public void SetTrack(TrackCurve t, Track e) {
		track = t;
		env = e;
	}

	/****************************
	*	Logic for car physics
	*****************************/
	private Vector2 GetAcceleration() {
		if (accelerationFraction > 0f) {
			return -1 * Transform.Y * enginePower * accelerationFraction;

		} else {
			return Vector2.Zero;
		}
	}

	private Vector2 GetFriction(float delta) {
		float effectFriction = upFriction + (slip * sideFriction);

		if (accelerationFraction < 0) {
			effectFriction += brakePower * accelerationFraction;
		}

		onTrack = track.IsOnTrack(Position);
		if (!onTrack) {
			effectFriction *= 2f;
		}
		
		return Velocity * delta * (effectFriction + (Velocity.Length() * drag));
	}

	private void SteerCar(float delta) {
		const float offset = Mathf.Pi * 0.5f;

		Vector2 vector = new Vector2(0, wheels.wheelbase*0.5f).Rotated(Rotation);
		Vector2 front = Position - vector;
		Vector2 rear = Position + vector;

		front += Velocity.Rotated(wheels.steeringAngle) * delta;
		rear += Velocity * delta;
		
		heading = rear.DirectionTo(front);
		wheels.ToggleTireParticles(isSlipping);
		
		Velocity = Velocity.Lerp(heading * Velocity.Length(), GetTraction());
		Rotation = offset + heading.Angle();
	}

	private float GetTraction() {
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

	public void ResetCar() {
		Position = startingPosition;
		Rotation = 0f;
		/*
		if (hasReset) {
			Position = point;
			Rotation = Vector2.Up.AngleTo(normal);

		} else {
			Position = startingPosition;
			Rotation = 0f;
			hasReset = true;
		}*/

		Velocity = Vector2.Zero;
		acceleration = Vector2.Zero;
		steeringFraction = 0f;
		accelerationFraction = 0f;
		reward = 0f;
		totalReward = 0f;
		lateral = 0f;
		slip = 0f;

		needsRestart = false;
		isSlipping = false;
		onTrack = true;

		wheels.ResetWheels();
		env.FinishedRestarting(carID);
		ProcessMode = ProcessModeEnum.Inherit;
	}

	private bool CarNeedsReset() {
		return needsRestart
			|| MathF.Abs(normal.AngleTo(heading)) > (MathF.PI*0.85) 
			|| MathF.Abs(lateralOffset) > maxDistanceFromTrack;
	}

	private void CheckCollisions(KinematicCollision2D collision) {
		if (collision == null || needsRestart) return;

		Car other = (Car) collision.GetCollider();

		if (other.Velocity.LengthSquared() > Velocity.LengthSquared()) {
			other.reward -= (other.Velocity-Velocity).Length() / 1500f;

		} else {
			reward -= (Velocity-other.Velocity).Length() / 1500f;
		}
		env.FreezeCar(carID);
		env.FreezeCar(other.carID);
	}

	/*************************************
	*	Logic for features and rewards
	**************************************/
	private void CalculateFeatures() {
		(point, normal, direct) = track.NearestPoint(Position);

		orthonormal = normal.Rotated(0.5f * Mathf.Pi);				// Vector orthogonal to the normal vector
		lateralVector = (Position-point).Project(orthonormal);		// Relative position in the direction of the orthonormal
		side = orthonormal.Dot(lateralVector) < 0 ? -1f : 1f;		// Which side of the track the position is located
		lateralOffset = lateralVector.Length() * side;				// The distance between position and track

		direction = normal.Dot(Velocity) < 0 ? -1f : 1f;			// Whether the car is going forwards on the track

		// Velocity vector (normalized)
		features[0] = Velocity.X / 2800f;
		features[1] = Velocity.Y / 2800f;

		// Heading vector
		features[2] = heading.X;
		features[3] = heading.Y;

		features[4] = wheels.steeringAngle / wheels.maxWheelAngle;
		features[5] = slip;

		features[6] = Velocity.Project(normal).Length() * direction / 2800f; 			
		features[7] = Mathf.Min(lateralOffset / maxDistanceFromTrack, 1f); 

		// Direction normal
		features[8] = direct.X; 
		features[9] = direct.Y;

		// Proximity detection
		proximity.SetProximityFeatures(Position, Rotation);
		for (int i = 0; i < Proximity.maxObj; i++) {
			features[10+i] = proximity.features[i];
		}
	}

	public float[] GetFeatures() {
		return features;
	}

	private void SetReward(float delta) {
		float r = onTrack ? Mathf.Max(features[6] * delta, 0f) : -0.05f * delta;
		reward += r;
		totalReward += r;
	}

	// Sets the car's next action to take
	public void SetActions(float steering, float acceleration) {
		steeringFraction = Mathf.Clamp(steering, -1f, 1f);
		accelerationFraction = Mathf.Clamp(acceleration, -1f, 1f);
	}

	// Feature reporting
	public void SetDebugText() {
		debug.Text = $"\n velo_X: {features[0]:f2}\n velo_Y: {features[1]:f2}\n head_X: {features[2]:f2}\n head_Y: {features[3]:f2}\n whlAng: {features[4]:f2}\n slipFr: {features[5]:f2}\n trkSpd: {features[6]:f2}\n trkDst: {features[7]:f2}\n dirc_X: {features[8]:f2}\n dirc_Y: {features[9]:f2}\n\n prox_0: {features[10]:f2}\n prox_1: {features[11]:f2}\n prox_2: {features[12]:f2}\n prox_3: {features[13]:f2}\n prox_4: {features[14]:f2}\n prox_5: {features[15]:f2}\n prox_6: {features[16]:f2}\n prox_7: {features[17]:f2}\n\n Reward: {totalReward}";
	}
}
