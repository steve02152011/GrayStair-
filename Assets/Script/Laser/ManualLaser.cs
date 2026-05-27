using UnityEngine;
using System.Collections;

public class ManualLaser : MonoBehaviour, IInteractable
{
    [Header("連動設定")]
    [Tooltip("把你要控制的雷射發射器拖曳到這裡！")]
    public LaserEmitter targetLaser;

    [Header("計時設定 (新增)")]
    [Tooltip("按下按鈕後，雷射會持續發射幾秒？")]
    public float activeDuration = 5.0f;

    [Header("視覺回饋 (選填)")]
    [Tooltip("如果按鈕本身會變色，把掛有 MaterialSwitcher 的物件拖來這裡")]
    public MaterialSwitcher visualFeedback;

    // ==========================================
    // 【新增】：按鈕與機關音效設定
    // ==========================================
    [Header("音效設定")]
    [Tooltip("用來播放按鈕與機關聲音的 AudioSource (請掛在按鈕物件上並拖曳進來)")]
    public AudioSource buttonAudioSource;

    [Tooltip("按下按鈕瞬間的音效 (例如：喀喀聲)")]
    public AudioClip pressSound;

    [Tooltip("雷射成功連線時的音效 (選填，例如：叮！或啟動聲)")]
    public AudioClip successSound;

    [Tooltip("倒數結束雷射關閉的音效 (選填，例如：斷電聲)")]
    public AudioClip failSound;
    // ==========================================

    private bool isTimerRunning = false;  // 正在倒數中
    private bool isPermanentlyOn = false; // 是否已經成功連線常駐

    public void OnInteract(Transform interactor)
    {
        // 1. 核心防呆：如果已經成功常駐開啟，或者正在倒數中，就直接退回！
        // (因為直接退回了，所以玩家狂按也不會執行到下面播放聲音的程式碼)
        if (isPermanentlyOn || isTimerRunning) return;

        // 【新增】：確認可以按之後，播放按下按鈕的聲音
        if (buttonAudioSource != null && pressSound != null)
        {
            buttonAudioSource.PlayOneShot(pressSound);
        }

        // 2. 啟動計時器任務
        StartCoroutine(LaserTimerRoutine());
    }

    private IEnumerator LaserTimerRoutine()
    {
        isTimerRunning = true;

        // 打開雷射與按鈕視覺開關
        if (targetLaser != null) targetLaser.isLaserOn = true;
        if (visualFeedback != null) visualFeedback.ToggleVisual();

        float timer = 0f;
        bool success = false;

        // 在設定的秒數內，每一幀都檢查有沒有射中真正的接收器 (LaserSensor)
        while (timer < activeDuration)
        {
            timer += Time.deltaTime;

            // 隨時詢問雷射發射器：「你現在有射到 Sensor 嗎？」
            if (targetLaser != null && targetLaser.isHittingSensor)
            {
                success = true;
                break; // 太棒了！提早結束倒數
            }

            yield return null; // 等待下一幀再檢查
        }

        // ================= 時間到或是提早成功的結算 =================
        if (success)
        {
            // 成功：保持開啟
            isPermanentlyOn = true;

            // 【新增】：播放連線成功音效
            if (buttonAudioSource != null && successSound != null)
            {
                buttonAudioSource.PlayOneShot(successSound);
            }

            Debug.Log("<color=green>[機關]</color> 雷射成功連接接收器，已鎖定為常駐啟動！");
        }
        else
        {
            // 失敗：關閉雷射
            if (targetLaser != null) targetLaser.isLaserOn = false;
            if (visualFeedback != null) visualFeedback.ToggleVisual(); // 把按鈕視覺切換回來

            // 【新增】：播放失敗/斷電音效
            if (buttonAudioSource != null && failSound != null)
            {
                buttonAudioSource.PlayOneShot(failSound);
            }

            Debug.Log("<color=orange>[機關]</color> 未能在時限內連接接收器，雷射已關閉。");
        }

        // 倒數結束 (不論成敗都解除計時狀態，讓失敗時可以重按)
        isTimerRunning = false;
    }
}