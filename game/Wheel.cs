using Godot;
using System;

public partial class Wheel : Node2D
{
	private GpuParticles2D particles;

	public override void _Ready()
	{
		particles = GetNode<GpuParticles2D>("Sprite/Particles");
	}

	public void ToggleTireParticles(bool emit) {
		particles.Emitting = emit;
	}
}
