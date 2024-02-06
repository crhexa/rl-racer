using Godot;
using System;

public partial class Car : CharacterBody2D
{
	public const int M = 17;	// Feature space dimensions

	// Node related variables
	private Wheels wheels;
	private Label debug;
	private TrackCurve track;
	private RayCast2D[] rays;
	private float[] rayLengths;
	private Controller controller;
	
	// Editor variables
	[Export] public bool hasFocus = false;
	[Export] public float slipSpeed = 175f;
	[Export] public float enginePower = 1000f;
	[Export] public float brakePower = -10f;
	[Export] public float upFriction = -20f;
	[Export] public float sideFriction = -25f;
	[Export] public float drag = -0.0005f;
	[Export] public float maxDistanceFromTrack = 2000f;

	// State variables
	private Vector2 acceleration = Vector2.Zero;
	private Vector2 heading;
	private float lateral = 0f;
	private float slip = 0f;
	private bool isSlipping = false;
	private bool onTrack = true;

	// Agent related variables
	public float reward = 0f;
	private float[] features;
	private Vector2 orthonormal;
	private Vector2 lateralVector;
	private float side;
	private float direction;
	private float lateralOffset;
	private float steeringFraction = 0f;
	private float accelerationFraction = 0f;


	public override void _Ready()
	{
		wheels = GetNode<Wheels>("Wheels");
		controller = GetNode<Controller>("Controller");
		debug = hasFocus ? GetNode<Label>("Canvas/DebugText") : null;

		rays = new RayCast2D[7];
		rayLengths = new float[7];
		for (int i = 0; i < 7; i++) {
			rays[i] = GetNode<RayCast2D>($"RayCast{i+1}");
			rayLengths[i] = rays[i].TargetPosition.Length();
		}

		heading = Vector2.Up.Rotated(Rotation);
		features = new float[M];
		controller.init(this);
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float) delta;
		wheels.Steer(steeringFraction, dt);

		acceleration = GetAcceleration();
		if (acceleration == Vector2.Zero && Velocity.Length() < 5) {
			Velocity = Vector2.Zero;

		} else {
			acceleration += GetFriction(dt);
		}

		SteerCar(dt);
		Velocity += acceleration * dt;
		MoveAndSlide();

		SetReward(dt);
	}
	
	public void SetTrack(TrackCurve t) {
		track = t;
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
			effectFriction *= 1.5f;
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


	/*************************************
	*	Logic for features and rewards
	**************************************/
	public float[] GetFeatures() {
		(Vector2 point, Vector2 normal) = track.NearestPoint(Position);

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

		// Track normal
		features[8] = normal.X; 
		features[9] = normal.Y;

		for (int i = 0; i < 7; i++) {
			bool colliding = rays[i].IsColliding();
			features[i+10] = colliding
				? rays[i].GetCollisionPoint().DistanceTo(rays[i].GlobalTransform.Origin) / rayLengths[i] 
				: 1f;
		}
		return features;
	}

	private void SetReward(float delta) {
		reward += onTrack ? Mathf.Max(features[6] * delta, 0f) : 0f;
	}

	// Sets the car's next action to take
	public void SetActions(float steering, float acceleration) {
		steeringFraction = Mathf.Clamp(steering, -1f, 1f);
		accelerationFraction = Mathf.Clamp(acceleration, -1f, 1f);
	}

	// Feature reporting
	public void SetDebugText() {
		debug.Text = $"\n velo_X: {features[0]:f2}\n velo_Y: {features[1]:f2}\n head_X: {features[2]:f2}\n head_Y: {features[3]:f2}\n whlAng: {features[4]:f2}\n slipFr: {features[5]:f2}\n trkSpd: {features[6]:f2}\n trkDst: {features[7]:f2}\n norm_X: {features[8]:f2}\n norm_Y: {features[9]:f2}\n\n rayc_1: {features[10]:f2}\n rayc_2: {features[11]:f2}\n rayc_3: {features[12]:f2}\n rayc_4: {features[13]:f2}\n rayc_5: {features[14]:f2}\n rayc_6: {features[15]:f2}\n rayc_7: {features[16]:f2}\n\n Reward: {reward}";
	}
}
