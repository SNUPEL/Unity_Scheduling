using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class Block : MonoBehaviour
{
    public void Initialize(Color _color, int _blockIndex, 
        List<Vector3> _positions, Vector3 _source, Vector3 _sink, float _created,
        float[] _moveTime, float[] _finishTime, float[] _startTime)
    {
        originalColor = _color;
        blockindex = _blockIndex;
        positions = _positions;
        created = _created;
        movetime = _moveTime;
        finishtime = _finishTime;
        starttime = _startTime;
        currentPosition = _source;
        sink = _sink;

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

    public List<Vector3> positions;
    public Vector3 sink;
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

    public float[] finishtime;
    public float[] movetime;
    public float[] starttime;
    private int num_process;
    public int blockindex;
    private float timer = 0.0f;          
    private float delta;
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
        transform.position = currentPosition;
        positions[0] = transform.position;
        targetPosition = positions[0];

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

        num_process = starttime.Length;
        delta = movetime[0];
        target_delta = movetime[1] - movetime[0];
        //target_delta = movetime[0] - 0.0f;
        // Debug.Log("Block" + blockindex + "has to wait till " + target_delta);
        SetTarget(positions[0]);

    }
    
    
    void Update()
    {
        
        if (timer >= finishtime[finishtime.Length - 1])
        {
            if (isFinished == false)
            {
                currentPosition = transform.position;
                blockrenderer.material.color = Color.black;
                moveProgress = 0.0f;
                targetPosition = sink;
            }
            
            Sink();

            isFinished = true;
            return;
        }

        if (isCreated == false)
        {
            if (timer >= created)
            {
                blockrenderer.material.color = originalColor;
                Debug.Log("Now this Block" + blockindex + "Turned Opaque!");
                isCreated = true;
            }
            else
            {
                blockrenderer.material.color = transparentColor;
                Debug.Log("Block"+blockindex + "\t a : " +blockrenderer.material.color.a);
                Debug.Log("Block"+blockindex + "\t r : " + blockrenderer.material.color.r);
                Debug.Log("Block"+blockindex + "\t g : " + blockrenderer.material.color.g);
                Debug.Log("Block"+blockindex + "\t b : " + blockrenderer.material.color.b);
            }
        }
        
        // Part 2
        if (delta > target_delta) // 단 한 번 호출되는 함수. movetime 도달을 제어
        {
            delta = 0.0f;
            if (blockindex == 1)
            {
                Debug.Log("At" + timer + ", Job " + blockindex + "set target to " + (positions[currentindex].x, positions[currentindex].y, positions[currentindex].z));
            }

            currentindex += 1; // 0에서 1로 변경
            SetTarget(positions[currentindex]);
            moveProgress = 0.0f;
            //colorChangeProgress = 0.0f;
            //colorChangeDuration = finishtime[currentindex] - starttime[currentindex];
            //blockrenderer.material.color = originalColor;

            if (currentindex != num_process - 1) // 만약 마지막 process가 아니라면, 1 더해줌
            {
                target_delta = movetime[currentindex + 1] - movetime[currentindex];
            }
            else
            {
                target_delta = float.MaxValue;
            }

        }
        if (isCreated == true)
        {
            if (timer >= movetime[currentindex] && timer <= starttime[currentindex])
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
                if (timer > starttime[currentindex] && timer < finishtime[currentindex])
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
