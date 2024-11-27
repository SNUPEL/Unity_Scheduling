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
        // r, g, b ���� 0~255 ������ ��ȯ
        float r = originalColor.r * 255f;
        float g = originalColor.g * 255f;
        float b = originalColor.b * 255f;

        // 0�� �ƴ� ���� -100 ����
        r = (r != 0) ? Mathf.Clamp(r - 100f, 0f, 255f) : r;
        g = (g != 0) ? Mathf.Clamp(g - 100f, 0f, 255f) : g;
        b = (b != 0) ? Mathf.Clamp(b - 100f, 0f, 255f) : b;

        // �ٽ� 0~1 ������ ������ ��ȯ �� ���ο� darkColor ����
        Color darkColor = new Color(r / 255f, g / 255f, b / 255f);
        // darkColor.a = 0.0f;

        return darkColor;
    }

    public List<Vector3> positions;
    public Vector3 sink;
    public int currentindex = 0; // ������ position�̳� time array �� ���� ������ �� ����� index ����
    public Vector3 currentPosition;     // ���� ��ġ
    public Vector3 targetPosition;      // ��ǥ ��ġ
    public Color processColor = Color.green;  // �������� ���� ����
    public Color originalColor;              // ���� ����
    public Color darkColor;
    public Color transparentColor;              

    public float movingTime = 10.0f;     // �̵� �ð� (0.5��)
    private float moveProgress = 0.0f;   // �̵� ���� ���� (0.0f ~ 1.0f)
    private float colorChangeDuration;  // ������ ���ϴ� �ð�
    private float colorChangeProgress = 0.0f;  // ���� ���� ���� ���� (0.0f ~ 1.0f)

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
        if (delta > target_delta) // �� �� �� ȣ��Ǵ� �Լ�. movetime ������ ����
        {
            delta = 0.0f;
            if (blockindex == 1)
            {
                Debug.Log("At" + timer + ", Job " + blockindex + "set target to " + (positions[currentindex].x, positions[currentindex].y, positions[currentindex].z));
            }

            currentindex += 1; // 0���� 1�� ����
            SetTarget(positions[currentindex]);
            moveProgress = 0.0f;
            //colorChangeProgress = 0.0f;
            //colorChangeDuration = finishtime[currentindex] - starttime[currentindex];
            //blockrenderer.material.color = originalColor;

            if (currentindex != num_process - 1) // ���� ������ process�� �ƴ϶��, 1 ������
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
        // Ÿ�̸� ����
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
        // ���� A�� ���� B�� ���� ��ǥ ��ġ ����
        targetPosition = newtarget;
        
        // ���� ��ġ ����
        currentPosition = transform.position;
    }
    void Sink()
    {
        // �̵� ���� ���� (0.0 ~ 1.0) ���
        moveProgress += Time.deltaTime / movingTime;

        // ���� ��ġ���� ��ǥ ��ġ�� �̵� (Lerp ���)
        transform.position = Vector3.Lerp(currentPosition, targetPosition, moveProgress);

        // �̵� �� �α� ���
        //Debug.Log("Block " + blockindex + " is moving towards: " + targetPosition);
    }
    // ������ ��ǥ ��ġ�� 0.5�� ���� �̵��ϴ� �Լ�
    void Move()
    {
        // �̵� ���� ���� (0.0 ~ 1.0) ���
        moveProgress += Time.deltaTime / movingTime;

        // ���� ��ġ���� ��ǥ ��ġ�� �̵� (Lerp ���)
        transform.position = Vector3.Lerp(currentPosition, targetPosition, moveProgress);

        // �̵� �� �α� ���
        //Debug.Log("Block " + blockindex + " is moving towards: " + targetPosition);
    }

    // �ð��� �����鼭 ������ ���ϴ� �Լ�
    void UpdateColorOverTime()
    {
        //Debug.Log("UpdateColorOverTime called");
        colorChangeProgress += Time.deltaTime / colorChangeDuration;

        // ���� ��ȭ ���� ���¿� ���� ���� ���� (Lerp ���)
        Color currentColor = Color.Lerp(originalColor, processColor, colorChangeProgress);
        
        blockrenderer.material.color = currentColor;
    }
}
