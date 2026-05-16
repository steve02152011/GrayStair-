using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LaserEmitter : MonoBehaviour
{
    [Header("雷射開關設定")]
    [Tooltip("雷射預設是開啟還是關閉？")]
    public bool isLaserOn = true;

    [Header("雷射參數")]
    public float maxDistance = 100f;
    public int maxBounces = 10;

    // ==========================================
    // 【新增】：讓環境雷射也能過濾圖層！
    // ==========================================
    [Tooltip("雷射可以打到哪些東西？(請把不需要擋雷射的圖層取消勾選)")]
    public LayerMask laserHitMask = ~0; // 預設為 Everything

    private LineRenderer lineRenderer;

    [Header("自動動態光源")]
    public bool enableDynamicLights = true;
    public Color lightColor = new Color(1f, 0.5f, 0f);
    public float lightIntensity = 2.0f;
    public float lightRange = 2.0f;
    public bool enableLightShadows = false;

    private List<Light> dynamicLights = new List<Light>();
    public bool isHittingSensor { get; private set; } = false;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    void Update()
    {
        if (isLaserOn)
        {
            CalculateLaser();
        }
        else
        {
            lineRenderer.positionCount = 0;
            isHittingSensor = false;
            TurnOffAllLights();
        }
    }

    public void ToggleLaser()
    {
        isLaserOn = !isLaserOn;
    }

    private void CalculateLaser()
    {
        List<Vector3> laserPoints = new List<Vector3>();
        Vector3 currentPos = transform.position;
        Vector3 currentDir = transform.forward;

        laserPoints.Add(currentPos);
        float remainingDistance = maxDistance;
        isHittingSensor = false;

        for (int i = 0; i < maxBounces; i++)
        {
            // 【關鍵修改】：把 laserHitMask 加進射線判定裡！
            if (Physics.Raycast(currentPos, currentDir, out RaycastHit hit, remainingDistance, laserHitMask))
            {
                laserPoints.Add(hit.point);
                remainingDistance -= hit.distance;

                ILaserReceiver receiver = hit.collider.GetComponent<ILaserReceiver>();

                if (receiver != null)
                {
                    if (hit.collider.GetComponent<LaserSensor>() != null)
                    {
                        isHittingSensor = true;
                    }

                    bool continueLaser = receiver.ProcessLaser(
                        hit.point, hit.normal, currentDir, hit.collider,
                        ref remainingDistance, laserPoints,
                        out currentPos, out currentDir
                    );

                    if (!continueLaser) break;
                }
                else
                {
                    break;
                }
            }
            else
            {
                laserPoints.Add(currentPos + currentDir * remainingDistance);
                break;
            }

            if (remainingDistance <= 0) break;
        }

        lineRenderer.positionCount = laserPoints.Count;
        lineRenderer.SetPositions(laserPoints.ToArray());
        UpdateDynamicLights(laserPoints);
    }

    private void UpdateDynamicLights(List<Vector3> points)
    {
        if (!enableDynamicLights)
        {
            TurnOffAllLights();
            return;
        }

        int requiredLights = points.Count;

        while (dynamicLights.Count < requiredLights)
        {
            GameObject lightObj = new GameObject("Auto_LaserLight_" + dynamicLights.Count);
            lightObj.transform.SetParent(this.transform);

            Light newLight = lightObj.AddComponent<Light>();
            newLight.type = LightType.Point;
            dynamicLights.Add(newLight);
        }

        for (int i = 0; i < dynamicLights.Count; i++)
        {
            if (i < requiredLights)
            {
                dynamicLights[i].enabled = true;
                dynamicLights[i].transform.position = points[i];
                dynamicLights[i].color = lightColor;
                dynamicLights[i].intensity = lightIntensity;
                dynamicLights[i].range = lightRange;
                dynamicLights[i].shadows = enableLightShadows ? LightShadows.Soft : LightShadows.None;
            }
            else
            {
                dynamicLights[i].enabled = false;
            }
        }
    }

    private void TurnOffAllLights()
    {
        foreach (var light in dynamicLights)
        {
            if (light != null) light.enabled = false;
        }
    }
}