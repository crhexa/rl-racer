using Godot;
using System;

public partial class TrackCurve : Path2D
{
	[Export] public float width = 80f;
	[Export(PropertyHint.ColorNoAlpha)] public Color innerColor = new("#0b991e");
	[Export(PropertyHint.ColorNoAlpha)] public Color outerColor = new("#333333");
	[Export(PropertyHint.ColorNoAlpha)] public Color borderColor = new(0, 0, 0);
	[Export] public float borderWidth = 3f;

	private Vector2[] curvePoly;
	private Vector2[] innerPoly;
	private Vector2[] outerPoly;

	
	public override void _Ready()
	{

		if (!IsInstanceValid(Curve)) {
			GD.PushError("TrackCurve: missing curve");
			CallDeferred("free");
		}

		curvePoly = Curve.GetBakedPoints();
		innerPoly = CreatePolygon(curvePoly, -width);
		outerPoly = CreatePolygon(curvePoly, width);

		if (innerPoly == null || outerPoly == null) {
			GD.PushError("TrackCurve: unable to create polygon from path curve");
			CallDeferred("free");
		}
	}


	public override void _Draw()
	{
		DrawColoredPolygon(outerPoly, outerColor);
		DrawColoredPolygon(innerPoly, innerColor);
		DrawPolyline(outerPoly, borderColor, borderWidth, false);
		DrawPolyline(innerPoly, borderColor, borderWidth, false);
	}


	private static Vector2[] CreatePolygon(Vector2[] curve, float delta) {
		Godot.Collections.Array<Vector2[]> polygons = Geometry2D.OffsetPolygon(curve, delta, Geometry2D.PolyJoinType.Round);
		if (polygons.Count == 1) {
			return polygons[0];
		}
		return null;
	}
}
