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
    public JobData jobdata;
    //public Vector3 sink;
    //public Vector3 source;
    public int currentindex = 0; // ������ position�̳� time array �� ���� ������ �� ����� index ����
    public Vector3 currentPosition;     // ���� ��ġ
    public Vector3 targetPosition;      // ��ǥ ��ġ
    public Color processColor = Color.green;  // �������� ���� ����
    public Color originalColor;              // ���� ����
    public Color darkColor;
    public Color transparentColor;

    public float movingTime = 10.0f;     // �̵� �ð� (0.5��)
    protected float moveProgress = 0.0f;   // �̵� ���� ���� (0.0f ~ 1.0f)
    private float colorChangeDuration;  // ������ ���ϴ� �ð�
    private float colorChangeProgress = 0.0f;  // ���� ���� ���� ���� (0.0f ~ 1.0f)


    // sink index : jobdata.waypoints.Count - 1
    protected float finishtime;
    public int blockindex;
    protected float timer = 0.0f;
    protected float delta = 0.0f;
    protected float target_delta;
    protected float created;

    public bool isFinished;
    public bool isCreated;

    public Renderer blockrenderer;

    public GameManager manager;
    public GameObject block;
    public GameObject house;
    public List<GameObject> houseComponents;
    public List<GameObject> building;      // ���� �����Ǵ� ���� ���� -> ���� ������ �߰� �ܰ�

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

        block = manager.BlockPrefab;
        house = manager.HousePrefab;
        for (int i = 0; i < 6; i++)
        {
            houseComponents.Add(house.transform.GetChild(i).gameObject);
        }

        if (blockrenderer == null)
        {
            Debug.LogError("Renderer component not found on the object!");
            return;
        }

        darkColor = CreateDarkColor(originalColor);

        // num_process = starttime.Length;
        delta = 0.0f;
        target_delta = jobdata.waypoints[currentindex + 1]._move - 0.0f;
        //target_delta = movetime[0] - 0.0f;
        Debug.Log("Block" + blockindex + "has to wait till " + target_delta);
        SetTarget(targetPosition);

    }


    void Update()
    {
        //ChangeHouse();
        if (timer >= finishtime)
        {
            if (isFinished == false)
            {
                currentPosition = transform.position;
                blockrenderer.material.color = Color.black;
                moveProgress = 0.0f;
                targetPosition = jobdata.waypoints[jobdata.waypoints.Count - 1]._position;
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
        if (delta > target_delta) // �� �� �� ȣ��Ǵ� �Լ�. movetime ������ ����
        {
            delta = 0.0f;

            currentindex += 1; // 0���� 1�� ����
            Vector3 waypoint = jobdata.waypoints[currentindex]._position;

            Debug.Log(timer + "Block " + blockindex + "'s currentindex now set to " + currentindex);
            Debug.Log(timer + "Block " + blockindex + " moving towards... " + waypoint);

            SetTarget(waypoint);
            moveProgress = 0.0f;
            //colorChangeProgress = 0.0f;
            //colorChangeDuration = finishtime[currentindex] - starttime[currentindex];
            //blockrenderer.material.color = originalColor;

            if (currentindex < jobdata.waypoints.Count - 1) // ���� �׷��� �ؼ� 1 ������ currentindex�� ������ process�� �ƴ϶��, 1 ������
            {
                target_delta = jobdata.waypoints[currentindex + 1]._move - jobdata.waypoints[currentindex]._move;
                Debug.Log(timer + "\tBlock " + blockindex + "'s new target_delta now set to " + target_delta);
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
        // Ÿ�̸� ����
        timer += Time.deltaTime;
        delta += Time.deltaTime;
        // Debug.Log("Time:"+timer+"| colorprogress: "+colorChangeProgress+ "| colorduration: " + colorChangeDuration);

    }

    protected void check_arrival()
    {
        if (transform.position == targetPosition)
        {
            currentPosition = transform.position;
        }
    }
    protected void SetTarget(Vector3 newtarget)
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
    protected virtual void Move()
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

    void ChangeHouse()
    {
        GameObject originalBlock = block;
        originalBlock.SetActive(false);
        block = Instantiate(house, transform.position, Quaternion.identity);
    }

    void BuildHouse()       // �� ���⸦ ���� �Լ�
    {

        for (int i = 0; i < manager.positionData.Count; i++)
        {
            if(transform.position == targetPosition)
            {
                building.Add(houseComponents[i]);
                for (int j = 0; j <= i; j++)
                {
                    Instantiate(building[i], transform.position, Quaternion.identity);
                }
            }
        }
        
    }
}
