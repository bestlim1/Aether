using UnityEngine;

/// <summary>Small first-person controller with grounded movement and optional camera motion.</summary>
public sealed class AetherPlayer : MonoBehaviour
{
    public Camera View;
    public CharacterController Body;
    public float Yaw;
    public float Pitch;
    float verticalSpeed, stride, stepTimer;
    Vector3 velocity;
    public bool CanMove => AetherGame.I != null && AetherGame.I.Phase == GamePhase.Playing && AetherGame.I.ActivePuzzle < 0 && !AetherGame.I.JournalOpen && !AetherGame.I.MapOpen && !AetherGame.I.PhotoMode && !AetherGame.I.HasOpenSidePuzzle;

    public void Initialize()
    {
        Body = gameObject.AddComponent<CharacterController>();
        Body.height = 1.8f; Body.radius = .32f; Body.center = new Vector3(0, .9f, 0);
        Body.stepOffset = .32f; Body.slopeLimit = 48; Body.skinWidth = .04f;
        var eye = new GameObject("Explorer camera");
        eye.transform.SetParent(transform, false); eye.transform.localPosition = new Vector3(0, 1.66f, 0);
        View = eye.AddComponent<Camera>(); View.tag = "MainCamera";
        View.fieldOfView = 67; View.nearClipPlane = .08f; View.farClipPlane = 500;
        View.allowHDR = true; View.allowMSAA = true;
        eye.AddComponent<AudioListener>();
        Teleport(new Vector3(0, .12f, -10), 0, 0);
    }

    public void Teleport(Vector3 point, float yaw = 0, float pitch = 0)
    {
        Body.enabled = false; transform.position = point; Body.enabled = true;
        Yaw = yaw; Pitch = pitch; verticalSpeed = 0; velocity = Vector3.zero;
        transform.rotation = Quaternion.Euler(0, Yaw, 0);
        View.transform.localRotation = Quaternion.Euler(Pitch, 0, 0);
    }

    void Update()
    {
        if (!CanMove) return;
        bool acceptInput = Cursor.lockState == CursorLockMode.Locked && !AetherGame.I.IsSmokeMode;
        if (!acceptInput && !AetherGame.I.IsSmokeMode)
        {
            if (Input.GetMouseButtonDown(0)) AetherGame.I.RefreshCursor();
        }
        if (acceptInput)
        {
            Yaw += Input.GetAxisRaw("Mouse X") * AetherGame.I.Sensitivity;
            Pitch = Mathf.Clamp(Pitch - Input.GetAxisRaw("Mouse Y") * AetherGame.I.Sensitivity, -78, 78);
        }
        transform.rotation = Quaternion.Euler(0, Yaw, 0);
        Vector2 axes = acceptInput ? new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")) : Vector2.zero;
        axes = Vector2.ClampMagnitude(axes, 1);
        float speed = Input.GetKey(KeyCode.LeftShift) ? 6.4f : 4.1f;
        Vector3 desired = (transform.right * axes.x + transform.forward * axes.y) * speed;
        velocity = Vector3.MoveTowards(velocity, desired, 24 * Time.deltaTime);
        if (Body.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
        if (acceptInput && Body.isGrounded && Input.GetKeyDown(KeyCode.Space)) verticalSpeed = 5.3f;
        verticalSpeed -= 17 * Time.deltaTime;
        Body.Move((velocity + Vector3.up * verticalSpeed) * Time.deltaTime);
        bool walking = Body.isGrounded && axes.sqrMagnitude > .1f;
        stride += Time.deltaTime * (speed > 5 ? 12 : 9);
        float bob = walking && !AetherGame.I.ReducedMotion ? Mathf.Sin(stride) * .025f : 0;
        View.transform.localPosition = Vector3.Lerp(View.transform.localPosition, new Vector3(0, 1.66f + bob, 0), 12 * Time.deltaTime);
        View.transform.localRotation = Quaternion.Euler(Pitch, 0, 0);
        View.fieldOfView = Mathf.Lerp(View.fieldOfView, !AetherGame.I.ReducedMotion && walking && speed > 5 ? 71 : 67, 4 * Time.deltaTime);
        stepTimer -= Time.deltaTime;
        if (walking && stepTimer <= 0)
        {
            AetherAudio.I?.Step(); stepTimer = speed > 5 ? .34f : .48f;
        }
        if (transform.position.y < -8)
        {
            Teleport(AetherGame.I.LastSafePosition, Yaw, 0);
            AetherGame.I.Notify("정원의 빛이 당신을 안전한 곳으로 돌려보냈습니다.");
        }
    }
}
