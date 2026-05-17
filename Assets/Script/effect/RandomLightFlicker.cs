using UnityEngine;

[RequireComponent(typeof(Light))]
public class RandomLightFlicker : MonoBehaviour
{
    private Light targetLight;

    [Header("亮度設定")]
    [Tooltip("燈光閃爍時的最暗亮度")]
    public float minIntensity = 0.2f;

    [Tooltip("燈光閃爍時的最亮亮度")]
    public float maxIntensity = 2.0f;

    [Header("速度與平滑度設定")]
    [Tooltip("數值越小，閃爍得越劇烈、越快；數值越大，燈光變化越平緩")]
    public float flickerSpeed = 0.05f;

    private float timer;

    void Start()
    {
        // 自動抓取掛在同一個物件上的 Light 組件
        targetLight = GetComponent<Light>();

        // 防呆機制：如果忘記開燈，自動幫你打開
        if (targetLight != null)
        {
            targetLight.enabled = true;
        }
    }

    void Update()
    {
        if (targetLight == null) return;

        // 用計時器來控制每次改變亮度的頻率
        timer += Time.deltaTime;

        if (timer >= flickerSpeed)
        {
            // 重置計時器
            timer = 0f;

            // 【隨機核心】：在最大與最小亮度之間，隨機抽一個數字套用上去
            targetLight.intensity = Random.Range(minIntensity, maxIntensity);
        }
    }
}