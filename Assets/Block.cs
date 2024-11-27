using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class Block : MonoBehaviour
{
    public void Initialize(Color _color, int _blockIndex, float _created, JobData _jobdata)
    {
        originalColor = _color;
        blockindex = _blockIndex;
        created = _created;
        jobdata = _jobdata;

    }

    Color CreateDarkColor(Color originalColor)
    {
        // r, g, b 값을 0~255 범위로 변환
        float r = originalColor.r * 255f;
        float g = originalColor.g * 255f;
        float b = originalColor.b * 255f;

        // 0이 아닌 값에 -100 적용
        r = (r != 0) ? Mathf.Clamp(r - 100f, 0f, 255f) : r;
        g = (g != 0) ? Mathf.Clamp(g - 100f, 0f, 255f) : g;
        b = (b != 0) ? Mathf.Clamp(b - 100f, 0f, 255f) : b;

        // 다시 0~1 사이의 값으로 변환 후 새로운 darkColor 생성
        Color darkColor = new Color(r / 255f, g / 255f, b / 255f);
        // darkColor.a = 0.0f;

        return darkColor;
    }
    public JobData jobdata;
    //public Vector3 sink;
    //public Vector3 source;
    public int currentindex = 0; // 앞으로 position이나 time array 의 값을 참조할 때 사용할 index 변수
    public Vector3 currentPosition;     // 현재 위치
    public Vector3 targetPosition;      // 목표 위치
    public Color processColor = Color.green;  // 진행중일 때의 색상
    public Color originalColor;              // 원래 색상
    public Color darkColor;
    public Color transparentColor;              

    public float movingTime = 10.0f;     // 이동 시간 (0.5초)
    private float moveProgress = 0.0f;   // 이동 진행 상태 (0.0f ~ 1.0f)
    private float colorChangeDuration;  // 색상이 변하는 시간
    private float colorChangeProgress = 0.0f;  // 색상 변경 진행 상태 (0.0f ~ 1.0f)


    // sink index : jobdata.waypoints.Count - 1
    float finishtime;
    public int blockindex;
    private float timer = 0.0f;          
    private float delta = 0.0f;
    private float target_delta;
    private float created;

    public bool isFinished;
    public bool isCreated;

    public Renderer blockrenderer;

    void SetColor(Color _color)
    {
        originalColor = _color;
    }

    void Start()
    {
        isFinished = false;
        isCreated = false;
        currentindex = 0;
        finishtime = jobdata.waypoints[jobdata.waypoints.Count - 1]._finish;

        transform.position = jobdata.waypoints[currentindex]._position;
        targetPosition = transform.position;

        transparentColor = originalColor;
        transparentColor.a = 0.0f;
        blockrenderer = GetComponent<Renderer>();
        blockrenderer.material.color = transparentColor;
        Debug.Log("This block " + blockindex + " is now transparent until " + created);


        if (blockrenderer == null)
        {
            Debug.LogError("Renderer component not found on the object!");
            return;
        }

        darkColor = CreateDarkColor(originalColor);

        // num_process = starttime.Length;
        delta = 0.0f;
        target_delta = jobdata.waypoints[currentindex+1]._move - 0.0f;
        //target_delta = movetime[0] - 0.0f;
        Debug.Log("Block" + blockindex + "has to wait till " + target_delta);
        SetTarget(targetPosition);

    }
    
    
    void Update()
    {

        if (timer >= finishtime)
        {
            if (isFinished == false)
            {
                currentPosition = transform.position;
                blockrenderer.material.color = Color.black;
                moveProgress = 0.0f;
                targetPosition = jobdata.waypoints[jobdata.waypoints.Count-1]._position;
            }

            //Sink();
            Move();
            isFinished = true;
            return;
        }

        if (isCreated == false)
        {
            if (timer >= created)
            {
                blockrenderer.material.color = originalColor;
                isCreated = true;
            }
            else
            {
                blockrenderer.material.color = transparentColor;
            }
        }
        
        // Part 2
        if (delta > target_delta) // 단 한 번 호출되는 함수. movetime 도달을 제어
        {
            delta = 0.0f;

            currentindex += 1; // 0에서 1로 변경
            Vector3 waypoint = jobdata.waypoints[currentindex]._position;

            Debug.Log(timer + "Block " + blockindex + "'s currentindex now set to " + currentindex);
            Debug.Log(timer + "Block " + blockindex + " moving towards... " + waypoint);

            SetTarget(waypoint);
            moveProgress = 0.0f;
            //colorChangeProgress = 0.0f;
            //colorChangeDuration = finishtime[currentindex] - starttime[currentindex];
            //blockrenderer.material.color = originalColor;

            if (currentindex < jobdata.waypoints.Count - 1) // 만약 그렇게 해서 1 더해진 currentindex가 마지막 process가 아니라면, 1 더해줌
            {
                target_delta = jobdata.waypoints[currentindex + 1]._move - jobdata.waypoints[currentindex]._move;
                Debug.Log(timer+"\tBlock " + blockindex + "'s new target_delta now set to " + target_delta);
            }
            else
            {
                target_delta = float.MaxValue;
            }

        }
        if (isCreated == true)
        {
            if (timer >= jobdata.waypoints[currentindex]._move && timer <= jobdata.waypoints[currentindex]._start)
            {
                blockrenderer.material.color = darkColor;
                Move();
            }
            else
            {
                if (transform.position != targetPosition)
                {
                    Move();
                }
                if (timer > jobdata.waypoints[currentindex]._start && timer < jobdata.waypoints[currentindex]._finish)
                {
                    check_arrival();
                    blockrenderer.material.color = originalColor;
                    //UpdateColorOverTime();
                }
                else
                {
                    blockrenderer.material.color = darkColor;
                }
            }
        }
        // 타이머 증가
        timer += Time.deltaTime;
        delta += Time.deltaTime;
        // Debug.Log("Time:"+timer+"| colorprogress: "+colorChangeProgress+ "| colorduration: " + colorChangeDuration);

    }

    void check_arrival()
    {
        if (transform.position == targetPosition)
        {
            currentPosition = transform.position;
        }
    }
    void SetTarget(Vector3 newtarget)
    {
        // 라인 A와 라인 B에 따라 목표 위치 설정
        targetPosition = newtarget;
        
        // 현재 위치 갱신
        currentPosition = transform.position;
    }
    void Sink()
    {
        // 이동 진행 상태 (0.0 ~ 1.0) 계산
        moveProgress += Time.deltaTime / movingTime;

        // 현재 위치에서 목표 위치로 이동 (Lerp 사용)
        transform.position = Vector3.Lerp(currentPosition, targetPosition, moveProgress);

        // 이동 중 로그 출력
        //Debug.Log("Block " + blockindex + " is moving towards: " + targetPosition);
    }
    // 지정된 목표 위치로 0.5초 동안 이동하는 함수
    void Move()
    {
        // 이동 진행 상태 (0.0 ~ 1.0) 계산
        moveProgress += Time.deltaTime / movingTime;

        // 현재 위치에서 목표 위치로 이동 (Lerp 사용)
        transform.position = Vector3.Lerp(currentPosition, targetPosition, moveProgress);

        // 이동 중 로그 출력
        //Debug.Log("Block " + blockindex + " is moving towards: " + targetPosition);
    }

    // 시간이 지나면서 색상이 변하는 함수
    void UpdateColorOverTime()
    {
        //Debug.Log("UpdateColorOverTime called");
        colorChangeProgress += Time.deltaTime / colorChangeDuration;

        // 색상 변화 진행 상태에 따라 색상 보간 (Lerp 사용)
        Color currentColor = Color.Lerp(originalColor, processColor, colorChangeProgress);
        
        blockrenderer.material.color = currentColor;
    }
}
