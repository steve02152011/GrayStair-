using UnityEngine;

public class MirrorPedestal : MonoBehaviour, IInteractable
{
    [Header("基座狀態")]
    [Tooltip("基座上現在有沒有放著鏡子？")]
    public bool hasMirror = false;

    [Header("物件綁定")]
    [Tooltip("放在基座上的『實體鏡子模型』 (請拖曳子物件進來)")]
    public GameObject mirrorModel;

    [Header("預覽視覺設定 (新增)")]
    [Tooltip("半透明的『預覽鏡子模型』 (請複製一個鏡子，換上半透明材質，拖曳進來)")]
    public GameObject ghostMirrorModel;

    [Tooltip("玩家要靠多近看著基座，才會顯示預覽？(請設定與你按F互動一樣的距離)")]
    public float previewDistance = 4f;

    [Header("背包與物品設定")]
    [Tooltip("攜帶式鏡子在 InventoryManager 的 allWeapons 陣列裡是第幾個？")]
    public int mirrorWeaponID = 2;

    private InventoryManager playerInventory;

    void Start()
    {
        if (mirrorModel != null)
        {
            mirrorModel.SetActive(hasMirror);
        }

        // 遊戲一開始，預覽模型一定是隱藏的
        if (ghostMirrorModel != null)
        {
            ghostMirrorModel.SetActive(false);
        }

        // 【已修復警告】：使用新版 Unity 效能更好的 API 來尋找背包系統
        playerInventory = FindFirstObjectByType<InventoryManager>();
    }

    void Update()
    {
        // 每幀處理預覽模型的顯示邏輯
        HandleGhostPreview();
    }

    private void HandleGhostPreview()
    {
        if (ghostMirrorModel == null || playerInventory == null) return;

        bool canShowPreview = false;

        // 只有當「基座是空的」且「玩家手上確實拿著鏡子」時，才需要檢查視線
        if (!hasMirror && playerInventory.GetCurrentItemID() == mirrorWeaponID)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                // 從攝影機正中央往前打出一道射線
                Ray ray = new Ray(mainCam.transform.position, mainCam.transform.forward);

                if (Physics.Raycast(ray, out RaycastHit hit, previewDistance))
                {
                    // 如果射線打到的正好是這個基座本身，代表玩家正在對準它！
                    if (hit.collider.gameObject == this.gameObject)
                    {
                        canShowPreview = true;
                    }
                }
            }
        }

        // 根據上面的檢查結果，決定要不要顯示預覽
        ghostMirrorModel.SetActive(canShowPreview);
    }

    public void OnInteract(Transform interactor)
    {
        if (playerInventory == null)
        {
            playerInventory = interactor.GetComponent<InventoryManager>();
        }

        if (playerInventory == null)
        {
            Debug.LogWarning("<color=red>[基座]</color> 找不到玩家的 InventoryManager！");
            return;
        }

        if (hasMirror)
        {
            bool added = playerInventory.AddItemToInventory(mirrorWeaponID, 1, 1);

            if (added)
            {
                hasMirror = false;
                if (mirrorModel != null) mirrorModel.SetActive(false);
                Debug.Log("<color=green>[基座]</color> 拿起了攜帶式鏡子！");
            }
        }
        else
        {
            int currentItemID = playerInventory.GetCurrentItemID();

            if (currentItemID == mirrorWeaponID)
            {
                playerInventory.ConsumeCurrentItem();

                hasMirror = true;
                if (mirrorModel != null) mirrorModel.SetActive(true);

                // 放上去的瞬間，強制把半透明預覽關掉
                if (ghostMirrorModel != null) ghostMirrorModel.SetActive(false);

                Debug.Log("<color=green>[基座]</color> 成功放下了攜帶式鏡子！");
            }
            else
            {
                Debug.Log("<color=orange>[基座]</color> 你必須拿著攜帶式鏡子才能放上去！");
            }
        }
    }
}