using Godot;
using System;

public partial class Controller : Node
{
	private Car agent;
	[Export] public bool reportFeatures = true;
	private Godot.Collections.Dictionary actionSpace;
	private Godot.Collections.Dictionary observationSpace;
	private Godot.Collections.Dictionary action;
	private Godot.Collections.Dictionary observation;
	private Godot.Collections.Array obs;

	public string heuristic = "human";
	public bool done = false;
	public int n_steps = 0;
	public bool needs_reset = false;

	public override void _Ready()
	{
		AddToGroup("AGENT");
	}

	public void init(Car car) {
		agent = car;

		actionSpace = new() {
			{ "move_action", new Godot.Collections.Dictionary {
				{ "size", 2 },
				{ "action_type", "continuous"}}
			}
		};

		observationSpace = new() {
			{ "obs", new Godot.Collections.Dictionary {
				{ "size", new Godot.Collections.Array() {Car.M} },
				{ "space", "box"}}
			}
		};

		action = new() {
			{ "move_action", new Godot.Collections.Array() {0f, 0f}}
		};

		obs = new();
		obs.Resize(Car.M);
		observation = new() {
			{ "obs", obs}
		};
	}


	public Godot.Collections.Dictionary get_obs() {
		float[] features = agent.GetFeatures();
		for (int i = 0; i < Car.M; i++) {
			obs[i] = features[i];
		}
		return observation;
	}

	public float get_reward() {
		return agent.reward;
	}

	public Godot.Collections.Dictionary get_action_space() {
		return actionSpace;
	}

	public Godot.Collections.Dictionary get_obs_space() {
		return observationSpace;
	}

	public void set_action(Godot.Collections.Dictionary action) {
		Godot.Collections.Array ma = (Godot.Collections.Array) action["move_action"];
		agent.SetActions((float) ma[0], (float) ma[1]);
	}

	public void set_heuristic(String h) {
		heuristic = h;
	}

	public bool get_done() {
		return done;
	}

	public void set_done_false() {
		done = false;
	}

	public void zero_reward() {
		agent.reward = 0f;
	}

	public void reset() {
		return;
	}

	public void reset_if_done() {
		if (done) reset();
	}
}
