using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class MirrorPickup : MonoBehaviour
{
    [Header("物品設定")]
    [Tooltip("攜帶式鏡子在背包裡的 ID 是多少？(請跟基座設定的 ID 一致，例如 2)")]
    public int mirrorWeaponID = 2;

    private bool isPlayerNear = false;
    private InventoryManager playerInventory;

    void Start()
    {
        // 確保碰撞體是觸發器模式
        GetComponent<BoxCollider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        // 檢查撞到我們的是不是玩家
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;

            // 記住玩家身上的背包系統
            playerInventory = other.transform.root.GetComponentInChildren<InventoryManager>();

            Debug.Log("<color=yellow>[撿拾系統]</color> 發現地上的鏡子！可以按 [F] 撿起了！");

            if (playerInventory == null)
            {
                Debug.LogError("<color=red>[嚴重錯誤]</color> 我找不到玩家身上的 InventoryManager 腳本！");
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            playerInventory = null; // 玩家離開時清空記憶
            Debug.Log("<color=grey>[撿拾系統]</color> 玩家離開了鏡子範圍。");
        }
    }

    void Update()
    {
        // 如果玩家在範圍內，且按下了 F 鍵
        if (isPlayerNear && Input.GetKeyDown(KeyCode.F))
        {
            if (playerInventory != null)
            {
                // 嘗試把鏡子塞進背包 (傳入我們設定好的鏡子 ID)
                if (playerInventory.AddItemToInventory(mirrorWeaponID, 1, 1))
                {
                    Debug.Log("<color=green>[撿拾系統]</color> 成功撿起攜帶式鏡子並收進背包！");

                    // 【關鍵】：塞成功了，直接把這個地上的道具徹底刪除！
                    // 這樣它就永遠消失了，玩家只能透過基座把鏡子拿出來
                    Destroy(gameObject);
                }
                else
                {
                    Debug.LogWarning("<color=orange>[撿拾系統]</color> 背包滿了或無法撿起！");
                }
            }
        }
    }
}