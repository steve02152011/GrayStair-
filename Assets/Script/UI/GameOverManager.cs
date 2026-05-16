using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    // 單例模式：讓其他腳本（例如理智值腳本）可以輕鬆呼叫它
    public static GameOverManager Instance;

    [Header("死亡畫面設定")]
    [Tooltip("把剛剛做好的 GameOverScreen 拖過來")]
    public CanvasGroup gameOverCanvasGroup;

    [Tooltip("淡入需要花費幾秒鐘？")]
    public float fadeDuration = 2.0f;

    private bool isDead = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 遊戲開始時，確保死亡畫面是隱藏且完全透明的
        if (gameOverCanvasGroup != null)
        {
            gameOverCanvasGroup.alpha = 0f;
            gameOverCanvasGroup.interactable = false;
            gameOverCanvasGroup.blocksRaycasts = false;
        }
    }

    // ==========================================
    // 當理智值歸零時，呼叫這個函數！
    // ==========================================
    public void TriggerGameOver()
    {
        if (isDead) return; // 防呆：避免連續重複觸發死亡
        isDead = true;

        StartCoroutine(FadeInGameOverScreen());
    }

    private IEnumerator FadeInGameOverScreen()
    {
        float timer = 0f;

        // 1. 隨時間慢慢增加透明度 (達成：遊戲畫面淡出，死亡畫面淡入)
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            // Mathf.Lerp 會平滑地把數值從 0 變成 1
            gameOverCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            yield return null; // 等待下一個影格
        }

        // 確保最後透明度精準停在 1 (完全不透明)
        gameOverCanvasGroup.alpha = 1f;

        // 2. 開啟 UI 互動權限 (為了你之後要加的按鈕做準備)
        gameOverCanvasGroup.interactable = true;
        gameOverCanvasGroup.blocksRaycasts = true;

        // 3. 解除滑鼠鎖定，讓玩家可以點擊畫面上的按鈕
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 4. 暫停遊戲時間 (怪物停止移動、玩家無法操作)
        Time.timeScale = 0f;
    }

    public void RetryGame()
    {
      
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}