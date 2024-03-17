using System.Linq;
using Godot;

public partial class Proximity : Area2D
{
	private Car car;

	public float[] features;
	public const int maxObj = 8;


    public override void _Ready()
    {
        car = GetParent<Car>();
		features = new float[maxObj];
    }

    public void SetProximityFeatures(Vector2 origin, float angle) {
		if (!HasOverlappingBodies()) {
			for (int i = 0; i < maxObj; i++) {
				features[i] = 1f;
			}
		}
		Godot.Collections.Array<Node2D> overlapping = GetOverlappingBodies();
		Vector2[] nearby = new Vector2[overlapping.Count];

		for (int i = 0; i < overlapping.Count; i++) {
			nearby[i] = overlapping[i].Position - origin; // TODO: with respect to angle
		}

		// TODO: sort nearby by distance


		// TODO: quantize to proximity sectors


		// TODO: calculate adjusted distance
		
	}
}
