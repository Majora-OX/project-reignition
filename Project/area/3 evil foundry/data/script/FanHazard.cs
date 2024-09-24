using System;
using Godot;

namespace Project.Gameplay.Hazards;

public partial class FanHazard : Hazard
{
	[Export]
	// private float spinSpeed;
	public float RotationsPerSecond;
	[Export]
	private bool playSFX;

	[ExportGroup("Components")]
	[Export]
	private NodePath root;
	private Node3D _root;
	[Export]
	private NodePath sfx;
	private AudioStreamPlayer3D _sfx;
	private float _rotationSpeed = 0;
	private Action<double> Animate;
	public override void _Ready()
	{
		if (RotationsPerSecond != 0)
		{
			_rotationSpeed = RotationsPerSecond * Mathf.Tau;
			Animate = Rotate;
		}
		else
		{
			Animate = Disabled;
		}
		_root = GetNode<Node3D>(root);
		_sfx = GetNode<AudioStreamPlayer3D>(sfx);

		if (playSFX)
			_sfx.Play();
	}
	private void Disabled(double delta) { }
	private void Rotate(double delta)
	{
		_root.Rotation = _root.Rotation + Vector3.Forward * (_rotationSpeed * (float)delta % Mathf.Tau);
	}
	public override void _PhysicsProcess(double delta)
	{
		Animate(delta);
		ProcessCollision();
	}
}
