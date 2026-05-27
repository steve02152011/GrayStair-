using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 6.0f;
    public float runSpeed = 10.0f;
    public float jumpHeight = 2.0f;
    public float gravity = -9.81f;

    [Header("Look Settings")]
    [Tooltip("請把 Player 底下的 CameraHolder 空物件拖進來")]
    public Transform cameraHolder;
    public float mouseSensitivity = 2.0f;
    public float lookXLimit = 85.0f;

    [Header("Interaction")]
    public PhysicsGrabber grabber;

    // ==========================================
    // 【修改】：針對「長音效」的腳步聲系統設定
    // ==========================================
    [Header("Audio Settings (腳步聲 - 長音效版)")]
    [Tooltip("請拖曳玩家身上的 AudioSource 進來")]
    public AudioSource footstepAudioSource;

    [Tooltip("走路的長音效 (可以塞 1~2 個讓它隨機挑選播放)")]
    public AudioClip[] walkSounds;

    [Tooltip("跑步的長音效 (可以塞 1~2 個讓它隨機挑選播放)")]
    public AudioClip[] runSounds;

    // 用來記錄前一幀是不是在跑步，如果狀態切換就要換音樂
    private bool wasRunning = false;
    // ==========================================

    private CharacterController characterController;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;

    [HideInInspector]
    public bool canMove = true;
    private bool isPaused = false;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraHolder == null)
        {
            Transform foundHolder = transform.Find("CameraHolder");
            if (foundHolder != null)
            {
                cameraHolder = foundHolder;
            }
            else
            {
                Debug.LogError("<color=red>[FPSController]</color> 找不到 CameraHolder！");
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

        if (isPaused) return;

        // --- 處理滑鼠旋轉 (Look Logic) ---
        if (canMove && cameraHolder != null)
        {
            if (grabber == null || !grabber.isInspecting)
            {
                float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
                float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

                rotationX -= mouseY;
                rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
                cameraHolder.localRotation = Quaternion.Euler(rotationX, 0, 0);
                transform.rotation *= Quaternion.Euler(0, mouseX, 0);
            }
        }

        // --- 處理移動 (Movement Logic) ---
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float curSpeedX = canMove ? (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Vertical") : 0;
        float curSpeedY = canMove ? (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Horizontal") : 0;

        float movementDirectionY = moveDirection.y;
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        // --- 處理跳躍與重力 (Jump & Gravity) ---
        if (characterController.isGrounded)
        {
            moveDirection.y = -0.5f;
            if (canMove && Input.GetButtonDown("Jump"))
            {
                moveDirection.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
            }
        }
        else
        {
            moveDirection.y = movementDirectionY + (gravity * Time.deltaTime);
        }

        characterController.Move(moveDirection * Time.deltaTime);

        // 每幀呼叫處理腳步聲的方法
        HandleFootsteps(isRunning);
    }

    // ==========================================
    // 【修改】：連續長音效播放邏輯核心
    // ==========================================
    private void HandleFootsteps(bool isRunning)
    {
        // 如果玩家不在地上、不能動、或遊戲暫停，就強制把聲音卡掉
        if (!characterController.isGrounded || !canMove || isPaused)
        {
            if (footstepAudioSource.isPlaying) footstepAudioSource.Stop();
            return;
        }

        // 取得玩家實際的「水平移動速度」
        Vector3 horizontalVelocity = new Vector3(characterController.velocity.x, 0, characterController.velocity.z);
        float currentSpeed = horizontalVelocity.magnitude;

        // 如果真的有在移動
        if (currentSpeed > 0.1f)
        {
            // 如果聲音沒在播，或者玩家從「走路」切換成「跑步」(或反過來)
            if (!footstepAudioSource.isPlaying || wasRunning != isRunning)
            {
                PlayContinuousSound(isRunning);
                wasRunning = isRunning;
            }
        }
        else
        {
            // 如果玩家停下來了，而且聲音還在播，立刻強制截斷停止！
            if (footstepAudioSource.isPlaying)
            {
                footstepAudioSource.Stop();
            }
        }
    }

    private void PlayContinuousSound(bool isRunning)
    {
        if (footstepAudioSource == null) return;

        // 決定要用走路陣列還是跑步陣列
        AudioClip[] clips = isRunning ? runSounds : walkSounds;
        if (clips.Length == 0) return;

        // 隨機抽一個音效
        int randomIndex = Random.Range(0, clips.Length);

        // 設定播放屬性並開始播放
        footstepAudioSource.clip = clips[randomIndex];
        footstepAudioSource.loop = true; // 【關鍵】：讓長音效自動無限循環
        footstepAudioSource.pitch = Random.Range(0.95f, 1.05f); // 微微改變音調，避免聽覺疲勞
        footstepAudioSource.Play();
    }
    // ==========================================

    void TogglePause()
    {
        isPaused = !isPaused;
        if (isPaused)
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            canMove = false;
        }
        else
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            canMove = true;
        }
    }
}