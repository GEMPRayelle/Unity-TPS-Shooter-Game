using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;//네이게이션 시스템을 사용하기 위한 네임스페이스

#if UNITY_EDITOR//전처리기를 통해서 빌드할때 UnityEditor가 빠지지않게 해준다
using UnityEditor;
//전처리기를 사용안하면 이 네임스페이스안의 기능을 사용한 메서드들은 실제 pc,ios빌드상황에서 에러가 난다
#endif

public class Enemy : LivingEntity
{
    private enum State//좀비 상태
    {
        Patrol,//정찰
        Tracking,//추적
        AttackBegin,//공격시작
        Attacking//공격중
    }
    
    private State state;
    
    private NavMeshAgent agent;
    private Animator animator;

    public Transform attackRoot;//공격을 하는 피벗포인트
    //attackRoot를 중심으로 반지름을 지정해 반경 내에 있는 물체나 플레이어가 공격을 당하게 한다
    public Transform eyeTransform;//좀비가 적을 일정 반경내 감지할 수 있게 하는 시야의 기준점
    
    private AudioSource audioPlayer;
    public AudioClip hitClip;
    public AudioClip deathClip;
    
    private Renderer skinRenderer;//좀비의 피부색을 공격력에 따라 다르게 주게함

    public float runSpeed = 10f;//달리는 속도
    [Range(0.01f, 2f)] public float turnSmoothTime = 0.1f;//방향을 회전할때 지연시간
    private float turnSmoothVelocity;//회전에 적용할 실시간 변화량
    
    public float damage = 30f;//공격력
    public float attackRadius = 2f;//공격반경
    private float attackDistance;//공격을 시도하는 거리
    
    public float fieldOfView = 50f;//시야각
    public float viewDistance = 10f;//시야거리
    public float patrolSpeed = 3f;//평소 속도
    
    //인스펙터창에서 가리게함, 코드로 통제할것이기때문
    [HideInInspector] public LivingEntity targetEntity;//추적할 대상, LivingEntity타입이면 다 추적할 대상이된다
    public LayerMask whatIsTarget;//적을 감지할 레이어필터


    private RaycastHit[] hits = new RaycastHit[10];//범위기반에 공격을할거라 배열을 사용
    private List<LivingEntity> lastAttackedTargets = new List<LivingEntity>();
    //공격을 새로 시작할때마다 초기화하는 리스트, 공격도중 직전 프레임까지 공격이 적용된 대상을 담아둠
    
    private bool hasTarget => targetEntity != null && !targetEntity.dead;
    //추적할 대상이 존재하는지 알려주는 프로퍼티
    

#if UNITY_EDITOR//유니티 에디터에서만 코드가 작동하고 실제 빌드에서는 빠지게된다

    private void OnDrawGizmosSelected()
    //현재 스크립트를 컴포넌트로 가지는 게임 오브젝트가 인스펙터,씬창에서 선택될때 매프레임 실행된다
    {
        if(attackRoot != null){//공격범위
            Gizmos.color = new Color(1f,0f,0f,0.5f);
            Gizmos.DrawSphere(attackRoot.position,attackRadius);//그릴 구의 중심점과 반지름
        }
        if(eyeTransform != null){//시야범위
            var leftEyeRotation = Quaternion.AngleAxis(-fieldOfView * 0.5f, Vector3.up); 
            var leftRayDirection = leftEyeRotation * transform.forward; //방향표시
            Handles.color = new Color(1f,1f,1f,0.2f);//arc(호)를 그리는 기능은 Handles에 포함
            Handles.DrawSolidArc(eyeTransform.position, Vector3.up, leftRayDirection,fieldOfView,viewDistance);
        }
    }
    
#endif
    
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        audioPlayer = GetComponent<AudioSource>();
        skinRenderer = GetComponentInChildren<Renderer>();

        var attackPivot = attackRoot.position;
        attackPivot.y = transform.position.y;//수평거리만 계산할거라 y값은 같게해서 계산시킴
        //player랑 enemy사이의 거리가 attackDistance보다 작거나 같다면 공격을 시도한다
        attackDistance = Vector3.Distance(transform.position, attackRoot.position) + attackRadius;

        agent.stoppingDistance = attackDistance;//공격이 가능한 거리로 진입했을때 agent는 멈추고 공격을한다
        agent.speed = patrolSpeed;//처음속도를 정찰속도로 해준다, 추격을 시작할때는 변경 해줌
    }

    //Enemy가 생성될때 스펙을 결정한다
    public void Setup(float health, float damage,
        float runSpeed, float patrolSpeed, Color skinColor)
    {
        this.startingHealth = health;
        this.health = health;
        this.damage = damage;
        this.runSpeed = runSpeed;
        this.patrolSpeed = patrolSpeed;

        skinRenderer.material.color = skinColor;

        agent.speed = patrolSpeed;//변경된 PatrolSpeed를 다시 한 번 더 적용시킴
    }

    private void Start()
    {
        StartCoroutine(UpdatePath());
    }

    private void Update()
    {
        if(dead) return;//죽으면 다른 처리들이 실행되지않고 즉시 종료
        
        //추적상태고 대상과 나 사이의 거리가 공격 거리보다 짧다면
        if(state == State.Tracking && Vector3.Distance(targetEntity.transform.position,transform.position)<=attackDistance){
            BeginAttack();//공격실행
        }

        animator.SetFloat("Speed", agent.desiredVelocity.magnitude);//현재 속도로 설정하고 싶은 값을 할당
        
    }

    private void FixedUpdate()//공격하는 대상을 바로보도록 회전시켜준다
    {
        if (dead) return;

        //현재 상태에 따라서 공격범위에 겹친 상대방 Collider를 통해 상대방을 감지하고 상대방에게 데미지를 주는 처리를한다
        if(state == State.AttackBegin || state == State.Attacking){//공격을 시작하거나 공격이 이루어지는 도중일때는
            var lookRotation = Quaternion.LookRotation(targetEntity.transform.position - transform.position);
            //상대가 바라보고 있는 방향을 강제로 추적 대상의 방향으로 변경

            //y축 회전에만 적용할거임, 회전을 할 때 y축 기준으로 회전시킬것이기 때문
            var targetAngleY = lookRotation.eulerAngles.y;

            targetAngleY = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngleY, ref turnSmoothVelocity, turnSmoothTime);
            transform.eulerAngles = Vector3.up * targetAngleY;//값을 부드럽게 전환시킨후 값 적용
        }

        if(state == State.Attacking){//공격이 들어가고 있는 상태라면
            var direction = transform.forward;//공격의 범위가 이동하고있는 방향
            var deltaDistance = agent.velocity.magnitude * Time.deltaTime;//agent가 시간만큼 이동하는 거리를 계산 = 공격의 궤적
            var size = Physics.SphereCastNonAlloc(attackRoot.position, attackRadius,direction,hits,deltaDistance,whatIsTarget);
            //SphereCast는 시작지점에서 어떤 거리만큼 이동할때 연속선상에서 겹치는 collider를 가져온다, 반환으로는 Raycast의 hit배열을 반환
            //nonAlloc은 return값으로 감지된 collider의 개수만 반환한다, 대신 입력으로 직접 Raycast Hit 배열을 할당한다

            for (var i = 0; i < size; i++){//감지된 횟수만큼 hits를 순회한다
                var attackTargetEntity = hits[i].collider.GetComponent<LivingEntity>();
                //cast를 통해 감지한 상대방이 LivingEntity타입으로서 가져와지는 오브젝트인지 검사
                if(attackTargetEntity!=null && !lastAttackedTargets.Contains(attackTargetEntity)){
                    //직전까지 공격을 적용했던 그 리스트에 Contains에 포함되어있지 않다면
                    //공격도중에 또 공격을당하면 안되기에 위와같은 조건을 사용
                    var message = new DamageMessage();//새로운 공격메세지
                    message.amount = damage;
                    message.damager = gameObject;//공격을 가하는 오브젝트는 자기자신

                    if(hits[i].distance <= 0f)//이미 겹친 Collider가 있어서 hits.point가 무조건 0이 나오는 경우에는
                    {
                        message.hitPoint = attackRoot.position;
                    }else{//그게 아니라 공격을 휘두르는 도중에 Collider가 감지된거라면
                        message.hitPoint = hits[i].point;
                    }
                    message.hitNormal = hits[i].normal;
                    attackTargetEntity.ApplyDamage(message);//메세지 전달
                    lastAttackedTargets.Add(attackTargetEntity);//이미 공격을 다한 상대방을 추가
                    break;//반복종료
                }
            }
        }
    }

    private IEnumerator UpdatePath()//주기적으로 추적할 대상의 위치를 찾아서 갱신하는 코루틴
    {
        while (!dead){//사망하지 않을동안 무한루프
            if (hasTarget)//추적대상이 존재한다면
            {
                if(state == State.Patrol){//정찰중인 상태였다면
                    state = State.Tracking;//추적상태로 변경
                    agent.speed = runSpeed;//뛰어다님
                }
                //대상의 위치를 목표위치로 삼고
                agent.SetDestination(targetEntity.transform.position);
                //navMeshAgent에게 목적지를 설정하려면 SetDestination메서드를 살행해 Vec3값을 전달한다
            }
            else//존재하지 않는다면
            {
                //정찰하면서 플레이어를 찾는다
                if (targetEntity != null) targetEntity = null;

                if(state != State.Patrol){//정찰상태가 아니라면
                    state = State.Patrol;//정찰상태로 변경
                    agent.speed = patrolSpeed;//정찰속도로 변경
                }

                if(agent.remainingDistance <= 1f){//목표지점까지 AI가 가야할 남은 거리가 1m이하면
                //새로운 PatrolPosition을 결정하도록 새로운 정찰지점을 결정하도록한다

                    //시야를 통해서 적을 감지하기전에 먼저 Enemy에게 임의의 위치를 찍어줘서 이동하게함
                    var patrolTargetPosition = Utility.GetRandomPointOnNavMesh(
                        transform.position, 20f,NavMesh.AllAreas);//나 자신의 위치에서 20만큼 반경내에서 랜덤한 위치를 찍는다
                    //위치와 반경을 기준으로 navMesh위에 랜덤한 위치를 하나 찍어준다

                    agent.SetDestination(patrolTargetPosition);//목표지점 설정
                }

                //시야를 통해서 적을 감지한다
                var colliders = Physics.OverlapSphere(eyeTransform.position,
                viewDistance, whatIsTarget);//필터링으로 layerMask에 포함된 레이어만 검사하게한다
                //눈의 위치를 기준으로 시야사정거리를 반지름삼아서 구를 그려서 겹치는 모든 Coliider를 다 가져온다

                foreach(var collider in colliders){//모든 Collider를 순회하면서
                    if(!IsTargetOnSight(collider.transform)){//상대가 시야내에 없으면
                        continue;//다음 회차 루프로 넘어감
                    }
                    //시야 내에 존재한다면
                    var livingEntity = collider.GetComponent<LivingEntity>();//살아있는 생명체인지 검사
                    //LivingEntity를 가지고있고 죽지않았다면 추적할 대상이다
                    if(livingEntity != null && !livingEntity.dead){
                        targetEntity = livingEntity;//타겟을 추적대상으로함
                        break;//반복종료
                    }
                }
            }
            
            yield return new WaitForSeconds(0.05f);//0.05초의 시간간격
        }
    }
    
    public override bool ApplyDamage(DamageMessage damageMessage)
    {
        if (!base.ApplyDamage(damageMessage)) return false;
        
        //데미지가 정상적으로 들어갔다면
        if(targetEntity == null){//아직 추적할 대상을 못찾았는데 공격을 당했다면    
            targetEntity = damageMessage.damager.GetComponent<LivingEntity>();
            //추적대상을 즉시 공격을 가한 상대방으로 바꿔준다
        }

        EffectManager.Instance.PlayHitEffect(damageMessage.hitPoint,damageMessage.hitNormal
        ,transform,EffectManager.EffectType.Flesh);//피튀는 이펙트 재생
        audioPlayer.PlayOneShot(hitClip);//효과음 재생

        return true;//공격이 성공적으로 적용됨
    }

    /// <summary>
    /// 명시적으로 공격을 시작할때 사용, 데미지가 들어가는 시점은 아니다
    /// </summary>
    public void BeginAttack()//코드로서 실행하는 메서드
    {
        state = State.AttackBegin;

        agent.isStopped = true;//추적을 중단하도록한다
        animator.SetTrigger("Attack");//공격 애니메이션이 재생
    }
 
    /// <summary>
    /// 데미지가 들어가기 시작하는 지점, 애니메이션 이벤트를 통해 애니메이션에서 실행
    /// </summary>
    public void EnableAttack()
    {
        state = State.Attacking;//공격중 상태로 변경

        lastAttackedTargets.Clear();//직전까지 공격이 적용된 대상 리스트를 비워준다
    }

    /// <summary>
    /// 공격이 끝나는 지점, 애니메이션 이벤트를 통해 애니메이션에서 실행
    /// </summary>
    public void DisableAttack()
    {
        if(hasTarget){//추적할 대상이 있다면
            state = State.Tracking;//현재상태를 추적중으로 바꾼다
        }else{
            state = State.Patrol;//공격할 대상이 없어서 정찰중으로 바꾼다
        }
        
        agent.isStopped = false;//AI가 다시 움직이도록 바꾼다
    }

    private bool IsTargetOnSight(Transform target)
    {
        //[조건]
        //눈의 위치에서 목표의 위치로 광선을 쐈을때 광선이 시야각을 벗어나지 말아야한다
        //중간에 장애물이 없어서 광선이 상대방에게 안정적으로 닿아야한다
        //그럴경우 상대방이 시야내에 존재한다

        var direction = target.position - eyeTransform.position;//눈에 위치에서 타겟의 위치로 향하는 방향벡터

        //간결한 예시를 위해 높이 차이는 고려하지않는다
        direction.y = eyeTransform.forward.y;

        if(Vector3.Angle(direction,eyeTransform.forward) > fieldOfView * 0.5){//두 방향벡터 사이의 각도
        //눈에서 목표까지 방향과, 눈 앞쪽 방향 사이의 각도가 FOV보다 크다면 
            return false;//arc에서 벗어나게된다
        }

        //eye과 target사이에 target을 가리는 장애물이 있는지 검사할때는 direction을 원래 값으로 되돌려야한다
        direction = target.position - eyeTransform.position;

        RaycastHit hit;

        //시야각내에 존재하지만 다른 물체에 중간에 가려져서 보이지않는다면
        if(Physics.Raycast(eyeTransform.position,direction,out hit,viewDistance,whatIsTarget)){
            if(hit.transform == targetEntity){//광선에 닿은 물체가 처음 검사했던 상대방이맞다면
                //상대방과 눈 사이에 장애물이 없어서 상대방이 보이게된다
                return true;
            }
        }
        return false;
    }
    
    public override void Die()
    {
        base.Die();
        GetComponent<Collider>().enabled = false;//길을 막지않게 해제
        agent.enabled = false;//네비게이션 시스템을 완전히 비활성화해줌
        animator.applyRootMotion = true;//애니메이션에 의해서 루트위치가 변경될수있게함
        animator.SetTrigger("Die");//사망 트리거 전달

        audioPlayer.PlayOneShot(deathClip);//사망오디오 재생
    }
}