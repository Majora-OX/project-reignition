using Godot;
using Godot.Collections;
using Project.Core;

namespace Project.Interface.Menus
{
	/// <summary>
	/// Base class for all menus.
	/// </summary>
	public partial class Menu : Control
	{
		public static Dictionary<MenuKeys, int> menuMemory = new Dictionary<MenuKeys, int>(); //Use this for determining which menu is open/which option is selected
		public enum MenuKeys
		{
			MainMenu,
			WorldSelect,
			LevelSelect,
			Max
		}
		public static void InitializeMemory()
		{
			for (int i = 0; i < (int)MenuKeys.Max; i++) //Initialize all memory to 0.
				menuMemory.Add((MenuKeys)i, 0);
		}

		[Export]
		protected Menu parentMenu;
		[Export]
		public Array<NodePath> submenus;
		protected Array<Menu> _submenus = new Array<Menu>(); //Also ensure the order of submenus is correct in the inspector hierarchy

		protected int HorizontalSelection { get; set; }
		protected int VerticalSelection { get; set; }

		[Export]
		protected bool isProcessing; //Should we process this menu?
		protected InputManager.Controller Controller => InputManager.controller;

		public override void _Ready()
		{
			if (submenus != null)
			{
				for (int i = 0; i < submenus.Count; i++) //Required due to inspector not allowing for custom classes
				{
					Menu submenu = GetNode<Menu>(submenus[i]);
					_submenus.Add(submenu);
					submenu.parentMenu = this;
				}
			}

			SetUp();

			if (isProcessing) //Enable isProcessing from the editor for quick single-menu debugging
				ShowMenu();
		}

		public override void _PhysicsProcess(double _)
		{
			if (!isProcessing || TransitionManager.IsTransitionActive) return;
			ProcessMenu();
		}

		protected virtual void SetUp() { }
		public void EnableProcessing() => isProcessing = true;
		public void DisableProcessing() => isProcessing = false;

		public virtual void ShowMenu() => Visible = true;
		public virtual void HideMenu() => Visible = false;

		public virtual void OpenParentMenu()
		{
			if (parentMenu == null)
			{
				GD.PrintErr($"No parent menu found for '{Name}'.");
				return;
			}

			parentMenu.ShowMenu();
		}
		public virtual void OpenSubmenu() => GD.PrintErr($"Submenus unimplemented on '{Name}'.");

		/// <summary> How long between each interval selection. </summary>
		protected float cursorSelectionTimer;
		protected const float SELECTION_INTERVAL = .2f;
		protected const float SELECTION_SCROLLING_INTERVAL = .1f;
		protected virtual void ProcessMenu()
		{
			if (Controller.jumpButton.wasPressed)
				Confirm();
			else if (Controller.actionButton.wasPressed)
				Cancel();
			else if (!Controller.IsHoldingNeutral)
			{
				if (Mathf.IsZeroApprox(cursorSelectionTimer))
					UpdateSelection();
				else
					cursorSelectionTimer = Mathf.MoveToward(cursorSelectionTimer, 0, PhysicsManager.physicsDelta);
			}
			else
			{
				cursorSelectionTimer = 0;
				isSelectionScrolling = false;
			}
		}

		/// <summary>
		/// Called when selection was changed.
		/// </summary>
		protected virtual void UpdateSelection() { }

		/// <summary>
		/// Call this to avoid selection changing too quickly.
		/// </summary>
		protected bool isSelectionScrolling;
		protected void StartSelectionTimer()
		{
			if (!isSelectionScrolling)
			{
				isSelectionScrolling = true;
				cursorSelectionTimer = SELECTION_INTERVAL;
			}
			else
				cursorSelectionTimer = SELECTION_SCROLLING_INTERVAL;
		}

		/// <summary>
		/// Called when the Confirmbutton is pressed.
		/// </summary>
		protected virtual void Confirm() { }

		/// <summary>
		/// Called when the Cancel button is pressed.
		/// </summary>
		protected virtual void Cancel() { }

		/// <summary>
		/// Wraps a selection around max selection
		/// </summary>
		protected int WrapSelection(int currentSelection, int maxSelection)
		{
			currentSelection %= maxSelection;
			if (currentSelection < 0)
				currentSelection += maxSelection;
			else if (currentSelection >= maxSelection)
				currentSelection -= maxSelection;

			return currentSelection;
		}
	}
}

