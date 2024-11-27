using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;  // for File I/O
using System.Globalization;  // for converting strings to floats

using System.Linq;
using System;
using Unity.VisualScripting;
using UnityEngine.UIElements;

public class JobData
{
    public JobData(int _index)
    {
        index = _index;
        waypoints = new List<(Vector3 position, float _move, float _start, float _finish, bool _isStacking)>();
        
    }
    public int index;
    public List<(Vector3 _position, float _move, float _start, float _finish, bool _isStacking)> waypoints;

    public void add_waypoint(Vector3 _position, float _move, float _start, float _finish, bool _isStacking)
    {
        waypoints.Add((_position, _move, _start, _finish, _isStacking));
    }
}

public class Job
{
    public Job(Color _color, int _blockindex, GameObject prefab, float created,JobData _jobdata)
    {
        
        // Instantiate�� static �޼����̹Ƿ� Object.Instantiate�� ȣ���ؾ� �մϴ�.
        block = UnityEngine.Object.Instantiate(prefab);
        // ������ �ν��Ͻ����� Block ������Ʈ ��������
        Block blockComp = block.GetComponent<Block>();
        // �ʱ�ȭ �޼��� ȣ��
        blockComp.Initialize(_color, _blockindex, created, _jobdata);

        // Ȱ��ȭ
        block.SetActive(true);

    }

    public GameObject block;

}
class Machine
{
    public Machine(GameObject prefab, Vector3 initialPosition)
    {

        // Instantiate�� static �޼����̹Ƿ� Object.Instantiate�� ȣ���ؾ� �մϴ�.
        process = UnityEngine.Object.Instantiate(prefab);
        // ������ �ν��Ͻ����� Block ������Ʈ ��������
        Process processComp = process.GetComponent<Process>();
        processComp.transform.position = initialPosition;

        process.SetActive(true);
    }
    public GameObject process;

}




public class GameManager : MonoBehaviour
{
    public GameObject BlockPrefab;
    public GameObject ProcessPrefab;

    public string colorPath = "color.csv"; // CSV ���� ��� (������Ʈ ���� ��)
    public string positionPath = "position.csv";
    public string timePath = "time.csv";
    public string IATPath = "iat.csv";

    private List<Color> colorData; // CSV ���Ϸκ��� ���� RGB ���� ����
    private List<Vector3> positionData;
    List<JobData> jobdata_list;
    private List<(int idx, float created)> IATData;

    public Transform parent;
    public int numBlocks ; // 20
    public float timescale;

    public bool isFinished;

    // Job ��ü �迭 ����
    private Job[] jobs;  // Job[] Ÿ������ ����
    List<Machine> machines = new List<Machine>();
    private Vector3 pos;
    private Vector3 processposition;
    private Vector3 machineposition;
    List<Vector3> source;
    List<Vector3> sink;
    List<Vector3> buffer;
    private float timer = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        Time.timeScale = timescale;
        // 1. CSV ���� �б�

        positionData = ReadPositionFromCSV(positionPath);
        colorData = ReadColorsFromCSV(colorPath);
        jobdata_list = ReadTimeTableFromCSV(timePath);
        IATData = ReadIATFromCSV(IATPath);

        Debug.Log("TImeData.Count : " + numBlocks);
        // num_created = 0;
        // sink_idx = 0;

        // Job �迭 ũ�� ����
        jobs = new Job[numBlocks];  // 3���� Job�� ���� �� �ִ� �迭 ����

        source = stackPositions(5, -2.0f, 0.0f, -1.2f, 0.6f, -0.6f);
        sink = stackPositions(5, 18.0f, 0.0f, 1.2f, -0.6f, -0.6f);

        // buffer ��ǥ�� (10, 0, 0)
        buffer = stackPositions(10, 10.0f, 0.0f, 3.0f, -0.6f, -0.6f);
            
        // ������ Job ��ü ����
        for (int d = 0; d < numBlocks; d++)
        {
            List<Vector3> p_data = new List<Vector3>();
            int num_waypoints = jobdata_list[d].waypoints.Count();
            jobdata_list[d].waypoints.Insert(0,(source[d], 0.0f, 0.0f, 0.0f,true));
            float finishtime = jobdata_list[d].waypoints[jobdata_list[d].waypoints.Count - 1].Item4;
            jobdata_list[d].waypoints.Add((sink[d], finishtime, finishtime+0.05f, finishtime+0.5f,true));


            float created = IATData[d].created;

            int coloridx = d % colorData.Count;


            jobs[d] = new Job(colorData[coloridx], d, BlockPrefab, created, jobdata_list[d]);
            Debug.Log("---------------------------------------------");

            Debug.Log("Job " + d + " generated with # of waypoints :"+ jobdata_list[d].waypoints.Count);
            for (int k=0; k < jobdata_list[d].waypoints.Count; k++)
            {
                Debug.Log("\tJob "+d+" Waypoints:"+jobdata_list[d].waypoints[k]._position);
            }

        }
        Debug.Log("All Jobs Generated!");
        int buffer_idx = 5;
        //------------------------------------------------
        for (int j = 0; j < 9; j++)
        {   
            // buffer idx�� 5������ �����δ� ������ �߰��Ǳ� ������ 1 �� ū ������ �ε����ؾ� ��
            if (j != buffer_idx)
            {
                Debug.Log("----------------------");
                Debug.Log($"Setting Machine {j}...");
                //Debug.Log($"Position: x = {positionData[j].x}, y = {positionData[j].y}, z = {positionData[j].z}");

                machineposition = new Vector3(positionData[j].x, -0.31f, positionData[j].z);
                machines.Add(new Machine(ProcessPrefab, machineposition));
                Debug.Log("New machine " + j + " generated on " + machineposition);
            }
            
        }




    }

    // Update is called once per frame
    void Update()
    {
        
        check_termination();
        //if (timer >= IATData[num_created].created)
        //{
        //    // jobs[num_created].Activate();

        //    if (num_created < numBlocks - 1)
        //    {
        //        num_created++;
        //    }
        //}

        timer += Time.deltaTime;

    }

    void check_termination()
    {

        int count = 0;
        for (int i = 0; i < jobs.Length; i++)
        {
            if (jobs[i].block.GetComponent<Block>().isFinished == true)
            {
                count++;
            }
        }

        if (count == jobs.Length)
        {
            Debug.Log("All Jobs Finished!");
        }
    }

    // d�� ���� idx ���� ������ ����� move ���� �����Ͽ� float[]�� �����ϴ� �Լ�
    float[] ExtractData(List<(int idx, int machine, float move, float start, float finish)> time, int d, int x)
    {
        // idx�� d�� ������ ���� �ε����� ����
        List<int> matchingIndices = new List<int>();

        for (int i = 0; i < time.Count; i++)
        {
            if (time[i].idx == d)
            {
                matchingIndices.Add(i);  // �ش� idx�� ������ ���� index�� ����
            }
        }

        // move �迭�� ũ��� matchingIndices�� ���� + 1 (ù ��° �� 0.0f ����)
        float[] temp = new float[matchingIndices.Count + 1];

        // ù ��° ���� 0.0f
        temp[0] = 0.0f;
        if (x == 0)
        {
            // matchingIndices�� �ִ� ����� move �迭�� ä��
            for (int i = 0; i < matchingIndices.Count; i++)
            {
                temp[i + 1] = time[matchingIndices[i]].move;  // �ش� index�� move ���� �߰�
            }
        }
        else if (x == 1)
        {
            // matchingIndices�� �ִ� ����� move �迭�� ä��
            for (int i = 0; i < matchingIndices.Count; i++)
            {
                temp[i + 1] = time[matchingIndices[i]].start;  // �ش� index�� move ���� �߰�
            }
        }
        else if (x == 2)
        {
            // matchingIndices�� �ִ� ����� move �迭�� ä��
            for (int i = 0; i < matchingIndices.Count; i++)
            {
                temp[i + 1] = time[matchingIndices[i]].finish;  // �ش� index�� move ���� �߰�
            }
        }
        else if (x==3)
        {
            temp[0] = time[matchingIndices[6]].machine;
        }

        return temp;
    }
    // CSV ���Ͽ��� RGB ���� �о�ͼ� List<float[3]> ���·� ����
    List<Vector3> ReadPositionFromCSV(string file)
    {
        List<Vector3> positions = new List<Vector3>();  // ���� ����Ʈ
        string path = Path.Combine(Application.dataPath, file);  // ���� ���
        
        Debug.Log($"Position Added: x = {0}, y = {0}, z = {0}");
        if (File.Exists(path))
        {
            string[] lines = File.ReadAllLines(path);  // ��� ������ ����

            foreach (string line in lines.Skip(1))
            {
                string[] values = line.Split(',');  // ��ǥ�� ������ �и�
                float x = float.Parse(values[1], CultureInfo.InvariantCulture);  // Red
                float y = float.Parse(values[2], CultureInfo.InvariantCulture);  // Green
                float z = float.Parse(values[3], CultureInfo.InvariantCulture);  // Blue

                positions.Add(new Vector3(x, y, z));  // float[3]�� ����
                Debug.Log($"Position Added: x = {x}, y = {y}, z = {z}");
            }
        }
        else
        {
            Debug.LogError("CSV file not found at: " + path);
        }

        return positions;
    }
    List<(int idx, float created)> ReadIATFromCSV(string file)
    {
        var log = new List<(int idx, float created)>();

        string path = Path.Combine(Application.dataPath, file);  // ���� ���

        if (File.Exists(path))
        {
            string[] lines = File.ReadAllLines(path);  // ��� ������ ����

            foreach (string line in lines.Skip(1))
            {
                string[] values = line.Split(',');  // ��ǥ�� ������ �и�
                int idx = int.Parse(values[0], CultureInfo.InvariantCulture);
                float created = float.Parse(values[1], CultureInfo.InvariantCulture);

                // ValueTuple�� ����Ʈ�� �߰�
                log.Add((idx, created));
            }

        }
        else
        {
            Debug.LogError("CSV file not found at: " + path);
        }

        return log;
    }

    List<JobData> ReadTimeTableFromCSV(string file)
    {
        string path = Path.Combine(Application.dataPath, file);  // ���� ���
        List<JobData> jobdata_list = new List<JobData>();
        if (File.Exists(path))
        {
            string[] lines = File.ReadAllLines(path);  // ��� ������ ����

            for (int i = 0; i < numBlocks; i++)
            {
                JobData job = new JobData(i);
                jobdata_list.Add(job);
            }
            Debug.Log("JobData Prepared:" + jobdata_list.Count());

            foreach (string line in lines.Skip(1))
            {
                string[] values = line.Split(',');  // ��ǥ�� ������ �и�
                int idx = int.Parse(values[0], CultureInfo.InvariantCulture);
                
                
                int machine = int.Parse(values[1], CultureInfo.InvariantCulture);
                float move = float.Parse(values[2], CultureInfo.InvariantCulture);
                float start = float.Parse(values[3], CultureInfo.InvariantCulture);
                float finish = float.Parse(values[4], CultureInfo.InvariantCulture);

                // TRUE/FALSE ���ڿ��� bool�� ��ȯ
                bool is_stacking = values[5].Trim().ToUpper() == "TRUE";

                jobdata_list[idx].add_waypoint(positionData[machine], move, start, finish, is_stacking);
            }
        }
        else
        {
            Debug.LogError("CSV file not found at: " + path);
        }

        return jobdata_list;

    }
    // CSV ���Ͽ��� RGB ���� �о�ͼ� List<float[3]> ���·� ����
    List<Color> ReadColorsFromCSV(string file)
    {
        List<Color> colors = new List<Color>();  // ���� ����Ʈ
        string path = Path.Combine(Application.dataPath, file);  // ���� ���

        if (File.Exists(path))
        {
            string[] lines = File.ReadAllLines(path);  // ��� ������ ����

            foreach (string line in lines)
            {
                string[] values = line.Split(',');  // ��ǥ�� ������ �и�
                float r = float.Parse(values[1], CultureInfo.InvariantCulture);  // Red
                float g = float.Parse(values[2], CultureInfo.InvariantCulture);  // Green
                float b = float.Parse(values[3], CultureInfo.InvariantCulture);  // Blue
                Color c = new Color(r / 255f, g / 255f, b / 255f);
                // c.a = 0.3f;
                colors.Add(c);  // float[3]�� ����
            }
        }
        else
        {
            Debug.LogError("CSV file not found at: " + path);
        }

        return colors;
    }

    List<Vector3> stackPositions(int n_row, float startX, float startY, float startZ, float zOffset, float xOffset)
    {
        
        List<Vector3> sources = new List<Vector3>();
        // ó�� 3���� ����� z �������� �װ�, �� ���ķδ� x �������� �����鼭 z �������� ����
        int a;
        int b;
        for (int i = 0; i < numBlocks; i++)
        {
            a = i / n_row;
            b = i % n_row;
            sources.Add(new Vector3(startX + xOffset * a, startY, startZ + zOffset * b));
            
        }
        return sources;
    }
}
