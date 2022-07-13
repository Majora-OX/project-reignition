using Godot;
using Project.Core;

namespace Project.Gameplay
{
	public class GameplayInterface : CanvasLayer
	{
		public static GameplayInterface instance;

		public override void _Ready()
		{
			instance = this;

			_hud = GetNode<Control>(hud);

			InitializeRingCounter();
			InitializeSoulGauge();
			InitializeHomingAttack();
		}


		#region Heads up display
		[Export]
		public NodePath hud;
		private Control _hud;

		[Export]
		public NodePath ringLabel;
		private Label _ringLabel;
		[Export]
		public NodePath maxRingLabel;
		private Label _maxRingLabel;
		[Export]
		public NodePath ringAnimator;
		private AnimationPlayer _ringAnimator;

		public int Score { get; private set; }

		public enum BonusTypes
		{
			Drift,
			Grinding,
			GrindStep,
		}

		public void AddBonus(BonusTypes bonusType)
		{
			//TODO Play point bonus animation, similar to Black Knight
		}

		public int RingCount { get; private set; }
		private int maxRingCount;

		private void InitializeRingCounter()
		{
			_ringLabel = GetNode<Label>(ringLabel);
			_maxRingLabel = GetNode<Label>(maxRingLabel);
			_ringAnimator = GetNode<AnimationPlayer>(ringAnimator);

			//TODO set ring count based on skills
			maxRingCount = StageSettings.instance.maxRingCount;
			_maxRingLabel.Text = maxRingCount.ToString("000");

			_ringLabel.Text = RingCount.ToString("000");

			if (RingCount == 0)
				_ringAnimator.Play("Ringless");
		}

		public void CollectRing(int amount)
		{
			//Play animation
			if (RingCount != maxRingCount)
				_ringAnimator.Play("CollectRing");

			RingCount += amount;
			RingCount = Mathf.Clamp(RingCount, 0, maxRingCount);

			if (amount > 0)
			{
				//Play SFX and particle effect
				CharacterController.instance.Animator.PlayRingParticleEffect();
			}

			_ringLabel.Text = RingCount.ToString("000");
		}

		public void LoseRings(int amount)
		{
			RingCount -= amount;
			if (RingCount <= 0)
				RingCount = 0;

			_ringLabel.Text = RingCount.ToString("000");

			_ringAnimator.Play("LoseRing");
			_ringAnimator.AnimationSetNext("LoseRing", RingCount == 0 ? "Ringless" : "RESET");
		}

		[Export]
		public NodePath soulGauge;
		private Control _soulGauge;
		private Control _soulGaugeRoot;
		[Export]
		public NodePath soulGaugeFill;
		private Control _soulGaugeFill;
		private Control _soulGaugeBackground;
		[Export]
		public NodePath soulGaugeAnimator;
		private AnimationPlayer _soulGaugeAnimator;
		private Tween _soulGaugeTweener;

		public bool IsSoulGaugeFull => soulPower == maxSoulPower;
		public bool IsSoulGaugeCharged { get; private set; }
		public bool IsSoulGaugeEmpty => soulPower == 0;
		private int soulPower;
		private int maxSoulPower;

		private float currentSoulPowerLength; //The length (in seconds) of soul skill

		private const int SOUL_GAUGE_BASE = 100; //Base size of soul gauge
		private const int SOUL_GAUGE_MAX = 300; //Max size of soul gauge
		private const int MINIMUM_SOUL_POWER = 50; //Minimum amount of soul power needed to use soul skills.

		private const int SOUL_GAUGE_FILL_OFFSET = 15;

		private void InitializeSoulGauge()
		{
			_soulGauge = GetNode<Control>(soulGauge);
			_soulGaugeRoot = _soulGauge.GetParent<Control>();
			_soulGaugeFill = GetNode<Control>(soulGaugeFill);
			_soulGaugeBackground = _soulGaugeFill.GetParent<Control>();
			_soulGaugeAnimator = GetNode<AnimationPlayer>(soulGaugeAnimator);
			_soulGaugeTweener = new Tween();
			AddChild(_soulGaugeTweener);

			//Soul Gauge increases by 20 every 5 levels, caps at 300 (level 50).
			float levelRatio = Mathf.Clamp(SaveManager.ActiveGameData.level, 0, 50) / 5f; //Current ratio (0 -> 10) compared to the soul gauge level cap (50)
			maxSoulPower = SOUL_GAUGE_BASE + Mathf.FloorToInt(levelRatio) * 20;

			int lerpFrom = Mathf.RoundToInt(_soulGaugeRoot.RectSize.y - _soulGauge.RectMinSize.y);
			levelRatio *= .1f; //Convert from 0 -> 10 to 0 -> 1
			_soulGauge.MarginTop = Mathf.Lerp(lerpFrom, 0, levelRatio); //Set the soul gauge to the correct size
			UpdateSoulFill(0f);
		}

		public void ModifySoulPearl(int amount)
		{
			soulPower += amount;
			soulPower = Mathf.Clamp(soulPower, 0, maxSoulPower);
			currentSoulPowerLength = soulPower / 60f;
			UpdateSoulFill((float)soulPower / maxSoulPower);

			if (amount > 0)
				UpdateSoulGaugeColor();
		}

		public void UpdateSoulGaugeColor()
		{
			if (!IsSoulGaugeCharged && soulPower >= MINIMUM_SOUL_POWER)
			{
				//Play animation
				IsSoulGaugeCharged = true;
				_soulGaugeAnimator.Play("Charged");
			}
			else if (IsSoulGaugeCharged && soulPower < MINIMUM_SOUL_POWER)
			{
				//Lost charge
				IsSoulGaugeCharged = false;
				_soulGaugeAnimator.Play("RESET"); //Revert to blue
			}
		}

		private void UpdateSoulFill(float t)
		{
			_soulGaugeTweener.StopAll();

			Vector2 end = Vector2.Down * Mathf.Lerp(_soulGaugeBackground.RectSize.y + SOUL_GAUGE_FILL_OFFSET, 0, t);
			_soulGaugeTweener.InterpolateProperty(_soulGaugeFill, "rect_position", _soulGaugeFill.RectPosition, end, .08f, Tween.TransitionType.Sine, Tween.EaseType.Out);
			_soulGaugeTweener.Start();
		}
		#endregion

		#region Homing Attack Reticle
		[Export]
		public NodePath homingReticle;
		private Node2D _homingReticle;
		[Export]
		public NodePath homingAnimator;
		private AnimationPlayer _homingAnimator;

		private void InitializeHomingAttack()
		{
			_homingReticle = GetNode<Node2D>(homingReticle);
			_homingAnimator = GetNode<AnimationPlayer>(homingAnimator);
		}

		public void DisableHomingReticle() => _homingAnimator.Play("disable");

		public void UpdateHomingReticle(Vector2 screenPosition, bool newTarget)
		{
			_homingReticle.Position = screenPosition;
			if (newTarget)
				_homingAnimator.Play("enable");
		}
		#endregion

		#region Camera Transition
		//Used for instant camera transitions
		[Export]
		public NodePath cameraTransition;
		private TextureRect _cameraTransition;
		[Export]
		public NodePath cameraTransitionAnimator;
		private AnimationPlayer _cameraTransitionAnimator;
		public void PlayCameraTransition(ImageTexture texture)
		{
			if (_cameraTransition == null)
			{
				_cameraTransition = GetNode<TextureRect>(cameraTransition);
				_cameraTransitionAnimator = GetNode<AnimationPlayer>(cameraTransitionAnimator);
			}

			_cameraTransition.Texture = texture;
			_cameraTransitionAnimator.Play("Transition");
		}
		#endregion
	}
}
