using Godot;
using Project.Core;

namespace Project.Gameplay
{
	public class CameraController : Spatial
	{
		[Export]
		public NodePath calculationRoot;
		private Spatial _calculationRoot; //Responsible for pitch rotation
		[Export]
		public NodePath calculationGimbal;
		private Spatial _calculationGimbal; //Responsible for yaw rotation
		[Export]
		public NodePath cameraRoot;
		private Spatial _cameraRoot;
		[Export]
		public NodePath cameraGimbal;
		private Spatial _cameraGimbal;
		[Export]
		public NodePath camera;
		private Camera _camera;

		private CharacterController Character => CharacterController.instance;
		private CharacterPathFollower PlayerPathFollower => Character.PathFollower;

		public Vector2 ConvertToScreenSpace(Vector3 worldSpace) => _camera.UnprojectPosition(worldSpace);
		public bool IsOnScreen(Vector3 worldSpace)
		{
			Vector2 screenPosition = ConvertToScreenSpace(worldSpace);
			if (screenPosition.x / 1920f > 1f || screenPosition.x < 0f) return false;
			if (screenPosition.y / 1080f > 1f || screenPosition.y < 0f) return false;
			return true;
		}	

		public override void _Ready()
		{
			//ResetFlag = true;
			_calculationRoot = GetNode<Spatial>(calculationRoot);
			_calculationGimbal = GetNode<Spatial>(calculationGimbal);

			_cameraRoot = GetNode<Spatial>(cameraRoot);
			_cameraGimbal = GetNode<Spatial>(cameraGimbal);
			_camera = GetNode<Camera>(camera);

			Character.Camera = this;
		}

		public override void _ExitTree()
		{
			//Fix memory leak
			currentSettings.Dispose();
			previousSettings.Dispose();
		}

		public void UpdateCamera()
		{
			UpdateGameplayCamera();

			if (!OS.IsDebugBuild())
				return;

			UpdateFreeCam();
		}

		#region Settings
		public CameraSettingsResource targetSettings; //End lerp here
		private readonly CameraSettingsResource previousSettings = new CameraSettingsResource(); //Start lerping here
		private readonly CameraSettingsResource currentSettings = new CameraSettingsResource(); //Apply transforms based on this
		public void SetCameraData(CameraSettingsResource data, float blendTime = .2f, bool useCrossfade = false)
		{
			transitionSpeed = blendTime;
			transitionTime = transitionTimeStepped = 0f; //Reset transition timers
			previousSettings.CopyFrom(currentSettings);

			previousSettings.viewAngle.x = currentRotation.x;
			previousSettings.viewAngle.y = currentRotation.y;
			previousTilt = currentTilt;
			previousStrafe = currentStrafe;
			previousYawTracking = currentYawTracking;
			previousPitchTracking = currentPitchTracking;

			targetSettings = data;

			//Copy modes
			currentSettings.heightMode = data.heightMode;
			currentSettings.followMode = data.followMode;
			currentSettings.strafeMode = data.strafeMode;
			currentSettings.tiltMode = data.tiltMode;
			currentSettings.viewPosition = data.viewPosition;

			if (useCrossfade) //Crossfade transition
			{
				Image img = _calculationGimbal.GetViewport().GetTexture().GetData();
				var tex = new ImageTexture();
				tex.CreateFromImage(img);
				HeadsUpDisplay.instance.PlayCameraTransition(tex);
			}
		}

		private float transitionTime; //Ratio (from 0 -> 1) of transition that has been completed
		private float transitionTimeStepped; //Smoothstepped transition time
		private float transitionSpeed; //Speed of transition
		private void UpdateActiveSettings() //Calculate the active camera settings
		{
			if (targetSettings == null) return; //ERROR! No data set.

			if (Mathf.IsZeroApprox(transitionSpeed))
				ResetFlag = true;

			if (ResetFlag)
				transitionTime = 1f;
			else
				transitionTime = Mathf.MoveToward(transitionTime, 1f, (1f / transitionSpeed) * PhysicsManager.physicsDelta);
			transitionTimeStepped = Mathf.SmoothStep(0, 1f, transitionTime);

			currentSettings.distance = Mathf.Lerp(previousSettings.distance, targetSettings.distance, transitionTime);
			currentSettings.height = Mathf.Lerp(previousSettings.height, targetSettings.height, transitionTime);
			currentSettings.heightTrackingStrength = Mathf.Lerp(previousSettings.heightTrackingStrength, targetSettings.heightTrackingStrength, transitionTime);
		}
		#endregion

		#region Gameplay Camera
		public bool ResetFlag { get; set; } //Set to true to skip smoothing

		private void UpdateGameplayCamera()
		{
			if (Character.PathFollower.ActivePath == null) return; //Uninitialized

			UpdateActiveSettings();
			UpdateBasePosition();

			if (!freeCamEnabled) //Apply transform
			{
				_cameraRoot.GlobalTransform = _calculationRoot.GlobalTransform;
				_cameraGimbal.GlobalTransform = _calculationGimbal.GlobalTransform;
			}

			if (ResetFlag) //Reset flag
				ResetFlag = false;
		}

		private float currentDistance;
		private float currentHeight;
		private float heightVelocity;
		private float distanceVelocity;
		private Vector2 currentRotation;
		private Vector2 rotationVelocity;
		private const float POSITION_SMOOTHING = .2f;
		private const float ROTATION_SMOOTHING = .06f;
		private void UpdateBasePosition()
		{
			UpdateOffsets(); //Height, distance
			UpdateRotation();
			UpdatePosition();
		}

		private Vector3 GetBasePosition(CameraSettingsResource resource)
		{
			if (resource.followMode == CameraSettingsResource.FollowMode.Static)
				return resource.viewPosition;
			else if (resource.followMode == CameraSettingsResource.FollowMode.Pathfollower)
				return PlayerPathFollower.GlobalTranslation;
			else
				return Character.GlobalTranslation - GetHeightDirection(resource.heightMode) * PlayerPathFollower.LocalPlayerPosition.y;
		}

		private Vector3 GetStrafeOffset(CameraSettingsResource resource)
		{
			if (resource.strafeMode == CameraSettingsResource.StrafeMode.Move)
			{
				float playerOffset = PlayerPathFollower.LocalPlayerPosition.x;
				if (Mathf.Abs(playerOffset) < resource.strafeDeadzone)
					playerOffset = 0f;
				else
					playerOffset = (Mathf.Abs(playerOffset) - resource.strafeDeadzone) * Mathf.Sign(playerOffset);

				Vector3 strafeOffset = Character.PathFollower.StrafeDirection * -playerOffset * resource.strafeTrackingStrength;
				return strafeOffset;
			}

			return Vector3.Zero;
		}

		private Vector3 GetHeightDirection(CameraSettingsResource.HeightMode mode)
		{
			Vector3 vector = Vector3.Up;
			if (mode == CameraSettingsResource.HeightMode.PathFollower)
				vector = PlayerPathFollower.Up();
			else if (mode == CameraSettingsResource.HeightMode.Camera)
				vector = _calculationGimbal.Up();

			return vector;
		}

		private Vector3 GetHeightOffset(CameraSettingsResource resource) => GetHeightDirection(resource.heightMode) * PlayerPathFollower.LocalPlayerPosition.y * resource.heightTrackingStrength;

		private void UpdateOffsets()
		{
			if (ResetFlag)
			{
				currentHeight = currentSettings.height;
				currentDistance = currentSettings.distance;
				heightVelocity = distanceVelocity = 0f;
			}
			else
			{
				currentHeight = ExtensionMethods.SmoothDamp(currentHeight, currentSettings.height, ref heightVelocity, POSITION_SMOOTHING);
				currentDistance = ExtensionMethods.SmoothDamp(currentDistance, currentSettings.distance, ref distanceVelocity, POSITION_SMOOTHING);
			}
		}

		private float previousTilt;
		private float currentTilt;
		private float previousYawTracking;
		private float currentYawTracking;
		private float previousPitchTracking;
		private float currentPitchTracking;
		private void UpdateRotation()
		{
			Vector3 forwardDirection;
			if (targetSettings.IsStaticCamera)
				forwardDirection = Character.CenterPosition - targetSettings.viewPosition;
			else
			{
				forwardDirection = PlayerPathFollower.Forward();

				if (Mathf.Abs(PlayerPathFollower.Forward().y) > .9f) //Fix for running up walls
					forwardDirection = Mathf.Sign(PlayerPathFollower.Forward().y) * PlayerPathFollower.Down();
			}

			Vector3 forwardFlattened = forwardDirection.RemoveVertical().Normalized();
			Vector3 rightDirection = forwardFlattened.Cross(Vector3.Up);

			Vector2 targetRotation = Vector2.Zero;
			if (targetSettings.yawMode == CameraSettingsResource.OverrideMode.Override)
				targetRotation.y = Mathf.LerpAngle(previousSettings.viewAngle.y, Mathf.Deg2Rad(targetSettings.viewAngle.y), transitionTime);
			else
			{
				targetRotation.y = forwardFlattened.SignedAngleTo(Vector3.Forward, Vector3.Up) + Mathf.Deg2Rad(targetSettings.viewAngle.y);

				if (targetSettings.yawMode == CameraSettingsResource.OverrideMode.Add)
					targetRotation.y += Mathf.Deg2Rad(targetSettings.viewAngle.y);
			}

			if (targetSettings.pitchMode == CameraSettingsResource.OverrideMode.Override)
				targetRotation.x = Mathf.LerpAngle(previousSettings.viewAngle.x, Mathf.Deg2Rad(targetSettings.viewAngle.x), transitionTime);
			else
			{
				float cachedRotation = _calculationRoot.GlobalRotation.y;
				_calculationRoot.GlobalRotation = Vector3.Down * targetRotation.y; //Temporarily apply the rotation so pitch calculation can be correct
				targetRotation.x = forwardFlattened.SignedAngleTo(PlayerPathFollower.Forward(), rightDirection) + Mathf.Deg2Rad(targetSettings.viewAngle.x);

				//Reset rotation
				_calculationRoot.GlobalRotation = Vector3.Up * cachedRotation;
				if (targetSettings.pitchMode == CameraSettingsResource.OverrideMode.Add)
					targetRotation.x += Mathf.Deg2Rad(targetSettings.viewAngle.x);
			}

			targetRotation.x = Mathf.LerpAngle(previousSettings.viewAngle.x, targetRotation.x, transitionTimeStepped);
			targetRotation.y = Mathf.LerpAngle(previousSettings.viewAngle.y, targetRotation.y, transitionTimeStepped);

			if (ResetFlag)
				currentRotation = targetRotation;
			else //Smooth out rotations
			{
				currentRotation = new Vector2(ExtensionMethods.SmoothDampAngle(currentRotation.x, targetRotation.x, ref rotationVelocity.x, ROTATION_SMOOTHING),
					ExtensionMethods.SmoothDampAngle(currentRotation.y, targetRotation.y, ref rotationVelocity.y, ROTATION_SMOOTHING));
			}

			Vector2 pitchVector = new Vector2(currentDistance, -PlayerPathFollower.LocalPlayerPosition.y);
			if (targetSettings.IsStaticCamera)
			{
				pitchVector.x = forwardDirection.Flatten().Length();
				pitchVector.y = -forwardDirection.y;
			}
			currentPitchTracking = Mathf.Lerp(previousPitchTracking, pitchVector.AngleTo(Vector2.Right) * (1 - currentSettings.heightTrackingStrength), transitionTimeStepped);

			//Apply Rotation
			_calculationRoot.GlobalRotation = Vector3.Down * currentRotation.y;
			_calculationGimbal.Rotation = Vector3.Zero;

			//Calculate tilt
			Vector3 tiltVector = PlayerPathFollower.Right().Rotated(Vector3.Up, forwardFlattened.SignedAngleTo(Vector3.Forward, Vector3.Up));
			float tiltAmount = tiltVector.SignedAngleTo(Vector3.Left, Vector3.Forward);
			float targetTilt = targetSettings.tiltMode == CameraSettingsResource.TiltMode.Disable ? 0 : tiltAmount;
			currentTilt = Mathf.LerpAngle(previousTilt, targetTilt, transitionTimeStepped);
			_calculationGimbal.RotateObjectLocal(Vector3.Back, currentTilt);

			float targetYawTracking = 0f;
			if (targetSettings.strafeMode == CameraSettingsResource.StrafeMode.Rotate && Mathf.Abs(PlayerPathFollower.LocalPlayerPosition.x) > 1f) //Track left/right
			{
				Vector2 v = new Vector2((Mathf.Abs(PlayerPathFollower.LocalPlayerPosition.x) - 1f) * Mathf.Sign(PlayerPathFollower.LocalPlayerPosition.x), currentDistance);
				targetYawTracking = v.AngleTo(Vector2.Down);
			}

			currentYawTracking = Mathf.LerpAngle(currentYawTracking, targetYawTracking, .2f);

			_calculationGimbal.RotateObjectLocal(Vector3.Up, Mathf.Lerp(previousYawTracking, currentYawTracking, transitionTimeStepped));
			_calculationGimbal.RotateObjectLocal(Vector3.Right, currentRotation.x + currentPitchTracking);
		}

		private Vector3 previousStrafe;
		private Vector3 currentStrafe;
		private Vector3 strafeVelocity;
		private const float STRAFE_SMOOTHING = .1f;
		private void UpdatePosition()
		{
			//Calculate positions
			Vector3 targetPosition = GetBasePosition(previousSettings).LinearInterpolate(GetBasePosition(targetSettings), transitionTimeStepped);

			//Distance
			Vector3 offset = _calculationRoot.Forward().Rotated(_calculationRoot.Right(), currentRotation.x);
			targetPosition += offset * currentDistance;

			//Height
			offset = offset.Rotated(_calculationRoot.Right(), Mathf.Pi * .5f - Mathf.Deg2Rad(currentSettings.viewAngle.x));
			targetPosition -= offset * currentHeight;

			//Height Tracking
			offset = GetHeightOffset(previousSettings).LinearInterpolate(GetHeightOffset(targetSettings), transitionTimeStepped);
			targetPosition += offset;

			//Update Strafe
			currentStrafe = currentStrafe.SmoothDamp(previousStrafe.LinearInterpolate(GetStrafeOffset(targetSettings), transitionTimeStepped), ref strafeVelocity, STRAFE_SMOOTHING);
			targetPosition += currentStrafe;

			_calculationRoot.GlobalTranslation = targetPosition;
		}
		#endregion

		#region Free Cam
		private float freecamMovespeed = 20;
		private const float MOUSE_SENSITIVITY = .2f;

		private bool freeCamEnabled;
		private bool freeCamRotating;

		private void UpdateFreeCam()
		{
			if (Input.IsKeyPressed((int)KeyList.R))
			{
				freeCamEnabled = freeCamRotating = false;
				_calculationRoot.Visible = false;
				ResetFlag = true;
			}

			freeCamRotating = Input.IsMouseButtonPressed((int)ButtonList.Left);
			if (freeCamRotating)
			{
				freeCamEnabled = true;
				_calculationRoot.Visible = true;
				Input.MouseMode = Input.MouseModeEnum.Captured;
			}
			else
				Input.MouseMode = Input.MouseModeEnum.Visible;

			if (!freeCamEnabled) return;

			float targetMoveSpeed = freecamMovespeed;

			if (Input.IsKeyPressed((int)KeyList.Shift))
				targetMoveSpeed *= 2;
			else if (Input.IsKeyPressed((int)KeyList.Control))
				targetMoveSpeed *= .5f;

			if (Input.IsKeyPressed((int)KeyList.E))
				_cameraRoot.GlobalTranslate(_camera.Up() * targetMoveSpeed * PhysicsManager.physicsDelta);
			if (Input.IsKeyPressed((int)KeyList.Q))
				_cameraRoot.GlobalTranslate(_camera.Down() * targetMoveSpeed * PhysicsManager.physicsDelta);
			if (Input.IsKeyPressed((int)KeyList.W))
				_cameraRoot.GlobalTranslate(_camera.Back() * targetMoveSpeed * PhysicsManager.physicsDelta);
			if (Input.IsKeyPressed((int)KeyList.S))
				_cameraRoot.GlobalTranslate(_camera.Forward() * targetMoveSpeed * PhysicsManager.physicsDelta);
			if (Input.IsKeyPressed((int)KeyList.D))
				_cameraRoot.GlobalTranslate(_camera.Right() * targetMoveSpeed * PhysicsManager.physicsDelta);
			if (Input.IsKeyPressed((int)KeyList.A))
				_cameraRoot.GlobalTranslate(_camera.Left() * targetMoveSpeed * PhysicsManager.physicsDelta);
		}

		public override void _Input(InputEvent e)
		{
			if (!freeCamRotating)
			{
				e.Dispose(); //Be sure to dispose events! Otherwise a memory leak may occour.
				return;
			}

			if (e is InputEventMouseMotion)
			{
				_cameraRoot.RotateY(Mathf.Deg2Rad(-(e as InputEventMouseMotion).Relative.x) * MOUSE_SENSITIVITY);
				_cameraGimbal.RotateX(Mathf.Deg2Rad(-(e as InputEventMouseMotion).Relative.y) * MOUSE_SENSITIVITY);
				_cameraGimbal.RotationDegrees = Vector3.Right * Mathf.Clamp(_cameraGimbal.RotationDegrees.x, -90, 90);
			}
			else if (e is InputEventMouseButton emb)
			{
				if (emb.IsPressed())
				{
					if (emb.ButtonIndex == (int)ButtonList.WheelUp)
					{
						freecamMovespeed += 5;
						GD.Print($"Free cam Speed set to {freecamMovespeed}.");
					}
					if (emb.ButtonIndex == (int)ButtonList.WheelDown)
					{
						freecamMovespeed -= 5;
						if (freecamMovespeed < 0)
							freecamMovespeed = 0;
						GD.Print($"Free cam Speed set to {freecamMovespeed}.");
					}
				}
			}

			e.Dispose();
		}
		#endregion
	}
}