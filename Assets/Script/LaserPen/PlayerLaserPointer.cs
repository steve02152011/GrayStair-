using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(LineRenderer))]
public class PlayerLaserPointer : MonoBehaviour
{
    [Header("裝備狀態")]
    public bool hasPickedUp = false;

    [Header("發射點設定")]
    public Transform firePoint;

    [Header("雷射設定")]
    public float laserRange = 50f;
    // 【修改】：我們把麻煩的 laserHitMask 刪掉了！
    [Tooltip("手電筒最多可以折射/穿透幾次？")]
    public int maxBounces = 3;

    [Header("電池與 UI 設定")]
    public float maxBattery = 100f;
    public float currentBattery;
    public float drainRate = 10f;

    public GameObject uiContainer;
    public Image batteryFillImage;

    [Header("靈異干擾設定")]
    public float interferenceRadius = 10f;
    public float flickerSpeed = 0.05f;

    private LineRenderer lineRenderer;
    private bool isLaserToggledOn = false;
    private float flickerTimer = 0f;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

        // 【修復核心 1】：搬到 Awake 執行！
        // 確保在被背包系統強制關閉之前，就先把自己專屬的 UI 給藏起來
        if (!hasPickedUp && uiContainer != null)
        {
            uiContainer.SetActive(false);
        }
    }

    void Start()
    {
        lineRenderer.enabled = false;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        // 原本在這裡的隱藏 UI 程式碼已經搬到 Awake 了
    }

    void OnEnable()
    {
        if (uiContainer != null)
        {
            // 【修復核心 2】：強化防呆
            // 每次裝備這個道具時，確實檢查到底撿起來了沒
            if (hasPickedUp)
            {
                uiContainer.SetActive(true);
                UpdateBatteryUI();
            }
            else
            {
                uiContainer.SetActive(false);
            }
        }
    }

    void OnDisable()
    {
        isLaserToggledOn = false;
        if (lineRenderer != null) lineRenderer.enabled = false;
        if (uiContainer != null) uiContainer.SetActive(false);
    }

    void Update()
    {
        if (!hasPickedUp) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (currentBattery > 0)
            {
                isLaserToggledOn = !isLaserToggledOn;
            }
        }

        if (isLaserToggledOn && currentBattery > 0)
        {
            HandleLaserActive();
        }
        else
        {
            if (currentBattery <= 0) isLaserToggledOn = false;
            TurnOffLaser();
        }

        UpdateBatteryUI();
    }

    private void HandleLaserActive()
    {
        currentBattery -= drainRate * Time.deltaTime;
        currentBattery = Mathf.Max(currentBattery, 0f);

        bool isInterfered = CheckForCloneInterference();

        if (isInterfered)
        {
            flickerTimer += Time.deltaTime;
            if (flickerTimer >= flickerSpeed)
            {
                flickerTimer = 0f;
                lineRenderer.enabled = Random.value > 0.5f;
            }
        }
        else
        {
            lineRenderer.enabled = true;
        }

        if (lineRenderer.enabled)
        {
            ShootLaser();
        }
    }

    private void TurnOffLaser()
    {
        if (lineRenderer != null) lineRenderer.enabled = false;
    }

    private void ShootLaser()
    {
        List<Vector3> laserPoints = new List<Vector3>();

        Vector3 currentPos = (firePoint != null) ? firePoint.position : transform.position;
        Vector3 currentDir = (firePoint != null) ? firePoint.forward : transform.forward;

        laserPoints.Add(currentPos);
        float remainingDistance = laserRange;

        for (int i = 0; i < maxBounces; i++)
        {
            // 【修改】：不使用 Mask，直接射出雷射！
            if (Physics.Raycast(currentPos, currentDir, out RaycastHit hit, remainingDistance))
            {
                // ==========================================
                // 【終極防呆密技】：自動無視玩家自己
                // 檢查打到的東西是不是跟雷射槍屬於同一個身體 (Player)
                // ==========================================
                if (hit.collider.transform.root == this.transform.root)
                {
                    // 把射線起點稍微往前推一點 (穿進玩家身體)，然後繼續射
                    // Unity 的物理引擎會自動忽略起點在內部的碰撞體
                    currentPos = hit.point + currentDir * 0.05f;
                    remainingDistance -= hit.distance;
                    i--; // 抵銷這回合的迴圈，不扣折射次數
                    continue;
                }

                laserPoints.Add(hit.point);
                remainingDistance -= hit.distance;

                ILaserReceiver receiver = hit.collider.GetComponent<ILaserReceiver>();
                if (receiver != null)
                {
                    bool continueLaser = receiver.ProcessLaser(
                        hit.point, hit.normal, currentDir, hit.collider,
                        ref remainingDistance, laserPoints,
                        out currentPos, out currentDir
                    );

                    if (!continueLaser) break;
                }
                else
                {
                    break; // 打到一般牆壁，乖乖停下來
                }
            }
            else
            {
                laserPoints.Add(currentPos + currentDir * remainingDistance);
                break;
            }

            if (remainingDistance <= 0) break;
        }

        lineRenderer.positionCount = laserPoints.Count;
        lineRenderer.SetPositions(laserPoints.ToArray());
    }

    private bool CheckForCloneInterference()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, interferenceRadius);
        foreach (Collider hit in hits)
        {
            if (hit.GetComponent<FoniaClone>() != null) return true;
        }
        return false;
    }

    private void UpdateBatteryUI()
    {
        if (batteryFillImage != null)
        {
            batteryFillImage.fillAmount = currentBattery / maxBattery;
        }
    }

    public void EquipLaser()
    {
        hasPickedUp = true;
        currentBattery = maxBattery;
        isLaserToggledOn = false;

        if (uiContainer != null) uiContainer.SetActive(true);
        UpdateBatteryUI();
    }
}