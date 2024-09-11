using System.Collections.Generic;
using Godot;
using Godot.Collections;
using Project.Core;

namespace Project.Gameplay.Triggers;

public partial class CullingTrigger : StageTriggerModule, ITriggeredCheckpointListener
{
	[Signal]
	public delegate void ActivatedEventHandler();
	[Signal]
	public delegate void DeactivatedEventHandler();

	[Export]
	private bool startEnabled; // Generally things should start culled
	[Export]
	private bool saveVisibilityOnCheckpoint;
	[Export]
	private bool isStageVisuals;
	private bool isActive;
	private StageSettings Level => StageSettings.instance;
	[Export]
	private bool respawnOnActivation;
	private List<IPlayerRespawnedListener> respawnableNodes = [];

	public override void _EnterTree()
	{
		Visible = true;
		if (isStageVisuals)
			DebugManager.Instance.Connect(DebugManager.SignalName.StageCullingToggled, new Callable(this, MethodName.UpdateCullingState));
	}

	public override void _ExitTree()
	{
		if (isStageVisuals)
			DebugManager.Instance.Disconnect(DebugManager.SignalName.StageCullingToggled, new Callable(this, MethodName.UpdateCullingState));
	}

	public override void _Ready()
	{
		// Cache all children with a respawn method
		if (respawnOnActivation)
		{
			Array<Node> children = GetChildren(true);
			foreach (Node child in children)
			{
				if (child is IPlayerRespawnedListener listener)
					respawnableNodes.Add(listener);
			}
		}

		if (saveVisibilityOnCheckpoint)
		{
			//Cache starting checkpoint state
			visibleOnCheckpoint = startEnabled;

			//Listen for checkpoint signals
			Level.ConnectTriggeredCheckpointSignal(this);
			Level.ConnectRespawnSignal(this);
		}

		CallDeferred(MethodName.Respawn);
	}

	private bool visibleOnCheckpoint;
	public void TriggeredCheckpoint()
	{
		ProcessCheckpoint();
	}
	/// <summary> Saves the current visiblity. Called when the player passes a checkpoint. </summary>
	private void ProcessCheckpoint()
	{
		if (StageSettings.instance.LevelState == StageSettings.LevelStateEnum.Loading)
			visibleOnCheckpoint = startEnabled;
		else
			visibleOnCheckpoint = Visible;
	}

	public override void Respawn()
	{
		if (saveVisibilityOnCheckpoint)
		{
			if (visibleOnCheckpoint)
				Activate();
			else
				Deactivate();

			return;
		}

		// Disable the node on startup?
		if (startEnabled)
			Activate();
		else
			Deactivate();
	}

	public override void Activate()
	{
		isActive = true;
		UpdateCullingState();

		// Respawn everything
		if (respawnOnActivation)
		{
			foreach (IPlayerRespawnedListener node in respawnableNodes)
				node.Respawn();
		}

		EmitSignal(SignalName.Activated);
	}

	public override void Deactivate()
	{
		isActive = false;
		UpdateCullingState();
		EmitSignal(SignalName.Deactivated);
	}

	private void UpdateCullingState()
	{
		if (isStageVisuals && !DebugManager.IsStageCullingEnabled) // Treat as active
		{
			SetDeferred("visible", true);
			SetDeferred("process_mode", (long)ProcessModeEnum.Inherit);
			return;
		}

		SetDeferred("visible", isActive);
		SetDeferred("process_mode", (long)(isActive ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled));
	}
}