using System;
using System.Collections;
using Newtonsoft.Json.Utilities;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.AI;


public class Gun : MonoBehaviour
{   
    //현재 총의 상태를 나타내는 열거형
    public enum State
    {
        Ready,//발사 준비된 상태
        Empty,//총의 탄약이 빈 상태
        Reloading//총이 재장전중인 상태
    }
    //상태를 Property로 선언
    public State state { get; private set; }//외부에서는 값을 가져오는 것만 가능
    
    private PlayerShooter gunHolder;//플레이어의 총을 쏘는 컴포넌트의 ref가 저장됨 = 총의 주인
    private LineRenderer bulletLineRenderer;//총알 궤적을 그리기위한 라인 렌더러
    
    private AudioSource gunAudioPlayer;//총알 발사소리와 재장전 소리를 저장할 컴포넌트
    public AudioClip shotClip;//발사소리 클립
    public AudioClip reloadClip;//재장선소리 클립
    
    public ParticleSystem muzzleFlashEffect;//총구 발사 이펙트
    public ParticleSystem shellEjectEffect;//탄피 배출 효과 이펙트
    
    public Transform fireTransform;//총알이 나가는 발사위치, 방향
    public Transform leftHandMount;//왼손의 위치를 알려줄 트랜스폼

    public float damage = 25;//데미지양
    public float fireDistance = 100f;//총알의 발사 체크를할 거리 

    public int ammoRemain = 100;//남은 탄약수
    public int magAmmo;//현재 탄창에 있는 탄약수
    public int magCapacity = 30;//탄창 용량

    public float timeBetFire = 0.12f;//총알의 발사 사이의 간격, 적을수록 연사가 빠름
    public float reloadTime = 1.8f;//재장전 시간
    
    [Range(0f, 10f)] public float maxSpread = 3f;//탄착군의 최대 범위
    [Range(1f, 10f)] public float stability = 1f;//반동이 증가하는 속도
    [Range(0.01f, 3f)] public float restoreFromRecoilSpeed = 2f;//연사를 중단한 다음 탄 퍼짐값이 0으로 돌아오는데까지 속도
    private float currentSpread;//현재 탄퍼짐의 정도
    private float currentSpreadVelocity;//현재 탄퍼짐 반경이 실시간으로 변하는 변화량을 기록

    private float lastFireTime;//가장 최근에 발사가 이루어진 시점

    private LayerMask excludeTarget;//총알을 쏴서는 안되는 대상을 거르기 위한 레이어

    private void Awake()
    {
        //Gun 오브젝트로부터 필요한 컴포넌트들을 불러온다
        gunAudioPlayer = GetComponent<AudioSource>();
        bulletLineRenderer = GetComponent<LineRenderer>();

        //사용할 점의 개수
        bulletLineRenderer.positionCount = 2;//1번째 점은 총구 위치, 2번째는 탄알이 닿은 위치
        bulletLineRenderer.enabled = false;//미리 비활성화 하지 못한 상황을 대비
    }

    //플레이어 슈터가 Gun 오브젝트의 컴포넌트를 대상으로 총의 초기화를 실행한다
    public void Setup(PlayerShooter gunHolder)
    {
        this.gunHolder = gunHolder;//총에 입장에서 현재 총을 쥐는 사람이 누구인지 식별하게한다
        excludeTarget = gunHolder.excludeTarget;//총의 주인이 쏘지 않기로 결정한 레이어를 가져와 내부에 저장

    }
    
    //총이활성화될때마다 매번 총의 상태를 초기화시켜준다
    private void OnEnable()
    {
        magAmmo = magCapacity;//현재 탄약수를 최대 용량까지 채움
        currentSpread = 0f;//현재 탄퍼짐 속도를 0부터 시작
        lastFireTime = 0f;
        state = State.Ready;
    }

    private void OnDisable()
    {
        //비활성화 될때 Gun오브젝트 내부에 실행중인 코루틴이 있다면 모두 종료하게한다
        StopAllCoroutines();
    }

    //Gun 클래스 외부에서 총을 사용해서 발사를 시도하도록 하는 메서드
    public bool Fire(Vector3 aimTarget)//조준 대상을 받는다 
    {
        //해당 방향으로 발사가 가능한 상태에서 Shot메서드를 실행한다
        
        if(state == State.Ready && Time.time >= lastFireTime + timeBetFire){//조건 만족시 발사

            //총알이 날아가는 방향을 벡터 뺄샘으로 구해야한다
            var fireDirection = aimTarget = fireTransform.position;

            //정규분포에 의한 오차 생성
            //spread가 높으룻록 분포가 완만해져서 오차값이 0과 차이가 많이 나는 값이 들어올 확률이 커진다
            var xError = Utility.GedRandomNormalDistribution(0f, currentSpread);
            var yError = Utility.GedRandomNormalDistribution(0f, currentSpread);

            //x,y오차만큼 원래 총알이 향하던 방향을 오른쪽이나 위쪽으로 움직여준다
            //곱하게 되면 y축 기준으로 원래 fireDirection에서 yError만큼 조금 더 회전한 방향이 나오게 된다
            fireDirection = Quaternion.AngleAxis(yError,Vector3.up) * fireDirection;
            fireDirection = Quaternion.AngleAxis(xError,Vector3.right) * fireDirection;

            currentSpread += 1f/stability; //안정성이 높을수록 반동이 줄어듬

            lastFireTime = Time.time;//마지막으로 총알을 발사한 시점을 갱신
            Shot(fireTransform.position, fireDirection);
            //정규분포 랜덤을 사용하여 탄퍼짐 구현
        }
        
        //발사에 실패
        return false;
    }
    
    //실제로 총알 발사 처리가 이루어진다
    private void Shot(Vector3 startPoint, Vector3 direction)//발사되는 지점, 날아가는 방향
    {
        RaycastHit hit;//충돌 정보를 저장할 hit;
        Vector3 hitPosition;//총알이 맞은 곳을 저장

        //~는 flip operator비트연산자로 비트를 0에서 1로 1에서 0으로 뒤집는 역할을 한다
        //excludeTarget 레이어를 제외하고 Raycast를 실행해야하도록 구현함
        if(Physics.Raycast(startPoint, direction,out hit, fireDistance, ~excludeTarget)){
            //Raycast가 성공하면 아래 코드가 실행된다
            var target = hit.collider.GetComponent<IDamageable>();//상대방이 데미지를 받을 수 있는 타입인지 검사
            if(target != null){//target을 가져오는데 성공했다면
                DamageMessage damageMessage;//구조체(value)타입이라 new를 통해 명시적으로 생성할 필요는 없지만 초기화할 필요는 있다
                damageMessage.damager = gunHolder.gameObject;
                damageMessage.amount = damage;
                damageMessage.hitPoint = hit.point;
                damageMessage.hitNormal = hit.normal;

                target.ApplyDamage(damageMessage);
            }
            hitPosition = hit.point;//Ray가 충돌한 위치를 저장
        }
        //만약 Raycast에 의해 충돌한 collider가 아무것도 없다면 
        else{
            //탄알이 최대 사정거리까지 날아갔을때 위치를 지정해준다
            hitPosition = startPoint + direction * fireDistance;
        }
        StartCoroutine(ShotEffect(hitPosition));

        magAmmo--;//탄약을 빼준다
        if(magAmmo <= 0) state = State.Empty;//탄약이 없으면 상태를 지정해준다
    }

    //총일이 맞은 지점을 입력으로 받아 총알 발사와 관련된 이펙트를 재생
    private IEnumerator ShotEffect(Vector3 hitPosition)
    {
        //어느정도 시간을 들여서 처리가 이루어지기에 코루틴 사용

        muzzleFlashEffect.Play();
        shellEjectEffect.Play();

        gunAudioPlayer.PlayOneShot(shotClip);//클립을 즉시 재생

        bulletLineRenderer.enabled = true;
        bulletLineRenderer.SetPosition(0,fireTransform.position);
        bulletLineRenderer.SetPosition(1,hitPosition);

        yield return new WaitForSeconds(0.03f);
        //대기시간이 없으면 활성화되마자가 궤적을 못그리고 비활성화 된다

        bulletLineRenderer.enabled = false;
    }
    
    //외부에서 재장전을 시도하는 메서드
    public bool Reload()
    {
        //재장전을 할 필요가 없는 조건
        if(state == State.Reloading || ammoRemain <= 0 || magAmmo >= magCapacity){return false;}
        //그게 아닐시 장전시작
        StartCoroutine(ReloadRoutine());
        
        //장전 성공
        return true;
    }

    //실제 재장전 처리는 시간을 들여 이 메서드에서 실행된다
    private IEnumerator ReloadRoutine()
    {
        state = State.Reloading;
        gunAudioPlayer.PlayOneShot(reloadClip);
        yield return new WaitForSeconds(reloadTime);

        //탄창에 30발을 추가해야하는데 남은게 20발뿐이라면 20발이라도 넣어야한다
        var ammoToFill = Mathf.Clamp(magCapacity - magAmmo, 0, ammoRemain);//채울 탄창
        //Mathf.Clamp로 주어진 범위 내의 값으로 잘라낸다

        magAmmo += ammoToFill;
        ammoRemain -= ammoToFill;//남은 탼약에서는 탄창에 넣은 수만큼 탄약을 빼준다
        state = State.Ready;
    }

    private void Update()
    {
        //총알 반동값을 상태에 따라서 갱신하는 코드가 작성된다 
        
        //가만히있으면 값이 0으로 되돌아와서 반동이 줄어들게한다
        //currentSpread값이 maxSpread값을 넘기지 못하도록 Clamp를 사용
        currentSpread = Mathf.Clamp(currentSpread,0f,maxSpread);//maxSpread이상으로 탄퍼짐이 심해지진 않는다
        currentSpread = Mathf.SmoothDamp(currentSpread, 0f, 
        ref currentSpreadVelocity, 1f/restoreFromRecoilSpeed);//반동감소
    }
}