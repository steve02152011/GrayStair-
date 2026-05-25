using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class DoloAI : MonoBehaviour
{
    public enum AIState { Wander, Chase, Attack, Flee }

    [Header("目前狀態")]
    public AIState currentState = AIState.Wander;

    [Header("目標設定")]
    public Transform player;
    private CharacterController playerController; // 用來讀取玩家的速度(聲音大小)

    [Header("尋聲定位設定 (盲眼聽覺)")]
    [Tooltip("Dolo 的基礎聽力極限距離")]
    public float maxHearingRadius = 25f;
    [Tooltip("玩家要走多快才會發出聲音？(低於此速度視為完全靜音)")]
    public float silentSpeedThreshold = 0.5f;
    [Tooltip("玩家的奔跑速度基準 (用來換算最大噪音，請填入你FPS腳本的RunSpeed)")]
    public float playerMaxSpeedReference = 10f;
    [Tooltip("Dolo 到達聲音來源後，會在該處停留尋找幾秒？")]
    public float investigateTime = 3f;

    private Vector3 lastHeardPosition; // 記錄最後聽到聲音的位置

    [Header("避光雷達設定 (僅漫遊有效)")]
    public LayerMask laserLayer;
    public float avoidLaserDistance = 7f;
    public float whiskersAngle = 30f;

    [Header("遊走設定")]
    public float wanderRadius = 10f;
    public float wanderTimer = 5f;
    public float walkSpeed = 3.5f;

    [Header("追擊與攻擊設定")]
    public float runSpeed = 8f;
    public float attackRange = 2.5f;
    public float attackCooldown = 2f;
    private float lastAttackTime;

    [Header("懼光逃跑設定")]
    public float fleeSpeed = 12f;
    public float fleeDistance = 15f;
    public float fleeDuration = 4f;

    [Header("攻擊力設定")]
    public float damageAmount = 20f;

    private NavMeshAgent agent;
    private float stateTimer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        stateTimer = wanderTimer;

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        // 綁定玩家的控制器，以便監聽腳步聲
        if (player != null)
        {
            playerController = player.GetComponent<CharacterController>();
        }
    }

    void Update()
    {
        switch (currentState)
        {
            case AIState.Wander: UpdateWanderState(); break;
            case AIState.Chase: UpdateChaseState(); break;
            case AIState.Attack: UpdateAttackState(); break;
            case AIState.Flee: UpdateFleeState(); break;
        }
    }

    private void ChangeState(AIState newState)
    {
        if (currentState == newState) return;
        Debug.Log($"<color=cyan>[Dolo 大腦]</color> 狀態切換：{currentState} ? <b>{newState}</b>");
        currentState = newState;
        stateTimer = 0f;
    }

    // ================== 漫遊狀態 (瞎眼，只靠聽覺) ==================

    private void UpdateWanderState()
    {
        agent.speed = walkSpeed;

        // 1. 避光雷達最優先
        if (CheckWhiskersForLaser())
        {
            PickNewWanderDestination();
        }
        else
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
            {
                stateTimer += Time.deltaTime;
                if (stateTimer >= wanderTimer)
                {
                    PickNewWanderDestination();
                }
            }
            else
            {
                stateTimer = 0f;
            }
        }

        // 2. 【核心修改】：漫遊時完全不再檢查玩家是否在眼前，只聽聲音！
        if (CheckForSounds())
        {
            ChangeState(AIState.Chase);
        }
    }

    private void PickNewWanderDestination()
    {
        Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
        agent.SetDestination(newPos);
        stateTimer = 0;
    }

    private bool CheckWhiskersForLaser()
    {
        Vector3 rayStart = transform.position + (Vector3.up * 0.5f);
        if (Physics.Raycast(rayStart, transform.forward, avoidLaserDistance, laserLayer)) return true;

        Vector3 leftDir = Quaternion.Euler(0, -whiskersAngle, 0) * transform.forward;
        if (Physics.Raycast(rayStart, leftDir, avoidLaserDistance, laserLayer)) return true;

        Vector3 rightDir = Quaternion.Euler(0, whiskersAngle, 0) * transform.forward;
        if (Physics.Raycast(rayStart, rightDir, avoidLaserDistance, laserLayer)) return true;

        return false;
    }

    // ================== 追擊狀態 (循聲調查) ==================

    private void UpdateChaseState()
    {
        agent.speed = runSpeed;

        // 1. 如果玩家持續移動發出聲音，不斷更新「最後聽到的位置」
        if (CheckForSounds())
        {
            stateTimer = 0f; // 重置疑惑時間
        }

        // 2. 往最後聽到聲音的地方衝過去 (不再像追蹤飛彈一樣鎖定玩家)
        agent.SetDestination(lastHeardPosition);

        // 3. 如果到達了聲音發出的地點
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
        {
            // 停在原地尋找，開始計算疑惑時間
            stateTimer += Time.deltaTime;

            // 只要他在找人的狀態，且玩家剛好就在他身邊 (距離過近)
            // 代表他直接撞到了獵物，發動攻擊！
            if (Vector3.Distance(transform.position, player.position) <= attackRange)
            {
                ChangeState(AIState.Attack);
            }
            // 如果找了一陣子還是安靜無聲，就放棄追擊，回去漫遊
            else if (stateTimer >= investigateTime)
            {
                Debug.Log("<color=grey>[Dolo 聽覺]</color> 奇怪...剛剛明明有聲音的...算了。");
                ChangeState(AIState.Wander);
            }
        }
        else
        {
            // 還沒跑到地點前，如果路上剛好直接撞到玩家，照樣開咬！
            if (Vector3.Distance(transform.position, player.position) <= attackRange)
            {
                ChangeState(AIState.Attack);
            }
        }
    }

    // ================== 聽覺核心系統 ==================

    private bool CheckForSounds()
    {
        if (player == null || playerController == null) return false;

        // 獲取玩家目前的移動速度 (X 和 Z 軸的平面移動)
        Vector3 horizontalVelocity = new Vector3(playerController.velocity.x, 0, playerController.velocity.z);
        float playerSpeed = horizontalVelocity.magnitude;

        // 【滿足你的需求】：如果玩家站著不動，或是緩慢移動 (低於閾值)，視為「完全靜音」
        if (playerSpeed <= silentSpeedThreshold) return false;

        // 根據玩家的速度來決定發出的「噪音半徑」 (跑越快，聲音傳越遠)
        // 使用 Lerp 依比例放大，跑到最高速時，噪音傳遞距離就等於 maxHearingRadius
        float currentNoiseRadius = Mathf.Lerp(0, maxHearingRadius, playerSpeed / playerMaxSpeedReference);

        // 檢查 Dolo 和玩家的距離是否在噪音傳遞半徑內
        if (Vector3.Distance(transform.position, player.position) <= currentNoiseRadius)
        {
            // 聽到了！記錄聲音發出的精準位置
            lastHeardPosition = player.position;
            return true;
        }

        return false;
    }

    // ================== 攻擊與逃跑 (保持不變) ==================

    public void ReactToLaser(Vector3 laserSourcePos)
    {
        if (currentState == AIState.Flee) return;
        ChangeState(AIState.Flee);
        Debug.Log("<color=blue>[Dolo 恐懼]</color> 嗚啊！！被光照到了！");

        Vector3 fleeDirection = (transform.position - laserSourcePos).normalized;
        Vector3 targetFleePoint = transform.position + fleeDirection * fleeDistance;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetFleePoint, out hit, 10f, NavMesh.AllAreas))
        {
            agent.speed = fleeSpeed;
            agent.SetDestination(hit.position);
        }
        else
        {
            agent.speed = fleeSpeed;
            agent.SetDestination(RandomNavSphere(transform.position, fleeDistance, -1));
        }
    }

    private void UpdateFleeState()
    {
        stateTimer += Time.deltaTime;
        if (stateTimer >= fleeDuration) ChangeState(AIState.Wander);
    }

    private void UpdateAttackState()
    {
        agent.isStopped = true;
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;
        transform.rotation = Quaternion.LookRotation(direction);

        if (Time.time >= lastAttackTime + attackCooldown)
        {
            Debug.Log("<color=magenta>[Dolo 戰鬥]</color> 飛撲撕咬！！");
            PlayerSanity playerSanity = player.GetComponent<PlayerSanity>();
            if (playerSanity != null)
            {
                playerSanity.TakeDamage(damageAmount);
            }
            lastAttackTime = Time.time;
        }

        // 如果玩家屏息逃出了攻擊範圍，Dolo 會立刻切換回循聲狀態尋找玩家
        if (Vector3.Distance(transform.position, player.position) > attackRange)
        {
            agent.isStopped = false;
            ChangeState(AIState.Chase);
        }
    }

    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;
        NavMeshHit navHit;
        NavMesh.SamplePosition(randDirection, out navHit, dist, layermask);
        return navHit.position;
    }
}