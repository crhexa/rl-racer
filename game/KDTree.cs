using System;
using System.Collections;
using System.Linq;
using Godot;

public partial class KDTree
{
	public bool valid = true;

	private readonly KDNode root;
	private readonly Rect bounds;
	private readonly CompareX cmpx = new();
	private readonly CompareY cmpy = new();

	public class KDNode
	{
		public Vector2 vector;
		public Vector2 normal;

		public KDNode left;
		public KDNode right;

		public KDNode(Vector2 v, Vector2 n) {
			vector = v;
			normal = n;

			left = null;
			right = null;
		}
	}

	public readonly struct Rect
	{
		public readonly Vector2 low;
		public readonly Vector2 high;

		public Rect(Vector2 l, Vector2 h) {
			low = l;
			high = h;
		}

		public readonly float DistanceSquaredTo(Vector2 point) {
			float dx = MathF.Max(MathF.Max(low.X-point.X, point.X-high.X), 0f);
			float dy = MathF.Max(MathF.Max(low.Y-point.Y, point.Y-high.Y), 0f);

			return (dx * dx) + (dy + dy);
		}

		public readonly Rect CutLeft(Vector2 point, int axis) {
			point[1-axis] = high[1-axis];
			return new Rect(low, point);
		}

		public readonly Rect CutRight(Vector2 point, int axis) {
			point[1-axis] = low[1-axis];
			return new Rect(point, high);
		}

		public readonly bool Contains(Vector2 point) {
			return (point.X >= low.X) && (point.Y >= low.Y) && (point.X <= high.X) && (point.Y <= high.Y);
		}
	}
	
	// Sorting compare methods implementation
	public class CompareX : IComparer
	{
		int IComparer.Compare(object x, object y) {
			return ((Vector2) x).X.CompareTo(((Vector2) y).X);
		}
	}

	public class CompareY : IComparer
	{
		int IComparer.Compare(object x, object y) {
			return ((Vector2) x).Y.CompareTo(((Vector2) y).Y);
		}
	}

	private void SortByX(Vector2[] vectors, Vector2[] normals) {
		Array.Sort(vectors, normals, cmpx);
	}

	private void SortByY(Vector2[] vectors, Vector2[] normals) {
		Array.Sort(vectors, normals, cmpy);
	}

	// Constructor
	public KDTree(Vector2[] vectors, Vector2[] normals, Vector2 low, Vector2 high) {
		bounds = new Rect(low, high);
		root = BuildTree(vectors, normals, 0);
	}

	private KDNode BuildTree(Vector2[] vectors, Vector2[] normals, int depth) {
		if (vectors.Length == 0) {
			return null;
		}

		if (depth % 2 == 0) {
			SortByX(vectors, normals);
		} else {
			SortByY(vectors, normals);
		}

		int median = vectors.Length / 2;
		if (!bounds.Contains(vectors[median])) {
			GD.PushError("KDTree: found point out of bounds when building tree");
			return null;
		}

		KDNode newNode = new(vectors[median], normals[median])
		{
			left = BuildTree(vectors.Take(median).ToArray(), normals.Take(median).ToArray(), depth+1),
			right = BuildTree(vectors.Skip(median+1).ToArray(), normals.Skip(median+1).ToArray(), depth+1)
		};

		return newNode;
	}

	// Called by car code
	public (Vector2 point, Vector2 normal) NearestNormal(Vector2 vector) {
		KDNode nearest = NearestNode(root, vector, bounds, null, Mathf.Inf, 0).Item1;
		if (nearest == null) {
			GD.PrintErr("KDTree: failed to find nearest node");
			return (Vector2.Inf, Vector2.Inf);
		}
		return (nearest.vector, nearest.normal);
	}


	// Finds the node with the coordinates closest to the given vector
	private (KDNode, float) NearestNode(KDNode node, Vector2 point, Rect box, KDNode best, float bestDist, int depth) {
		if (node == null) {
			return (best, bestDist);
		}

		float currDist = point.DistanceSquaredTo(node.vector);
		if (currDist < bestDist) {
			best = node;
			bestDist = currDist;
		}

		int axis = depth % 2;
		Rect leftBox = box.CutLeft(node.vector, axis);
		Rect rightBox = box.CutRight(node.vector, axis);


		if (point[axis] < node.vector[axis]) {
			(best, bestDist) = NearestNode(node.left, point, leftBox, best, bestDist, depth+1);
			if (rightBox.DistanceSquaredTo(point) < bestDist) { // The best distance is within the right child's bounds
				(best, bestDist) = NearestNode(node.right, point, rightBox, best, bestDist, depth+1);
			}
			
		} else {
			(best, bestDist) = NearestNode(node.right, point, rightBox, best, bestDist, depth+1);
			if (leftBox.DistanceSquaredTo(point) < bestDist) { // The best distance is within the left child's bounds
				(best, bestDist) = NearestNode(node.left, point, leftBox, best, bestDist, depth+1);
			}
		}
		return (best, bestDist);
	}

	// Debug functions
	private static int CountNodes(KDNode node) {
		if (node == null) {
			return 0;
		}
		return 1 + CountNodes(node.left) + CountNodes(node.right);
	}

	private static int CountTreeHeight(KDNode node) {
		if (node == null) {
			return 0;
		}
		return 1 + Math.Max(CountTreeHeight(node.left), CountTreeHeight(node.right));
	}

	public int GetNumNodes() {
		return CountNodes(root);
	}

	public int GetTreeHeight() {
		return CountTreeHeight(root) - 1;
	}
}
