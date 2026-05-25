using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class PillPickup : MonoBehaviour
{
    // ==========================================
    // 【新增】：UI 綁定欄位
    // ==========================================
    [Header("UI 綁定")]
    [Tooltip("玩家靠近時顯示的『按 F 撿起藥瓶』提示群組或文字")]
    public GameObject interactPrompt;

    private bool isPlayerNear = false;
    private InventoryManager playerInventory;

    void Awake()
    {
        // 遊戲一開始先確保提示字是關閉的，避免出現幽靈 UI
        if (interactPrompt != null)
        {
            interactPrompt.SetActive(false);
        }
    }

    void Start()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
            playerInventory = other.transform.root.GetComponentInChildren<InventoryManager>();

            // 【新增】：玩家靠近，打開提示字
            if (interactPrompt != null)
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
            playerInventory = null;

            // 【新增】：玩家離開，關閉提示字
            if (interactPrompt != null)
            {
                interactPrompt.SetActive(false);
            }
        }
    }

    void Update()
    {
        if (isPlayerNear && Input.GetKeyDown(KeyCode.F))
        {
            if (playerInventory != null)
            {
                // 【關鍵參數】：代表這是一號武器(理智藥)，給 1 瓶，最大可疊加 3 瓶！
                if (playerInventory.AddItemToInventory(1, 1, 3))
                {
                    Debug.Log("<color=green>[撿拾系統]</color> 獲得理智藥！");

                    // 【關鍵防呆】：東西被撿走並銷毀前，一定要先把提示字關掉！
                    if (interactPrompt != null)
                    {
                        interactPrompt.SetActive(false);
                    }

                    Destroy(gameObject);
                }
            }
        }
    }
}