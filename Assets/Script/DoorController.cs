using UnityEngine;
using UnityEngine.AI;

public class DoorController : MonoBehaviour
{
    public enum DoorType { Sliding, Rotating }
    public enum SpeedMode { SameSpeed, SeparateSpeeds }

    [Header("門的類型設定")]
    public DoorType doorType = DoorType.Sliding;

    [Header("滑動門設定 (如果選 Sliding)")]
    public Vector3 openOffset = new Vector3(0, 3, 0);

    [Header("旋轉門設定 (如果選 Rotating)")]
    public Vector3 openRotationOffset = new Vector3(0, 90, 0);

    [Tooltip("如果發現門總是往你臉上拍，就把這個打勾反轉方向！")]
    public bool invertPushDirection = false;

    [Header("單向門設定")]
    [Tooltip("打勾代表這是一扇單向門，只能從特定的一面打開")]
    public bool isOneWayDoor = false;

    [Tooltip("打勾: 只能從正面打開 / 取消打勾: 只能從背面打開 (請在遊戲中測試判定)")]
    public bool canOpenFromFrontOnly = true;

    [Header("速度設定")]
    [Tooltip("SameSpeed(A選項): 開關速度一樣 \nSeparateSpeeds(B選項): 可獨立調整開門與關門速度")]
    public SpeedMode speedMode = SpeedMode.SameSpeed;
    public float openSpeed = 5.0f;
    public float closeSpeed = 5.0f;

    [Header("AI 感應設定")]
    [Tooltip("這扇門是否允許被怪物打開？(請只在「玩家能按F打開的手動門」打勾，雷射機關門請保持取消)")]
    public bool allowAIAccess = false;

    [Tooltip("Dolo 靠近多近時會自動觸發開門")]
    public float aiDetectRadius = 3.5f;

    // ==========================================
    // 【新增】：門的音效設定
    // ==========================================
    [Header("音效設定")]
    [Tooltip("用來播放門開關聲音的發聲器 (請掛載 AudioSource 並拖曳進來)")]
    public AudioSource doorAudioSource;

    [Tooltip("門打開時的音效")]
    public AudioClip openSound;

    [Tooltip("門關上時的音效")]
    public AudioClip closeSound;

    [Tooltip("門上鎖/推不開時的音效 (例如：喀喀聲)")]
    public AudioClip lockedSound;
    // ==========================================

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private Quaternion closedRotation;
    private Quaternion openRotation;

    public bool isOpen = false;
    private bool isOpenedByAI = false;

    private NavMeshObstacle navObstacle;
    private bool lastOpenState;

    void Start()
    {
        closedPosition = transform.localPosition;
        closedRotation = transform.localRotation;

        openPosition = closedPosition + openOffset;
        openRotation = closedRotation * Quaternion.Euler(openRotationOffset);

        navObstacle = GetComponent<NavMeshObstacle>();
        lastOpenState = isOpen;

        if (navObstacle != null)
        {
            navObstacle.enabled = !isOpen;
        }
    }

    void Update()
    {
        float currentSpeed = openSpeed;
        if (speedMode == SpeedMode.SeparateSpeeds && !isOpen)
        {
            currentSpeed = closeSpeed;
        }

        if (allowAIAccess)
        {
            CheckAIProximity();
        }

        // ==========================================
        // 【修改】：自動同步路障狀態與「開關門音效」
        // ==========================================
        if (isOpen != lastOpenState)
        {
            // 處理 AI 路障
            if (navObstacle != null)
            {
                navObstacle.enabled = !isOpen;
            }

            // 【新增】：處理開關門音效
            if (doorAudioSource != null)
            {
                if (isOpen && openSound != null)
                {
                    doorAudioSource.PlayOneShot(openSound);
                }
                else if (!isOpen && closeSound != null)
                {
                    doorAudioSource.PlayOneShot(closeSound);
                }
            }

            lastOpenState = isOpen;
        }

        // 執行開關門位移與旋轉
        if (doorType == DoorType.Sliding)
        {
            Vector3 targetPos = isOpen ? openPosition : closedPosition;
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * currentSpeed);
        }
        else if (doorType == DoorType.Rotating)
        {
            Quaternion targetRot = isOpen ? openRotation : closedRotation;
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * currentSpeed);
        }
    }

    private void CheckAIProximity()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, aiDetectRadius);
        bool aiNearby = false;
        Vector3 closestAIPos = Vector3.zero;

        foreach (var hit in hits)
        {
            if (hit.GetComponent<DoloAI>() != null)
            {
                aiNearby = true;
                closestAIPos = hit.transform.position;
                break;
            }
        }

        if (aiNearby)
        {
            if (!isOpen)
            {
                Vector3 localAIPos = transform.InverseTransformPoint(closestAIPos);
                bool isAIInFront = localAIPos.z > 0;

                if (isOneWayDoor)
                {
                    if (canOpenFromFrontOnly && !isAIInFront) return;
                    if (!canOpenFromFrontOnly && isAIInFront) return;
                }

                isOpenedByAI = true;
                isOpen = true;

                if (doorType == DoorType.Rotating)
                {
                    float multiplier = isAIInFront ? 1f : -1f;
                    if (invertPushDirection) multiplier *= -1f;
                    openRotation = closedRotation * Quaternion.Euler(openRotationOffset * multiplier);
                }
            }
        }
        else
        {
            if (isOpen && isOpenedByAI)
            {
                isOpen = false;
                isOpenedByAI = false;
            }
        }
    }

    public void ToggleDoor(Vector3 interactorPosition)
    {
        Vector3 localPlayerPos = transform.InverseTransformPoint(interactorPosition);
        bool isPlayerInFront = localPlayerPos.z > 0;

        // 【修改】：處理單向門鎖住時的音效
        if (!isOpen && isOneWayDoor)
        {
            if ((canOpenFromFrontOnly && !isPlayerInFront) || (!canOpenFromFrontOnly && isPlayerInFront))
            {
                // 播放上鎖/推不開的聲音
                if (doorAudioSource != null && lockedSound != null)
                {
                    doorAudioSource.PlayOneShot(lockedSound);
                }
                Debug.Log("<color=orange>[門]</color> 門從另一側被鎖上了，無法從這裡開啟！");
                return;
            }
        }

        isOpen = !isOpen;

        if (!isOpen) isOpenedByAI = false;

        if (isOpen && doorType == DoorType.Rotating)
        {
            float multiplier = isPlayerInFront ? 1f : -1f;
            if (invertPushDirection) multiplier *= -1f;
            openRotation = closedRotation * Quaternion.Euler(openRotationOffset * multiplier);
        }
    }

    public void SetDoorState(bool state)
    {
        if (isOpen == state) return;

        isOpen = state;
        if (!isOpen) isOpenedByAI = false;

        if (isOpen && doorType == DoorType.Rotating)
        {
            openRotation = closedRotation * Quaternion.Euler(openRotationOffset);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (allowAIAccess)
        {
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawSphere(transform.position, aiDetectRadius);
        }
    }
}