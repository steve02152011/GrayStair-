using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class DoloActivationTrigger : MonoBehaviour
{
    [Header("目標怪物")]
    [Tooltip("請把場景中的 Dolo 拖曳到這個欄位")]
    public DoloAI targetDolo;

    [Header("設定")]
    [Tooltip("打勾代表觸發一次之後，這個觸發區就會自我銷毀，避免重複觸發")]
    public bool triggerOnlyOnce = true;

    void Start()
    {
        // 確保這個物件的 Collider 一定是觸發器模式
        GetComponent<BoxCollider>().isTrigger = true;

        // 遊戲一開始時，強制先將 Dolo 的大腦 (腳本) 關閉，讓牠在原地待機
        if (targetDolo != null)
        {
            targetDolo.enabled = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // 檢查走進範圍的是不是玩家
        if (other.CompareTag("Player"))
        {
            if (targetDolo != null && !targetDolo.enabled)
            {
                // 喚醒 Dolo 的大腦！
                targetDolo.enabled = true;
                Debug.Log("<color=red>[事件]</color> 玩家進入觸發區，Dolo 已甦醒開始行動！");

                // 如果設定為只觸發一次，啟動完就立刻把這個隱形觸發區刪除
                if (triggerOnlyOnce)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}