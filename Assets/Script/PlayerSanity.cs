using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// 【URP 專屬修改】：引入 URP 的渲染控制庫
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerSanity : MonoBehaviour
{
    [Header("理智度設定")]
    public float maxSanity = 100f;
    public float currentSanity;

    // 【新增】：吃藥後幾秒內會把理智平滑補完
    public float healDuration = 1.5f;
    private Coroutine healCoroutine;

    [Header("UI 設定")]
    public Image sanityFillImage;
    public float uiAnimationDuration = 0.5f;

    [Header("畫面表現 (URP 暈影)")]
    public Volume globalVolume;
    public float maxVignetteIntensity = 0.6f;

    [Header("畫面表現 (恐慌抖動)")]
    public HeadBobbing headBobbingScript;
    public float baseIdleBobAmount = 0.01f;
    public float maxIdleBobAmount = 0.1f;
    public float maxIdleBobSpeed = 6f;
    public float maxShakeMagnitude = 0.08f;

    private bool isInsane = false;
    private Coroutine sanityUIAnimation;
    private Vignette vignetteEffect;

    void Start()
    {
        currentSanity = maxSanity;
        if (sanityFillImage != null) sanityFillImage.fillAmount = 1f;

        if (globalVolume != null && globalVolume.profile != null)
        {
            globalVolume.profile.TryGet(out vignetteEffect);
        }

        UpdateSanityVisuals();
    }

    public void TakeDamage(float damageAmount)
    {
        if (isInsane) return;

        currentSanity -= damageAmount;
        currentSanity = Mathf.Max(currentSanity, 0f);

        Debug.Log($"<color=purple>[理智度]</color> 受到驚嚇！扣除 {damageAmount} 點，剩餘：{currentSanity}");

        UpdateSanityUI();
        UpdateSanityVisuals();

        if (currentSanity <= 0f) TriggerInsanity();
    }

    // 【核心修改】：將瞬間回血改成啟動平滑回血協程
    public void HealSanity(float healAmount)
    {
        if (isInsane) return;

        // 如果玩家連吃兩瓶，或者原本的回血還沒跑完，就先打斷它，重新計算新目標
        if (healCoroutine != null) StopCoroutine(healCoroutine);
        healCoroutine = StartCoroutine(SmoothRecoverSanity(healAmount));
    }

    // ================== 核心：平滑回血與視野漸明 (流暢優化版) ==================
    private IEnumerator SmoothRecoverSanity(float amount)
    {
        float startSanity = currentSanity;
        float targetSanity = Mathf.Min(currentSanity + amount, maxSanity);
        float time = 0f;

        // 如果被怪咬的瞬間 UI 還在扣血，強制打斷它，直接進入回血狀態
        if (sanityUIAnimation != null) StopCoroutine(sanityUIAnimation);

        while (time < healDuration)
        {
            time += Time.deltaTime;
            // 1. 算出基礎的時間比例 (0 到 1)
            float t = time / healDuration;
            // 2. 【細節魔法】：套用 Ease-Out Cubic 曲線 (先快後慢)
            // 讓藥效一開始衝得快，快滿的時候柔和地慢下來
            float easeT = 1f - Mathf.Pow(1f - t, 3);
            // 3. 使用流暢的比例來計算真實的理智值
            currentSanity = Mathf.Lerp(startSanity, targetSanity, easeT);

            // 每一幀即時更新 UI 血條
            if (sanityFillImage != null) sanityFillImage.fillAmount = currentSanity / maxSanity;

            // 每一幀更新畫面！暈影與發抖就會跟著血條「滑順地」慢慢褪去！
            UpdateSanityVisuals();

            yield return null;
        }

        // 確保最終數值精準無誤
        currentSanity = targetSanity;
        if (sanityFillImage != null) sanityFillImage.fillAmount = currentSanity / maxSanity;
        UpdateSanityVisuals();
    }

    private void UpdateSanityVisuals()
    {
        float sanityRatio = currentSanity / maxSanity;
        float insanityLevel = 1f - sanityRatio;

        if (vignetteEffect != null)
        {
            vignetteEffect.intensity.value = Mathf.Lerp(0f, maxVignetteIntensity, insanityLevel);
        }

        if (headBobbingScript != null)
        {
            headBobbingScript.idleBobAmount = Mathf.Lerp(baseIdleBobAmount, maxIdleBobAmount, insanityLevel);
            headBobbingScript.idleBobSpeed = Mathf.Lerp(2f, maxIdleBobSpeed, insanityLevel);
            headBobbingScript.currentShakeMagnitude = Mathf.Lerp(0f, maxShakeMagnitude, insanityLevel);
        }
    }

    private void UpdateSanityUI()
    {
        if (sanityFillImage == null) return;
        float targetFill = currentSanity / maxSanity;
        if (sanityUIAnimation != null) StopCoroutine(sanityUIAnimation);
        sanityUIAnimation = StartCoroutine(SmoothFillAmount(targetFill));
    }

    private IEnumerator SmoothFillAmount(float targetFill)
    {
        float startFill = sanityFillImage.fillAmount;
        float time = 0f;
        while (time < uiAnimationDuration)
        {
            time += Time.deltaTime;
            float t = time / uiAnimationDuration;
            float easeOutT = 1f - Mathf.Pow(1f - t, 3);
            sanityFillImage.fillAmount = Mathf.Lerp(startFill, targetFill, easeOutT);
            yield return null;
        }
        sanityFillImage.fillAmount = targetFill;
    }

    private void TriggerInsanity()
    {
        isInsane = true;
        Debug.Log("<color=red>[遊戲結束]</color> 玩家理智值歸零，徹底崩潰！");

        // 【關鍵結合點】：呼叫 GameOverManager 的死亡演出！
        if (GameOverManager.Instance != null)
        {
            GameOverManager.Instance.TriggerGameOver();
        }
        else
        {
            Debug.LogError("找不到 GameOverManager！請確定場景中有物件掛載了 GameOverManager 腳本。");
        }
    }
}