using Godot;
using Project.Core;

namespace Project.Gameplay
{
	[Tool]
	public class Launcher : Area
	{
		[Export]
		public LauncherType launcherType;
		public enum LauncherType
		{
			Spring,
			JumpPanel,
			Jump, //Use this to channel the power of Mario.
		}

		[Export]
		public float startingHeight; //Height at the beginning of the arc
		[Export]
		public float middleHeight; //Height at the highest point of the arc
		[Export]
		public float finalHeight; //Height at the end of the arc
		[Export]
		public float distance; //How far to travel

		[Export]
		public LaunchDirection launchDirection;
		public enum LaunchDirection
		{
			Forward,
			Up,
		}
		public Vector3 GetLaunchDirection()
		{
			if(launchDirection == LaunchDirection.Forward)
				return this.Forward();

			return this.Up();
		}

		public Vector3 InitialVelocity => GetLaunchDirection().Flatten().Normalized() * InitialHorizontalVelocity + Vector3.Up * InitialVerticalVelocity;

		public float InitialHorizontalVelocity => distance / TotalTravelTime;
		public float InitialVerticalVelocity => Mathf.Sqrt(-2 * GRAVITY * (middleHeight - startingHeight));
		public float FinalVerticalVelocity => GRAVITY * SecondHalfTime;
		public float FirstHalfTime => Mathf.Sqrt((-2 * middleHeight) / GRAVITY);
		public float SecondHalfTime => Mathf.Sqrt((-2 * (middleHeight - finalHeight)) / GRAVITY);
		public float TotalTravelTime => FirstHalfTime + SecondHalfTime;
		public Vector3 StartingPoint => GlobalTransform.origin + Vector3.Up * startingHeight;

		public const float GRAVITY = -24.0f;

		public Vector3 InterpolatePosition(float t)
		{
			Vector3 displacement = InitialVelocity * t + Vector3.Up * GRAVITY * t * t / 2f;
			return StartingPoint + displacement;
		}

		public virtual void Activate(Area a)
		{
			IsCharacterCentered = recenterSpeed == 0;
			Character.StartLauncher(this);
		}

		[Export]
		public int recenterSpeed; //How fast to recenter the character
		public bool IsCharacterCentered { get; private set; }
		private CharacterController Character => CharacterController.instance;

		public Vector3 RecenterCharacter()
		{
			Vector3 pos = Character.GlobalTransform.origin.MoveToward(StartingPoint, recenterSpeed * PhysicsManager.physicsDelta);
			IsCharacterCentered = pos.IsEqualApprox(StartingPoint);
			return pos;
		}

		public bool IsLauncherFinished(float t) => t + PhysicsManager.physicsDelta >= TotalTravelTime;
	}
}
