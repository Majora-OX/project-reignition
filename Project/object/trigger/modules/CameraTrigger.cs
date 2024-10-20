using Godot;
using Godot.Collections;

namespace Project.Gameplay.Triggers;

/// <summary>
/// Activates a <see cref="CameraSettingsResource"/>.
/// </summary>
[Tool] //Needed to draw distance blend endpoint, like drift triggers
public partial class CameraTrigger : StageTriggerModule
{
	#region Editor
	public override Array<Dictionary> _GetPropertyList()
	{
		Array<Dictionary> properties = new()
		{
		ExtensionMethods.CreateProperty("Additional Blend Settings/Blend By Distance", Variant.Type.Bool)
		};

		if (BlendByDistance)
		{
			properties.Add(ExtensionMethods.CreateProperty("Additional Blend Settings/Blend Distance", Variant.Type.Int, PropertyHint.Range, "1, 300"));
			properties.Add(ExtensionMethods.CreateProperty("Additional Blend Settings/Distance Blend Setting", Variant.Type.Object));
		}
		return properties;
	}
	public override Variant _Get(StringName property)
	{
		switch ((string)property)
		{
			case "Additional Blend Settings/Blend By Distance":
				return BlendByDistance;
			case "Additional Blend Settings/Blend Distance":
				return (int)blendDistance;
			case "Additional Blend Settings/Distance Blend Setting":
				return (CameraSettingsResource)DistanceBlendSetting;
		}
		return base._Get(property);
	}
	public override bool _Set(StringName property, Variant value)
	{
		switch ((string)property)
		{
			case "Additional Blend Settings/Blend By Distance":
				BlendByDistance = (bool)value;
				NotifyPropertyListChanged();
				break;
			case "Additional Blend Settings/Blend Distance":
				blendDistance = (int)value;
				break;
			case "Additional Blend Settings/Distance Blend Setting":
				DistanceBlendSetting = (CameraSettingsResource)value;
				break;
			default:
				return false;
		}
		return true;
	}
	#endregion

	/// <summary> How long the transition is (in seconds). Use a transition time of 0 to perform an instant cut. </summary>
	[Export(PropertyHint.Range, "0,5,0.1,or_greater")]
	public float transitionTime = 0.5f;
	/// <summary> Override to have a different blend time during deactivation. </summary>
	[Export(PropertyHint.Range, "-1,2,0.1")]
	public float deactivationTransitionTime = -1f;
	[Export] public CameraTransitionType transitionType;
	[Export] public bool enableInputBlending;

	/// <summary> Must be assigned to something. </summary>
	[Export] public CameraSettingsResource settings;
	/// <summary> Reference to the camera data that was being used when this trigger was entered. </summary>
	[Export] private CameraSettingsResource previousSettings;

	[ExportGroup("Transform Overrides")]
	/// <summary> Update positions and rotations every frame? </summary>
	[Export] public bool UpdateEveryFrame { get; private set; }
	[Export(PropertyHint.NodePathValidTypes, "Node3D")] private NodePath followObject;
	private Node3D _followObject;
	private Camera3D _referenceCamera;
	private bool IsOverridingCameraTransform => settings.copyPosition || settings.copyRotation || settings.copyRotation;

	private bool cachedPreviousSettings;
	private Vector3 previousStaticPosition;
	private Basis previousStaticRotation;
	private PlayerCameraController Camera => Player.Camera;

	public override void _Ready()
	{
		if (!IsOverridingCameraTransform)
			return;

		_followObject = followObject?.IsEmpty == false ? GetNode<Node3D>(followObject) : this;

		if (_followObject is Camera3D)
			_referenceCamera = _followObject as Camera3D;
	}

	public void UpdateStaticData(CameraBlendData data)
	{
		if (data.SettingsResource != settings || !IsOverridingCameraTransform) return;

		if (data.SettingsResource.copyPosition)
			data.Position = GlobalPosition;

		if (data.SettingsResource.copyRotation)
			data.RotationBasis = GlobalBasis;

		if (data.SettingsResource.copyFov && _referenceCamera != null)
			data.Fov = _referenceCamera.Fov;
	}

	public override void Activate()
	{
		if (settings == null)
		{
			GD.PrintErr($"{Name} doesn't have a CameraSettingResource attached!");
			return;
		}

		if (!cachedPreviousSettings)
		{
			previousSettings ??= Camera.ActiveSettings;
			previousStaticPosition = Camera.ActiveBlendData.Position; // Cache static position
			previousStaticRotation = Camera.ActiveBlendData.RotationBasis; // Cache static rotation
			cachedPreviousSettings = true;
		}

		if (Camera.ActiveSettings == settings &&
			!(settings.copyPosition || settings.copyRotation))
		{
			return;
		}

		Camera.UpdateCameraSettings(new()
		{
			BlendsOverDistance = false,
			BlendTime = transitionTime,
			SettingsResource = settings,
			TransitionType = transitionType,
			Trigger = this
		}, enableInputBlending);
   
		if (BlendByDistance) //If this a distance blend Trigger, add the second setting/camera for the first to blend with as well
		{
			Camera.UpdateCameraSettings(new()
			{
				DistanceBlendEndPoint = BlendFinishPoint,
				blendLength = blendDistance,
				BlendsOverDistance = BlendByDistance,
				SettingsResource = DistanceBlendSetting,
				IsCrossfadeEnabled = transitionType == TransitionType.Crossfade,
				Trigger = this
			}, enableInputBlending);
		}

		UpdateStaticData(Camera.ActiveBlendData);
	}

	public override void Deactivate()
	{
		if (previousSettings == null || settings == null)
			return;

		if (Camera.ActiveSettings != settings && Camera.ActiveSettings != DistanceBlendSetting)
			return; // Already overridden by a different trigger

		if (Player.IsTeleporting)
			return;

		Camera.UpdateCameraSettings(new()
		{
			BlendTime = Mathf.IsEqualApprox(deactivationTransitionTime, -1) ? transitionTime : deactivationTransitionTime,
			SettingsResource = previousSettings,
			Position = previousStaticPosition, // Restore cached static position
			RotationBasis = previousStaticRotation // Restore cached static rotation
		}, enableInputBlending);
	}
}
