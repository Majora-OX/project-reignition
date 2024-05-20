using Godot;

namespace Project.Core
{
	/// <summary>
	/// Handles transitions and scene changes.
	/// The transition will play halfway, at which point a signal will be emitted, allowing for loading.
	/// Call <see cref="FinishTransition"/> to complete the transition.
	/// </summary>
	public partial class TransitionManager : Node
	{
		public static TransitionManager instance;
		/// <summary> Path to the main menu scene. </summary>
		public const string MENU_SCENE_PATH = "res://interface/menu/Menu.tscn";
		/// <summary> Path to story events. </summary>
		public const string EVENT_SCENE_PATH = "res://video/event/scene/Event";

		[Export]
		private ColorRect fade;
		[Export]
		private AnimationPlayer animator;
		[Export]
		private AnimationPlayer loadingAnimator;

		//Converts realtime seconds to a ratio for the animation player's speed. ALL ANIMATIONS MUST BE 1 SECOND LONG.
		public float ConvertToAnimatorSpeed(float seconds) => 1f / seconds;

		public override void _Ready()
		{
			instance = this;
		}

		#region Transition Types
		//Simple cut transition. During loading, everything will freeze temporarily.
		private void StartCut() => EmitSignal(SignalName.TransitionProcess);
		private void StartFade()
		{
			if (IsTransitionActive)
			{
				GD.Print("Transition is already active!");
				return;
			}

			if (CurrentTransitionData.loadAsynchronously)
				loadingAnimator.Play("show");

			IsTransitionActive = true;
			fade.Color = CurrentTransitionData.color;
			animator.Play("fade");

			if (CurrentTransitionData.inSpeed == 0)
			{
				animator.Seek(animator.CurrentAnimationLength, true);
				CallDeferred(MethodName.EmitSignal, SignalName.TransitionProcess);
			}
			else
			{
				animator.SpeedScale = ConvertToAnimatorSpeed(CurrentTransitionData.inSpeed);
				animator.Connect(AnimationPlayer.SignalName.AnimationFinished, new(instance, MethodName.TransitionLoading), (uint)ConnectFlags.OneShot);
			}
		}

		private void FinishFade()
		{
			if (CurrentTransitionData.outSpeed != 0)
				animator.SpeedScale = ConvertToAnimatorSpeed(CurrentTransitionData.outSpeed);

			if (CurrentTransitionData.loadAsynchronously)
				loadingAnimator.Play("hide");

			animator.PlayBackwards("fade");
			animator.Connect(AnimationPlayer.SignalName.AnimationFinished, new(instance, MethodName.TransitionFinished), (uint)ConnectFlags.OneShot);
		}
		#endregion

		private TransitionData CurrentTransitionData { get; set; }
		public static bool IsTransitionActive { get; set; }
		[Signal]
		public delegate void TransitionProcessEventHandler(); //Called in the middle of the transition (i.e. when the screen is completely black)
		[Signal]
		public delegate void TransitionFinishEventHandler(); //Called when the transition is finished
		private void TransitionLoading(string _) => EmitSignal(SignalName.TransitionProcess);
		private void TransitionFinished(string _)
		{
			IsTransitionActive = false;
			EmitSignal(SignalName.TransitionFinish);
		}

		public static void StartTransition(TransitionData data)
		{
			instance.animator.Play("RESET"); //Reset animator, just in case
			instance.animator.Advance(0);

			instance.CurrentTransitionData = data;

			if (data.inSpeed == 0 && data.outSpeed == 0)
			{
				instance.StartCut(); //Cut transition
				return;
			}

			instance.StartFade();
		}

		public static void FinishTransition() => instance.FinishFade();

		/// <summary> Queues a scene to load. Be sure to call StartTransition to actually transition to the scene. </summary>
		public static void QueueSceneChange(string scene)
		{
			instance.queuedScene = scene;

			if (!instance.IsConnected(SignalName.TransitionProcess, new(instance, MethodName.ApplySceneChange)))
				instance.Connect(SignalName.TransitionProcess, new(instance, MethodName.ApplySceneChange), (uint)ConnectFlags.OneShot);
		}

		private string queuedScene;
		private async void ApplySceneChange()
		{
			SoundManager.instance.CancelDialog(); //Cancel any active dialog
			if (string.IsNullOrEmpty(queuedScene)) //Reload the current scene
				GetTree().ReloadCurrentScene();
			else
			{
				if (CurrentTransitionData.loadAsynchronously)
				{
					ResourceLoader.LoadThreadedRequest(queuedScene);
					while (ResourceLoader.LoadThreadedGetStatus(queuedScene) == ResourceLoader.ThreadLoadStatus.InProgress) // Still loading
						await ToSignal(GetTree().CreateTimer(.1f), SceneTreeTimer.SignalName.Timeout); // Wait a bit
				}

				GetTree().ChangeSceneToFile(queuedScene);
			}

			queuedScene = string.Empty; //Clear queue
			FinishFade();
		}
	}

	public struct TransitionData
	{
		//Keep both speeds at 0 to perform simple cut transitions
		public float inSpeed;
		public float outSpeed;
		public Color color;
		public bool loadAsynchronously;
	}
}
