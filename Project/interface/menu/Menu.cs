using Godot;
using Godot.Collections;
using Project.Core;

namespace Project.Interface.Menu
{
	/// <summary>
	/// Base class for all menus.
	/// </summary>
	public class Menu : Control
	{
		public static Dictionary<string, int> memory = new Dictionary<string, int>(); //Use this for determining which menu is open/which option is selected

		[Export]
		public NodePath parentMenu;
		protected Menu _parentMenu;
		[Export]
		public Array<NodePath> submenus = new Array<NodePath>();
		protected Array<Menu> _submenus = new Array<Menu>(); //Also ensure the order of submenus is correct in the inspector higherarchy

		protected int SelectionHorizontal { get; set; }
		protected int SelectionVertical { get; set; }

		[Export]
		protected bool isReadingInputs; //Should we process this menu?
		protected InputManager.Controller Controller => InputManager.controller;

		public override void _Ready()
		{
			if(parentMenu != null)
				_parentMenu = GetNode<Menu>(parentMenu);

			for (int i = 0; i < submenus.Count; i++)
				_submenus.Add(GetNode<Menu>(submenus[i]));

			SetUp();
		}

		public override void _PhysicsProcess(float _)
		{
			if (!isReadingInputs || TransitionManager.IsTransitionActive) return;
			ProcessMenu();
		}

		protected virtual void SetUp() { }
		public void EnableProcessing() => isReadingInputs = true;
		public void DisableProcessing() => isReadingInputs = false;

		public new virtual void Show() => Visible = true;
		public new virtual void Hide() => Visible = false;

		//For animation signals
		public virtual void Show(string _) => Show();
		public virtual void Hide(string _) => Hide();

		protected virtual void ProcessMenu() { }
	}
}

