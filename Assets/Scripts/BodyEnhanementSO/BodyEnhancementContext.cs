using UnityEngine;

public class BodyEnhancementContext
{
    public PlayerFpsController FpsController { get; }
    public CharacterController CharacterController { get; }
    public FPSInput Input { get; }
    public Transform CameraHolder { get; }
    public Transform Orientation { get; }

    public BodyEnhancementContext(
        PlayerFpsController fpsController,
        CharacterController characterController,
        FPSInput input,
        Transform cameraHolder,
        Transform orientation)
    {
        FpsController = fpsController;
        CharacterController = characterController;
        Input = input;
        CameraHolder = cameraHolder;
        Orientation = orientation;
    }
}