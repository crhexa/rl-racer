using Godot;
using System;

public partial class TrackCurve : Path2D
{
	[Export] public bool clockwise = true;
	[Export] public float width = 450f;
	[Export(PropertyHint.ColorNoAlpha)] public Color innerColor = new("#0b991e");
	[Export(PropertyHint.ColorNoAlpha)] public Color outerColor = new("#333333");
	[Export(PropertyHint.ColorNoAlpha)] public Color borderColor = new(0, 0, 0);
	[Export] public float borderWidth = 24f;
	[Export] public Vector2 bottomLeftBound;
	[Export] public Vector2 topRightBound;
	[Export] public static int trackVision = 100;

	private Vector2[] curvePoly;
	private Vector2[] innerPoly;
	private Vector2[] outerPoly;
	private Vector2[] bakedPoints;
	private (Vector2 normal, Vector2 direct)[] bakedNormals;
	private KDTree pointTree;

	
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

		bakedPoints = Curve.GetBakedPoints();
		bakedNormals = BakeNormals(bakedPoints, bakedPoints.Length);
		GD.Print(bakedNormals.Length.ToString() + " normal vectors baked");

		pointTree = new(bakedPoints, bakedNormals, bottomLeftBound, topRightBound);
		GD.Print(pointTree.GetNumNodes().ToString() + " KDTree nodes created\n" + pointTree.GetTreeHeight().ToString() + " tree height");
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

	private static (Vector2 normal, Vector2 direct)[] BakeNormals(Vector2[] points, int num) {
		(Vector2 normal, Vector2 direct)[] normals = new (Vector2 normal, Vector2 direct)[num];

		for (int i = 0; i < num-1; i++) {
			normals[i] = (
				points[i].DirectionTo(points[i+1]),
				points[i].DirectionTo(points[(i+trackVision) % num])
			);
		}

		// If the last point overlaps with the first point
		if (points[0].IsEqualApprox(points[num-1])) {
			normals[num-1] = normals[0];

		} else {
			normals[num-1] = (
				points[num-1].DirectionTo(points[0]),
				points[num-1].DirectionTo(points[trackVision-1])
			);
		}
		return normals;
	}

	public (Vector2 point, Vector2 normal, Vector2 direct) NearestPoint(Vector2 position) {
		return pointTree.NearestNormal(position);
	}

	public bool IsOnTrack(Vector2 position) {
		return Geometry2D.IsPointInPolygon(position, outerPoly) && (!Geometry2D.IsPointInPolygon(position, innerPoly));
	}
}
