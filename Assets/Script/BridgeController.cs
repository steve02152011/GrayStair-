using UnityEngine;

public class BridgeController : MonoBehaviour
{
    // 定義延伸方向的種類
    public enum ExtendDirection
    {
        Forward,  // 正 Z
        Backward, // 負 Z
        Right,    // 正 X
        Left,     // 負 X
        Up,       // 正 Y
        Down      // 負 Y
    }

    [Header("橋樑延伸設定")]
    [Tooltip("選擇橋要往哪個方向伸長")]
    public ExtendDirection extendDirection = ExtendDirection.Forward;

    [Tooltip("橋樑完全延伸時的最大長度")]
    public float maxBridgeLength = 10f;

    [Tooltip("橋樑延伸與收回的速度")]
    public float extendSpeed = 5f;

    private float currentLength = 0f;
    private bool isExtending = false;

    private Vector3 initialScale;
    private Vector3 initialPosition;

    void Start()
    {
        // 記錄初始狀態
        initialScale = transform.localScale;
        initialPosition = transform.localPosition;

        currentLength = 0f;
        UpdateBridgeTransform();
    }

    void Update()
    {
        float targetLength = isExtending ? maxBridgeLength : 0f;
        currentLength = Mathf.Lerp(currentLength, targetLength, Time.deltaTime * extendSpeed);
        UpdateBridgeTransform();
    }

    void UpdateBridgeTransform()
    {
        Vector3 newScale = initialScale;
        Vector3 offset = Vector3.zero;

        // 根據選擇的方向，決定縮放哪一個軸，以及位移的方向
        switch (extendDirection)
        {
            case ExtendDirection.Forward:
                newScale.z = currentLength;
                offset = transform.forward * (currentLength / 2f);
                break;
            case ExtendDirection.Backward:
                newScale.z = currentLength;
                offset = -transform.forward * (currentLength / 2f);
                break;
            case ExtendDirection.Right:
                newScale.x = currentLength;
                offset = transform.right * (currentLength / 2f);
                break;
            case ExtendDirection.Left:
                newScale.x = currentLength;
                offset = -transform.right * (currentLength / 2f);
                break;
            case ExtendDirection.Up:
                newScale.y = currentLength;
                offset = transform.up * (currentLength / 2f);
                break;
            case ExtendDirection.Down:
                newScale.y = currentLength;
                offset = -transform.up * (currentLength / 2f);
                break;
        }

        // 套用縮放
        transform.localScale = newScale;

        // 【關鍵邏輯】：修正位置，讓橋看起來是從一端「長」出來，而不是從中心點變大
        // 我們將初始位置加上長度一半的位移
        transform.position = initialPosition + offset;
    }

    // 讓雷射接收器或按鈕呼叫
    public void SetBridgeState(bool state)
    {
        isExtending = state;
    }
}