using Godot;
using Godot.Collections;
using Project.Core;

namespace Project.Interface.Menus
{
	public partial class WorldSelect : Menu
	{
		[Export]
		private AnimationPlayer animator;

		[ExportSubgroup("Media Settings")]
		[Export]
		private VideoStreamPlayer primaryVideoPlayer;
		[Export]
		private VideoStreamPlayer secondaryVideoPlayer;
		[Export]
		private Array<StringName> videoStreamPaths = new Array<StringName>();
		private VideoStream[] videoStreams;
		private VideoStreamPlayer ActiveVideoPlayer { get; set; }
		private VideoStreamPlayer PreviousVideoPlayer { get; set; }

		private Color crossfadeColor;
		private float videoFadeFactor;
		private const float VIDEO_FADE_SPEED = 5.0f;

		[ExportSubgroup("Selection Settings")]
		[Export]
		private LevelDescription description;
		[Export]
		private Array<Rect2> levelSpriteRegions = new Array<Rect2>();
		[Export]
		private Array<string> levelDescriptionKeys = new Array<string>();
		[Export]
		private Array<NodePath> levelTextSprites = new Array<NodePath>();
		[Export]
		private Array<NodePath> levelGlowSprites = new Array<NodePath>();
		private readonly Array<Sprite2D> _levelTextSprites = new Array<Sprite2D>();
		private readonly Array<Sprite2D> _levelGlowSprites = new Array<Sprite2D>();

		protected override void SetUp()
		{
			for (int i = 0; i < levelTextSprites.Count; i++)
				_levelTextSprites.Add(GetNode<Sprite2D>(levelTextSprites[i]));

			for (int i = 0; i < levelGlowSprites.Count; i++)
				_levelGlowSprites.Add(GetNode<Sprite2D>(levelGlowSprites[i]));

			VerticalSelection = menuMemory[MemoryKeys.WorldSelect];
			videoStreams = new VideoStream[videoStreamPaths.Count];
			CallDeferred(MethodName.LoadVideos);
		}

		/// <summary>
		/// Load videos a frame after scene is set up to prevent crashing
		/// </summary>
		private void LoadVideos()
		{
			for (int i = 0; i < videoStreams.Length; i++)
				videoStreams[i] = ResourceLoader.Load<VideoStream>(videoStreamPaths[i]);
		}

		public override void _Process(double _)
		{
			if (primaryVideoPlayer.IsVisibleInTree())
			{
				UpdateVideo();
				if (ActiveVideoPlayer.Stream != null)
				{
					if (!ActiveVideoPlayer.IsPlaying())
						ActiveVideoPlayer.CallDeferred(VideoStreamPlayer.MethodName.Play);
					else
						videoFadeFactor = Mathf.MoveToward(videoFadeFactor, 1, VIDEO_FADE_SPEED * PhysicsManager.normalDelta);
				}

				ActiveVideoPlayer.Modulate = Colors.Transparent.Lerp(Colors.White, videoFadeFactor);

				if (PreviousVideoPlayer != null)
					PreviousVideoPlayer.Modulate = crossfadeColor.Lerp(Colors.Transparent, videoFadeFactor);
			}
		}

		protected override void UpdateSelection()
		{
			if (Controller.verticalAxis.sign != 0)
			{
				VerticalSelection = WrapSelection(VerticalSelection + Controller.verticalAxis.sign, (int)SaveManager.WorldEnum.Max);
				menuMemory[MemoryKeys.WorldSelect] = VerticalSelection;
				menuMemory[MemoryKeys.LevelSelect] = 0; //Reset level selection

				bool isScrollingUp = Controller.verticalAxis.sign < 0;
				int transitionIndex = WrapSelection(isScrollingUp ? VerticalSelection - 1 : VerticalSelection + 1, (int)SaveManager.WorldEnum.Max);
				UpdateSpriteRegion(3, transitionIndex); //Update level text

				animator.Play(isScrollingUp ? "scroll-up" : "scroll-down");
				animator.Seek(0.0, true);
				DisableProcessing();
			}
		}

		protected override void Confirm()
		{
			//World hasn't been unlocked
			if (!SaveManager.ActiveGameData.IsWorldUnlocked(VerticalSelection)) return;

			animator.Play("confirm");
		}

		protected override void Cancel() => animator.Play("cancel");
		public override void ShowMenu() => animator.Play("show");

		public override void OpenParentMenu()
		{
			base.OpenParentMenu();
			ActiveVideoPlayer.Stop();

			SaveManager.SaveGame();
			SaveManager.ActiveSaveSlotIndex = -1;
		}
		public override void OpenSubmenu()
		{
			SaveManager.ActiveGameData.lastPlayedWorld = (SaveManager.WorldEnum)VerticalSelection;
			_submenus[VerticalSelection].ShowMenu();
		}

		private void UpdateVideo()
		{
			//Don't change video?
			if (ActiveVideoPlayer != null && ActiveVideoPlayer.Stream == videoStreams[VerticalSelection]) return;
			if (!SaveManager.ActiveGameData.IsWorldUnlocked(VerticalSelection)) return; //World is locked
			if (!Mathf.IsZeroApprox(Controller.verticalAxis.value)) return; //Still scrolling

			if (ActiveVideoPlayer != null && ActiveVideoPlayer.IsPlaying())
			{
				videoFadeFactor = 0;
				crossfadeColor = ActiveVideoPlayer.Modulate;

				PreviousVideoPlayer = ActiveVideoPlayer;
				PreviousVideoPlayer.Paused = true;
			}

			ActiveVideoPlayer = ActiveVideoPlayer == secondaryVideoPlayer ? primaryVideoPlayer : secondaryVideoPlayer;
			ActiveVideoPlayer.Stream = videoStreams[VerticalSelection];
			ActiveVideoPlayer.Paused = false;
		}

		public void UpdateLevelText()
		{
			UpdateSpriteRegion(0, VerticalSelection - 1); //Top option
			UpdateSpriteRegion(1, VerticalSelection); //Center option
			UpdateSpriteRegion(2, VerticalSelection + 1); //Bottom option

			for (int i = 0; i < _levelGlowSprites.Count; i++) //Sync glow regions
				_levelGlowSprites[i].RegionRect = _levelTextSprites[i].RegionRect;
		}

		private void UpdateSpriteRegion(int spriteIndex, int selectionIndex)
		{
			selectionIndex = WrapSelection(selectionIndex, (int)SaveManager.WorldEnum.Max);
			if (!SaveManager.ActiveGameData.IsWorldUnlocked(selectionIndex)) //World isn't unlocked.
				selectionIndex = levelSpriteRegions.Count - 1;

			_levelTextSprites[spriteIndex].RegionRect = levelSpriteRegions[selectionIndex];

			if (spriteIndex == 1) //Updating primary selection
				description.SetText(levelDescriptionKeys[selectionIndex]);
		}
	}
}
