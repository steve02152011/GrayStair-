using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(BoxCollider))]
public class ReadableDocument : MonoBehaviour
{
    [Header("文件設定")]
    [Tooltip("請把這張紙『放大後要顯示的圖片 (Sprite)』拖曳到這裡")]
    public Sprite documentImage;

    [Header("UI 綁定 (請拖曳場景上的 UI 進來)")]
    [Tooltip("玩家靠近時顯示的『按 F 查看』提示文字或圖片")]
    public GameObject interactPrompt;

    [Tooltip("整個閱讀畫面的 UI 群組 (包含黑底、大圖、右下角提示字)")]
    public GameObject readingUIContainer;

    [Tooltip("用來顯示文件大圖的 Image 組件")]
    public Image documentDisplayImage;

    private bool isPlayerNear = false;
    private bool isReading = false;

    // ==========================================
    // 【新增】：用來記錄玩家的 FPSController，以便鎖死視角
    // ==========================================
    private FPSController playerController;

    void Awake()
    {
        if (interactPrompt != null) interactPrompt.SetActive(false);
        if (readingUIContainer != null) readingUIContainer.SetActive(false);
    }

    void Start()
    {
        GetComponent<BoxCollider>().isTrigger = true;

        // 遊戲一開始，自動去場景裡尋找玩家的 FPSController
        playerController = FindFirstObjectByType<FPSController>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
            if (!isReading && interactPrompt != null)
            {
                interactPrompt.SetActive(true);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            if (interactPrompt != null) interactPrompt.SetActive(false);

            if (isReading) CloseDocument();
        }
    }

    void Update()
    {
        if (isPlayerNear && Input.GetKeyDown(KeyCode.F))
        {
            if (!isReading)
            {
                OpenDocument();
            }
            else
            {
                CloseDocument();
            }
        }
    }

    private void OpenDocument()
    {
        isReading = true;

        if (interactPrompt != null) interactPrompt.SetActive(false);

        if (documentDisplayImage != null && documentImage != null)
        {
            documentDisplayImage.sprite = documentImage;
        }

        if (readingUIContainer != null) readingUIContainer.SetActive(true);

        Time.timeScale = 0f;

        // 【新增】：文件打開時，強制沒收玩家的移動與轉視角權限！
        if (playerController != null) playerController.canMove = false;
    }

    private void CloseDocument()
    {
        isReading = false;

        if (readingUIContainer != null) readingUIContainer.SetActive(false);

        if (isPlayerNear && interactPrompt != null) interactPrompt.SetActive(true);

        Time.timeScale = 1f;

        // 【新增】：文件關閉時，把移動與轉視角權限還給玩家！
        if (playerController != null) playerController.canMove = true;
    }
}