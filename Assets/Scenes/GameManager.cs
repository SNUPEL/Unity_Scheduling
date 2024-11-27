using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;  // for File I/O
using System.Globalization;  // for converting strings to floats

using System.Linq;
using System;

class Job
{
    public Job(Color _color, int _blockindex, GameObject prefab, 
        List<Vector3> _positions, Vector3 _source, Vector3 _sink, float created,
        float[] _movetime, float[] _finishtime, float[] _starttime)
    {
        
        // Instantiate는 static 메서드이므로 Object.Instantiate로 호출해야 합니다.
        block = UnityEngine.Object.Instantiate(prefab);
        // 생성한 인스턴스에서 Block 컴포넌트 가져오기
        Block blockComp = block.GetComponent<Block>();
        // 초기화 메서드 호출
        blockComp.Initialize(_color, _blockindex, 
            _positions, _source, _sink, created,
            _movetime, _finishtime, _starttime);

        // 활성화
        block.SetActive(true);

    }

    public GameObject block;

}
class Machine
{
    public Machine(GameObject prefab, Vector3 initialPosition)
    {

        // Instantiate는 static 메서드이므로 Object.Instantiate로 호출해야 합니다.
        process = UnityEngine.Object.Instantiate(prefab);
        // 생성한 인스턴스에서 Block 컴포넌트 가져오기
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

    public string colorPath = "color.csv"; // CSV 파일 경로 (프로젝트 폴더 내)
    public string positionPath = "position.csv";
    public string timePath = "time.csv";
    public string IATPath = "iat.csv";

    private List<Color> colorData; // CSV 파일로부터 읽은 RGB 값들 저장
    private List<Vector3> positionData;
    private List<(int idx, int machine, float move, float start, float finish)> timeData;
    private List<(int idx, float created)> IATData;

    public Transform parent;
    static int numBlocks; // 20
    static int numProcesses = 7;
    // private int sink_idx;

    public bool isFinished;

    // Job 객체 배열 선언
    private Job[] jobs;  // Job[] 타입으로 선언
    List<Machine> machines = new List<Machine>();
    private Vector3 pos;
    private Vector3 processposition;
    private Vector3 machineposition;
    int count;
    float movingtime = 10.0f;
    List<Vector3> source;
    List<Vector3> sink;
    List<Vector3> buffer;
    private float timer = 0.0f;
    // int num_created = 0;
    // private float timer = 0.0f;
    // int num_created;

    // Start is called before the first frame update
    void Start()
    {
        Time.timeScale = 20f;
        // 1. CSV 파일 읽기
        timeData = ReadTimeTableFromCSV(timePath);
        IATData = ReadIATFromCSV(IATPath); 

        numBlocks = timeData.Count / 7;
        Debug.Log("TImeData.Count : " + numBlocks);
        colorData = ReadColorsFromCSV(colorPath);
        positionData = ReadPositionFromCSV(positionPath);
        // num_created = 0;
        // sink_idx = 0;

        // Job 배열 크기 지정
        jobs = new Job[numBlocks];  // 3개의 Job을 담을 수 있는 배열 생성

        source = stackPositions(5, -2.0f, 0.0f, -1.2f, 0.6f, -0.6f);
        sink = stackPositions(5, 18.0f, 0.0f, 1.2f, -0.6f, -0.6f);

        // buffer 좌표는 (10, 0, 0)
        buffer = stackPositions(10, 10.0f, 0.0f, 3.0f, -0.6f, -0.6f);
            
        // 각각의 Job 객체 생성
        for (int d = 0; d < numBlocks; d++)
        {
            List<Vector3> p_data = new List<Vector3>();
            // processData 리스트를 p_data 리스트로 복사
            p_data.AddRange(positionData);
            for (int u = 0; u < 6; u++)
            {
                p_data[u] = positionData[u]; 
                // 0 : source
                // 1, 2, 3, 4, 5 : machine 1~5
                // 6 : Buffer
            }

            // processData의 6번째 값(index = 5)을 새로운 값으로 교체
            p_data[6] = buffer[d];  // 원하는 값을 넣으면 됩니다.
            float[] m = ExtractData(timeData, d, 3);
            p_data[7] = positionData[(int)m[0] + 1];
            //p_data[8] = positionData[(int)m[0] + 1];
            float[] move = ExtractData(timeData, d, 0);
            float[] start = ExtractData(timeData, d, 1);
            float[] finish = ExtractData(timeData, d, 2);
            float created = IATData[d].created;

            int coloridx = d % colorData.Count;

            jobs[d] = new Job(colorData[coloridx], d, BlockPrefab,
            p_data, source[d], sink[d],
            created, move, finish, start);
            Debug.Log("---------------------------------------------");
            Debug.Log("Job " + d);
            // move 배열의 값들을 출력 (배열을 문자열로 변환)
            string moveValues = string.Join(", ", move);  // move 배열의 요소들을 쉼표로 구분하여 문자열로 변환
            //Debug.Log("Move: " + moveValues);
            string startValues = string.Join(", ", start);  // start 배열의 요소들을 쉼표로 구분하여 문자열로 변환
            string finishValues = string.Join(", ", finish);  // finish 배열의 요소들을 쉼표로 구분하여 문자열로 변환
            // start 값과 finish 값을 출력 (이 값들이 변수가 맞다면 그대로 출력)
            //Debug.Log("Start: " + startValues);
            //Debug.Log("Finish: " + finishValues);
        }
        Debug.Log("All Jobs Generated!");
        int buffer_idx = 5;
        //------------------------------------------------
        for (int j = 1; j < 10; j++)
        {   
            // buffer idx는 5이지만 실제로는 원점이 추가되기 때문에 1 더 큰 값으로 인덱싱해야 함
            if (j != buffer_idx+1)
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

    // d와 같은 idx 값을 가지는 행들의 move 값을 추출하여 float[]로 저장하는 함수
    float[] ExtractData(List<(int idx, int machine, float move, float start, float finish)> time, int d, int x)
    {
        // idx가 d와 동일한 행의 인덱스를 모음
        List<int> matchingIndices = new List<int>();

        for (int i = 0; i < time.Count; i++)
        {
            if (time[i].idx == d)
            {
                matchingIndices.Add(i);  // 해당 idx를 가지는 행의 index를 저장
            }
        }

        // move 배열의 크기는 matchingIndices의 개수 + 1 (첫 번째 값 0.0f 포함)
        float[] temp = new float[matchingIndices.Count + 1];

        // 첫 번째 값은 0.0f
        temp[0] = 0.0f;
        if (x == 0)
        {
            // matchingIndices에 있는 값들로 move 배열을 채움
            for (int i = 0; i < matchingIndices.Count; i++)
            {
                temp[i + 1] = time[matchingIndices[i]].move;  // 해당 index의 move 값을 추가
            }
        }
        else if (x == 1)
        {
            // matchingIndices에 있는 값들로 move 배열을 채움
            for (int i = 0; i < matchingIndices.Count; i++)
            {
                temp[i + 1] = time[matchingIndices[i]].start;  // 해당 index의 move 값을 추가
            }
        }
        else if (x == 2)
        {
            // matchingIndices에 있는 값들로 move 배열을 채움
            for (int i = 0; i < matchingIndices.Count; i++)
            {
                temp[i + 1] = time[matchingIndices[i]].finish;  // 해당 index의 move 값을 추가
            }
        }
        else if (x==3)
        {
            temp[0] = time[matchingIndices[6]].machine;
        }

        return temp;
    }
    // CSV 파일에서 RGB 값을 읽어와서 List<float[3]> 형태로 저장
    List<Vector3> ReadPositionFromCSV(string file)
    {
        List<Vector3> positions = new List<Vector3>();  // 색상 리스트
        string path = Path.Combine(Application.dataPath, file);  // 파일 경로
        positions.Add(new Vector3(0.0f, 0.0f, 0.0f));  // float[3]로 저장
        Debug.Log($"Position Added: x = {0}, y = {0}, z = {0}");
        if (File.Exists(path))
        {
            string[] lines = File.ReadAllLines(path);  // 모든 라인을 읽음

            foreach (string line in lines)
            {
                string[] values = line.Split(',');  // 쉼표로 값들을 분리
                float x = float.Parse(values[0], CultureInfo.InvariantCulture);  // Red
                float y = float.Parse(values[1], CultureInfo.InvariantCulture);  // Green
                float z = float.Parse(values[2], CultureInfo.InvariantCulture);  // Blue

                positions.Add(new Vector3(x, y, z));  // float[3]로 저장
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

        string path = Path.Combine(Application.dataPath, file);  // 파일 경로

        if (File.Exists(path))
        {
            string[] lines = File.ReadAllLines(path);  // 모든 라인을 읽음

            foreach (string line in lines.Skip(1))
            {
                string[] values = line.Split(',');  // 쉼표로 값들을 분리
                int idx = int.Parse(values[0], CultureInfo.InvariantCulture);
                float created = float.Parse(values[1], CultureInfo.InvariantCulture);

                // ValueTuple을 리스트에 추가
                log.Add((idx, created));
            }

        }
        else
        {
            Debug.LogError("CSV file not found at: " + path);
        }

        return log;
    }

    List<(int idx, int machine, float move, float start, float finish)> ReadTimeTableFromCSV(string file)
    {
        var log = new List<(int idx, int machine, float move, float start, float finish)>();

        string path = Path.Combine(Application.dataPath, file);  // 파일 경로

        if (File.Exists(path))
        {
            string[] lines = File.ReadAllLines(path);  // 모든 라인을 읽음

            foreach (string line in lines.Skip(1))
            {
                string[] values = line.Split(',');  // 쉼표로 값들을 분리
                int idx = int.Parse(values[0], CultureInfo.InvariantCulture);
                int machine = int.Parse(values[1], CultureInfo.InvariantCulture);
                float move = float.Parse(values[2], CultureInfo.InvariantCulture);
                float start = move + movingtime;
                float finish = float.Parse(values[4], CultureInfo.InvariantCulture);

                // ValueTuple을 리스트에 추가
                log.Add((idx, machine, move, start, finish));
            }

        }
        else
        {
            Debug.LogError("CSV file not found at: " + path);
        }

        return log;

    }
    // CSV 파일에서 RGB 값을 읽어와서 List<float[3]> 형태로 저장
    List<Color> ReadColorsFromCSV(string file)
    {
        List<Color> colors = new List<Color>();  // 색상 리스트
        string path = Path.Combine(Application.dataPath, file);  // 파일 경로

        if (File.Exists(path))
        {
            string[] lines = File.ReadAllLines(path);  // 모든 라인을 읽음

            foreach (string line in lines)
            {
                string[] values = line.Split(',');  // 쉼표로 값들을 분리
                float r = float.Parse(values[1], CultureInfo.InvariantCulture);  // Red
                float g = float.Parse(values[2], CultureInfo.InvariantCulture);  // Green
                float b = float.Parse(values[3], CultureInfo.InvariantCulture);  // Blue
                Color c = new Color(r / 255f, g / 255f, b / 255f);
                // c.a = 0.3f;
                colors.Add(c);  // float[3]로 저장
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
        // 처음 3개의 블록은 z 방향으로 쌓고, 그 이후로는 x 방향으로 나가면서 z 방향으로 쌓음
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
