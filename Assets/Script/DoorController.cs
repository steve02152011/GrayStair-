using UnityEngine;

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

    // ==========================================
    // AI 自動感應設定
    // ==========================================
    [Header("AI 感應設定")]
    [Tooltip("這扇門是否允許被怪物打開？(請只在「玩家能按F打開的手動門」打勾，雷射機關門請保持取消)")]
    public bool allowAIAccess = false;

    [Tooltip("Dolo 靠近多近時會自動觸發開門")]
    public float aiDetectRadius = 3.5f;

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private Quaternion closedRotation;
    private Quaternion openRotation;

    public bool isOpen = false;

    // 記錄這扇門現在是不是「因為 Dolo 經過」才打開的
    private bool isOpenedByAI = false;

    void Start()
    {
        closedPosition = transform.localPosition;
        closedRotation = transform.localRotation;

        openPosition = closedPosition + openOffset;
        openRotation = closedRotation * Quaternion.Euler(openRotationOffset);
    }

    void Update()
    {
        // 決定速度
        float currentSpeed = openSpeed;
        if (speedMode == SpeedMode.SeparateSpeeds && !isOpen)
        {
            currentSpeed = closeSpeed;
        }

        // ==========================================
        // 每幀檢查 Dolo 是否靠近
        // ==========================================
        if (allowAIAccess)
        {
            CheckAIProximity();
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

    // ==========================================
    // 核心邏輯：Dolo 靠近自動開門 / 離開自動關門
    // ==========================================
    private void CheckAIProximity()
    {
        // 在門的周圍畫一個無形的圓圈，偵測裡面有沒有碰撞體
        Collider[] hits = Physics.OverlapSphere(transform.position, aiDetectRadius);
        bool aiNearby = false;
        Vector3 closestAIPos = Vector3.zero;

        foreach (var hit in hits)
        {
            // 【修改重點】：現在只會偵測 DoloAI，完全無視 Fonia
            if (hit.GetComponent<DoloAI>() != null)
            {
                aiNearby = true;
                closestAIPos = hit.transform.position;
                break;
            }
        }

        if (aiNearby)
        {
            // Dolo 在附近，且門是關著的 -> 嘗試開門
            if (!isOpen)
            {
                // 1. 判斷 Dolo 是在門的正面還是背面
                Vector3 localAIPos = transform.InverseTransformPoint(closestAIPos);
                bool isAIInFront = localAIPos.z > 0;

                // 2. 嚴格遵守單向門規則！如果 Dolo 從死路走過來，門死都不開
                if (isOneWayDoor)
                {
                    if (canOpenFromFrontOnly && !isAIInFront) return; // 擋住 Dolo
                    if (!canOpenFromFrontOnly && isAIInFront) return; // 擋住 Dolo
                }

                // 3. 通過驗證，Dolo 成功開門
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
            // Dolo 不在附近，如果這扇門剛才是由 Dolo 打開的，就自動關上它
            if (isOpen && isOpenedByAI)
            {
                isOpen = false;
                isOpenedByAI = false;
            }
        }
    }

    // 專門給玩家手動按 F 用的「智慧雙向開關」
    public void ToggleDoor(Vector3 interactorPosition)
    {
        Vector3 localPlayerPos = transform.InverseTransformPoint(interactorPosition);
        bool isPlayerInFront = localPlayerPos.z > 0;

        if (!isOpen && isOneWayDoor)
        {
            if (canOpenFromFrontOnly && !isPlayerInFront)
            {
                Debug.Log("<color=orange>[門]</color> 門從另一側被鎖上了，無法從這裡開啟！");
                return;
            }
            if (!canOpenFromFrontOnly && isPlayerInFront)
            {
                Debug.Log("<color=orange>[門]</color> 門從另一側被鎖上了，無法從這裡開啟！");
                return;
            }
        }

        isOpen = !isOpen;

        // 【細節】：如果玩家手動把門關上，我們要把 AI 標籤清掉，避免邏輯衝突
        if (!isOpen) isOpenedByAI = false;

        if (isOpen && doorType == DoorType.Rotating)
        {
            float multiplier = isPlayerInFront ? 1f : -1f;
            if (invertPushDirection) multiplier *= -1f;
            openRotation = closedRotation * Quaternion.Euler(openRotationOffset * multiplier);
        }
    }

    // 給雷射接收器 (LaserSensor) 用的標準開關
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

    // 【視覺輔助】：在 Unity 編輯器裡面畫出一顆紅色的球，讓你知道感應範圍有多大
    private void OnDrawGizmosSelected()
    {
        if (allowAIAccess)
        {
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawSphere(transform.position, aiDetectRadius);
        }
    }
}