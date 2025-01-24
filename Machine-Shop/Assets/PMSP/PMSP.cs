using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;                                                                // File for input and output
using System.Globalization;                                                     // For converting strings to floats

using System.Linq;
using System;
using Unity.VisualScripting;
using UnityEngine.UIElements;

public class JobData                                                                                                                    // Class representing a job's data
{
    public JobData(int _index)
    {
        index = _index;                                                                                                                 // Set the job index
        waypoints = new List<(Vector3 position, int jobindex, int _machineindex, float _created, float _move, float _setup,  float _start,
            float _finish, float _JobSetup, float _MachineSetup, float _tardiness, int tardLevel, bool _isStacking)>();                                // Initialize empty list of waypoints

       
    }
    public int index;                                                                                                                   // Job index
    public List<(Vector3 position, int jobindex, int _machineindex, float _created, float _move, float _setup, float _start,
           float _finish, float _JobSetup, float _MachineSetup, float _tardiness, int tardLevel, bool _isStacking)> waypoints;                         // List of waypoints    

    public void add_waypoint(Vector3 position, int jobindex, int _machineindex, float _created, float _move, float _setup, float _start,
            float _finish, float _JobSetup, float _MachineSetup, float _tardiness, int tardLevel, bool _isStacking)                                    // Adds a waypoint to the list      
    {
        waypoints.Add((position, index, _machineindex, _created, _move, _setup, _start, _finish, _JobSetup, _MachineSetup, _tardiness, tardLevel, _isStacking));
    }
}

// The following class represents a job block in the simulation
// Handles the creation (Instantiate) of visual game objects and initializes their behavior using the SingleJobBlock script
public class Job
{
    public Job(string _mode, JobData _jobdata, GameObject prefab, Color _color, int _blockindex)
    {
        block = UnityEngine.Object.Instantiate(prefab);                         // Instantiate is a static method so it is called as Object 
        SingleJobBlock blockComp = block.GetComponent<SingleJobBlock>();
        blockComp.Initialize(_mode, _jobdata, _color, _blockindex);    // Initializing the parameters of the job
        //blockComp.Initialize(_mode, _color, _jobdata, _blockindex, _source, _position, _sink, _timetable,
        //    _setupmode, _setuptime, _tardiness, _tardLevel);
        Debug.Log("Job" + _blockindex + " Created!");
       // block.SetActive(true);
    }
    public void Activate()
    {
        block.SetActive(true);                                                  // The block is initially active (visible in the scene)
    }
    public GameObject block;
}
// The following class represents a processing unit in the simulation. Each machine is instantiated from the ProcessPrefab and placed at a specified position.
public class Machine
{
    public Machine(GameObject prefab, Vector3 initialPosition)
    {
        process = UnityEngine.Object.Instantiate(prefab);                       // Instantiate is a static method so it is called as Object.Instantiate 
        Process processComp = process.GetComponent<Process>();                  // Import the Block component from the created instance
        processComp.transform.position = initialPosition;                       

        process.SetActive(true);                                                // The process is initially active (visible in the scene)
    }
    public GameObject process;                                                  
}

    public class PMSP : MonoBehaviour
    {
        public GameObject BlockPrefab;                                              // Visual representation of jobs
        public GameObject ProcessPrefab;                                            // Visual representation of machines

        public string colorPath = "color.csv";                                      // File path (in project folder) for the CSV file containing colors
        public string positionPath = "position.csv";                                // File path (in project folder) to job positions
        public string timePath = "ATCS.csv";                                        // File path (in project folder) to timetable for the job execution
        public string mode = "";
        public int hFactor = 0;                                                     // Vertical offset applied to positions (stacking layers of jobs)
        private List<Color> colorData;                                              // Save RGB values read from the CSV file 
        private List<Vector3> positionData;
        List<JobData> jobdata_list;                                                 // List of job data
        //private List<(int idx, float created)> IATData;                             // Inter-arrival time data
       // List<Machine> machines = new List<Machine>();                               // List of Machines

        //private List<(int idx, int machine, float release, float move, float setup, float start, float finish, int setupmode, int setupTime, float tardiness, int tardLevel)> timeData;

        public Transform parent;
        static int numBlocks = 100;                                                 // Number of jobs
        static int numMachine = 5;                                                  // Number of machines
        private int sink_idx;

        public bool isfinished;                                                     // To track if all the jobs are completed

        //Job object array declaration
        public Job[] jobs;                                                         // Declare as job[] type
        public Machine[] machines;
        private Vector3 pos;
        private Vector3 processposition;
        private Vector3 machineposition;

        List<Vector3> source;
        List<Vector3> sink;
        List<Vector3> buffer;                                                       // List of buffer positons
        private float timer = 0.0f;                                                 // To track the elapsed time in the simulation

        //private Job[] jobs;  // Job[] 타입으로 선언
        //public Machine[] machines;

        TimerManager timermanager;
        IntManager setupmanager;
        FloatManager tardinessmanager;


    // Start is called before the first frame update
    void Start()
        {
            Time.timeScale = 40f;
            sink_idx = 0;
            jobs = new Job[numBlocks];
            machines = new Machine[numMachine];

            timermanager = GameObject.Find(name + "_Timer").GetComponent<TimerManager>();
            setupmanager = GameObject.Find(name + "_Setup").GetComponent<IntManager>();
            tardinessmanager = GameObject.Find(name + "_Tard").GetComponent<FloatManager>();
            // Metrics like setup time and tardiness are handled by external managers (setupmanager, tardinessmanager)

            colorData = ReadColorsFromCSV(colorPath);                               // Read CSV file and parse color information
            positionData = ReadPositionFromCSV(positionPath);                       // Read CSV file and loads positions (x,y,z) for jobs in vector3 and adjusts for vertical offsets, hFacto
            jobdata_list = ReadTimeTableFromCSV(timePath);                          // Read CSV file for job schedule including timings and setups

            Debug.Log("TImeData.Count : " + numBlocks);

            int jobindex = 0;
            source = stackPositions(10, 0.0f, 0.0f, 1.0f, 0.6f, -0.6f);
            sink = stackPositions(10, 11.4f, 0.0f, 6.4f, -0.6f, -0.6f);
            //buffer = stackPositions(10, 10.0f, 0.0f, 3.0f, -0.6f, -0.6f);

            

            for (int d = 0; d < numBlocks; d++)
            {
                List<Vector3> p_data = new List<Vector3>();

                int num_waypoints = jobdata_list[d].waypoints.Count();

                int coloridx = d % colorData.Count;
                int machineidx = d % numMachine;

                jobdata_list[d].waypoints.Insert(0, (source[d], 0, 0, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0, true));                                           // Add source to waypoints
                int machineNUM = jobdata_list[d].waypoints[jobdata_list[d].waypoints.Count - 1].Item2;
                float finishtime = jobdata_list[d].waypoints[jobdata_list[d].waypoints.Count - 1].Item8;                // Retrieves the item4 in the tuple representing the waypoint corresponding to the finish time
                float created = jobdata_list[d].waypoints[jobdata_list[d].waypoints.Count - 1].Item4;                // Retrieves the item4 in the tuple representing the waypoint corresponding to the finish time
                float setuptime = jobdata_list[d].waypoints[jobdata_list[d].waypoints.Count - 1].Item6;                // Retrieves the item4 in the tuple representing the waypoint corresponding to the finish time
                float JobSetup = jobdata_list[d].waypoints[jobdata_list[d].waypoints.Count - 1].Item9;                // Retrieves the item4 in the tuple representing the waypoint corresponding to the finish time
                float SetupTime = jobdata_list[d].waypoints[jobdata_list[d].waypoints.Count - 1].Item10;                // Retrieves the item4 in the tuple representing the waypoint corresponding to the finish time
                float tardiness = jobdata_list[d].waypoints[jobdata_list[d].waypoints.Count - 1].Item11;                // Retrieves the item4 in the tuple representing the waypoint corresponding to the finish time
                int tardLevel = jobdata_list[d].waypoints[jobdata_list[d].waypoints.Count - 1].Item12;

                jobdata_list[d].waypoints.Add((sink[d],jobindex, machineNUM, created, finishtime, setuptime, finishtime + 0.05f, finishtime + 0.5f, JobSetup, SetupTime, tardiness, tardLevel, true));           // Add source to waypoints

                jobs[d] = new Job(mode, jobdata_list[d], BlockPrefab, colorData[coloridx], d);                       // Instantiate and configure the job
                Debug.Log("---------------------------------------------");

                Debug.Log("Job " + d + " generated with # of waypoints :" + jobdata_list[d].waypoints.Count);
                for (int k = 0; k < num_waypoints; k++)
                {
                    Debug.Log("\tJob " + d + " Waypoints:" + jobdata_list[d].waypoints[k]._created);
                }
            }

            for (int j = 0; j < numMachine; j++)
             {
                   machineposition = new Vector3(positionData[j].x, -0.305f, positionData[j].z);
                    //machines.Add(new Machine(ProcessPrefab, machineposition));
                    machines[j] = new Machine(ProcessPrefab, machineposition);
            }

        //int buffer_idx = 5;

       // for (int j = 0; j < 9; j++)
       // {
        //   if (j != buffer_idx)
        //   {
         //     Debug.Log("----------------------");
        //      Debug.Log($"Setting Machine {j}...");
         //     Debug.Log($"Position: x = {positionData[j].x}, y = {positionData[j].y}, z = {positionData[j].z}");
         //     if (j >= 0 && j < positionData.Count)
         //     {
         //         machineposition = new Vector3(positionData[j].x, -0.31f, positionData[j].z);                        // Initializes machine positions
         //         machines[j] = new Machine(ProcessPrefab, machineposition);
        
         //     }// Adds machine positions to waypoint
          //    Debug.Log("New machine " + j + " generated on " + machineposition);
         // }

        //}
    }


        //if (j >= 0 && j < positionData.Count)
        //{
        //machineposition = new Vector3(positionData[j].x, -0.31f, positionData[j].z);  // Safe to access positionData[j]
        //}
        //else
        //{
        //   Debug.LogError("Index j is out of range: " + j + ". positionData.Count is: " + positionData.Count);
        //}
        // Each job object is initialized with color (based on setupmode), source, Sink, and Machine Positions (determined from CSV and stacking logic), timetable (extracted from timeData)
        //for (int d = 0; d < numBlocks; d++)
        // {
        //int coloridx = d % colorData.Count;
        // int machineidx = d % numMachine;

        //List<Vector3> p_data = new List<Vector3>();
        //     int num_waypoints = jobdata_list[d].waypoints.Count();
        //     jobdata_list[d].waypoints.Insert(0, (source[d], 0.0f, 0.0f, 0.0f, true));                                 // Add source to waypoints
        //    float finishtime = jobdata_list[d].waypoints[jobdata_list[d].waypoints.Count - 1].Item4;                // Retrieves the item4 in the tuple representing the waypoint corresponding to the finish time
        //    jobdata_list[d].waypoints.Add((sink[d], finishtime, finishtime + 0.05f, finishtime + 0.5f, true));           // Add source to waypoints
        // }
        // Create an array that contains jobs

        // Machines are created and placed at their designated positions using machineposition	
        //for (int j = 0; j < numMachine; j++)
        // {
        // Debug.Log(j);
        //     machineposition = new Vector3(positionData[j].x, -0.305f, positionData[j].z);
        //     machines[j] = new Machine(ProcessPrefab, machineposition);
        // Debug.Log("New machine " + j + " generated on " + machineposition);
        // }

        //float[] timetable = new float[] { timeData[d].Item3, timeData[d].Item4, timeData[d].Item5, timeData[d].Item6, timeData[d].Item7 };

        // Creates a floating-point array timetable with 5 elements. Item3 to Item7 are values extracted from the timeData[d] entry, representing specific time-related attributes
        //
        //jobs[d] = new Job(mode, colorData[timeData[d].setupmode], d, BlockPrefab,
        //positionData[timeData[d].Item2], source[d], sink[d], timetable, timeData[d].setupmode, timeData[d].setupTime, timeData[d].tardiness, timeData[d].tardLevel);

        // Creates a new Job object and assigns it to the d-th index of the jobs array. Mode specifies the mode of the job, d is the job index.
        // BlockPrefab is the blocks visual representation, sink and source associated with d representing start and end of the job process.
        // colorData[timeData[d].setupmode] retrieves a color or value from colorData using the setupmode property of timeData[d]

        // Timetable is the previously defined array of times for this job.
        // The setup mode for the job, likely specifying a configuration or operational mode.
        // The setup time required for this job. 
        // A measure of how late the job is compared to its deadline
        // The level of tardiness or severity, possibly a categorization of how late the job is.


        // Update is called once per frame
        void Update()
        {
            //jobs = new Job[numBlocks];

            check_termination();                                       // Check if all jobs are complete
            if (isfinished)
            {
                timermanager.btn_active = false;
                return;
            }
            timer += Time.deltaTime;                                                // The timer tracks elapsed time to manage job scheduling
        }

        // check_termination scans through all jobs and checks if they have reached their final sink positions. If all jobs are finished, the simulation ends.
        void check_termination()
        {
            if (jobs == null || jobs.Length == 0) // Check if jobs array is null or empty
            {
                Debug.LogError("Jobs array is not initialized or empty.");
            }

            int count = 0;                                                          // Initialize the count of finished jobs to zero
            for (int i = 0; i < jobs.Length; i++)                                   // Iterate over all jobs to check their status
            {
                if (jobs[i].block.GetComponent<SingleJobBlock>().isFinished == true)         // Check if the current job's block component is finished
                {
                    count++;
                    // Increment the count for each finished job
                }
            }
            // If all jobs are finished, log a completion message
            if (count == jobs.Length)
            {
                Debug.Log("All Jobs Finished!");
            }
        }

         // Extracts specific data (move, start, or finish times) from a timetable based on job index
         //float[] ExtractData(List<(int idx, int machine, float move, float start, float finish)> time, int d, int x)
         //{
         //    List<int> matchingIndices = new List<int>();                                    // List to store the indices of matching records

         //    for (int i = 0; i < time.Count; i++)                                            // Find all indices in the time list where the job index matches 'd'
         //   {
         //       if (time[i].idx == d)
         //       {
         //            matchingIndices.Add(i);                                                 // Add the matching index to the list
         //        }
         //   }

        // Create an array to store the extracted data, initialized with one extra slot
        //    float[] temp = new float[matchingIndices.Count + 1];

        //    temp[0] = 0.0f;                                                                 // Default value for the first element
        //    if (x == 0)                                                                     // Depending on the value of 'x', populate the array with specific data
        //    {
        //         for (int i = 0; i < matchingIndices.Count; i++)
        //         {
        //            temp[i + 1] = time[matchingIndices[i]].move;                            // Extract move times
        //        }
        //   }
        //   else if (x == 1)
        //   {
        //       for (int i = 0; i < matchingIndices.Count; i++)
        //       {
        //           temp[i + 1] = time[matchingIndices[i]].start;                           // Extract start times
        //        }
        //   }
        //   else if (x == 2)
        //   {
        //       for (int i = 0; i < matchingIndices.Count; i++)
        //       {
        //           temp[i + 1] = time[matchingIndices[i]].finish;                          // Extract finish times
        //        }
        //    }
        //    else if (x == 3)
        //    {
        //       temp[0] = time[matchingIndices[6]].machine;                                 // Extract the machine value for a specific record
        //   }

        //   return temp;                                                                    // Return the array of extracted data
        // }

        List<JobData> ReadTimeTableFromCSV(string file)
        {
            string path = Path.Combine(Application.dataPath, file); // File Path
                                      // Positions List 

            var jobdata_list = new List<JobData>(); // List to store JobData objects
            if (File.Exists(path))
            {
                string[] lines = File.ReadAllLines(path); // Read all lines
                for (int i = 0; i < numBlocks; i++)
                {
                    JobData job = new JobData(i);                                                   // Create a new JobData instance with index
                    jobdata_list.Add(job);                                                          // Add the job to the list

                    Debug.Log("JobData Prepared:" + jobdata_list.Count());                              // Log the number of prepared job data
                }
                foreach (string line in lines.Skip(1))                                              // Skip the header line
                {
                    string[] values = line.Split(',');                                              // Separate values with commas

                    int idx = int.Parse(values[0], CultureInfo.InvariantCulture);
                    int machine = int.Parse(values[1], CultureInfo.InvariantCulture);
                    float release = float.Parse(values[2], CultureInfo.InvariantCulture);
                    float move = float.Parse(values[3], CultureInfo.InvariantCulture);
                    float setup = float.Parse(values[3], CultureInfo.InvariantCulture);
                    float start = float.Parse(values[5], CultureInfo.InvariantCulture) + 2.0f;
                    float finish = float.Parse(values[6], CultureInfo.InvariantCulture);
                    int setupmode = int.Parse(values[7], CultureInfo.InvariantCulture);
                    int machineSetup = int.Parse(values[8], CultureInfo.InvariantCulture);
                    int setupTime = Math.Abs(machineSetup - setupmode);
                    float tardiness = float.Parse(values[9], CultureInfo.InvariantCulture);
                    bool is_stacking = values[5].Trim().ToUpper() == "TRUE";                        // Parse stacking flag, TRUE/FALSE
                    int tardLevel = GetTardinessLevel(tardiness);                                   // Helper method to determine tardiness level

                    // Add the initial waypoint (release point)
                    jobdata_list[idx].add_waypoint(positionData[machine], idx, machine, release, move, setup, start, finish, setupmode, setupTime, tardiness, tardLevel, false);

                //log.Add(job);                                                                   // Add the job data to the list
                }
            }
            else
            {
                Debug.LogError("CSV file not found at: " + path);
            }
                return jobdata_list;

        }

        //Helper method to determine tardiness level
        public int GetTardinessLevel(float tardiness)
        {
            if (tardiness == 0.0f) return 0;
            if (tardiness < 50.0f) return 1;
            if (tardiness < 100.0f) return 2;
            if (tardiness < 150.0f) return 3;
            if (tardiness < 200.0f) return 4;
            return 5;
        }

        // Read position values from CSV file and save them to List<float[3]> format
        List<Vector3> ReadPositionFromCSV(string file)
        {
            string path = Path.Combine(Application.dataPath, file);  // 파일 경로
            //List<JobData> jobdata_list = new List<JobData>();
        List<Vector3> positions = new List<Vector3>();
        if (File.Exists(path))
            {
                string[] lines = File.ReadAllLines(path);  // 모든 라인을 읽음

               // for (int i = 0; i < numBlocks; i++)
               // {
               //     JobData job = new JobData(i);
               //     jobdata_list.Add(job);
              //  }
//                Debug.Log("JobData Prepared:" + jobdata_list.Count());

                foreach (string line in lines)
                {
                    string[] values = line.Split(',');                              // Separate values with commas
                    float x = float.Parse(values[0], CultureInfo.InvariantCulture);                                 // Position along x-axis
                    float y = float.Parse(values[1], CultureInfo.InvariantCulture);                                 // Position along y-axis
                    float z = float.Parse(values[2], CultureInfo.InvariantCulture) - 12.0f * hFactor;               // Position along z-axis
                                                                                                                // Converts the string value in values[0] into a floating-point number independent of regional settings
                    positions.Add(new Vector3(x, y, z));                                                            // Save positions as float[3]
                                                                                                                //jobdata_list[idx].add_waypoint(positionData[machine], move, start, finish, is_stacking);
                }
            }
        
            else
            {
                Debug.LogError("CSV file not found at: " + path);
            }
            return positions;
        }
        // Read RGB values from CSV file and save them to List<float[3]> format
        List<Color> ReadColorsFromCSV(string file)
        {
            List<Color> colors = new List<Color>();                                 // Color List
            string path = Path.Combine(Application.dataPath, file);                 // File Path

            if (File.Exists(path))
            {
                string[] lines = File.ReadAllLines(path);                           // Read file line by line

                foreach (string line in lines)
                {
                    string[] values = line.Split(',');                              // Separate the values with commas
                    float r = float.Parse(values[1], CultureInfo.InvariantCulture); // Red
                    float g = float.Parse(values[2], CultureInfo.InvariantCulture); // Green
                    float b = float.Parse(values[3], CultureInfo.InvariantCulture); // Blue

                    colors.Add(new Color(r / 255f, g / 255f, b / 255f));            // Save as float[3]
                }
            }
            else
            {
                Debug.LogError("CSV file not found at: " + path);
            }

            return colors;
        }

        // This method likely returns a list of 3D coordinates (vector3) arranged in a "stack" pattern.
        // The parameters control the starting position and how each position is spaced in the stack.
        List<Vector3> stackPositions(int n_row, float startX, float startY, float startZ, float zOffset, float xOffset)
        {
            List<Vector3> sources = new List<Vector3>();
            // The first three blocks are stacked z-direction, and after that, the blocks move out in the x-direction and stacked in the z-directions
            // Positions for jobs and sinks are calculated dynamically based on grid-like stacking patterns.

            int a;
            int b;
            for (int i = 0; i < numBlocks; i++)
            {
                a = i / n_row;
                b = i % n_row;
                sources.Add(new Vector3(startX + xOffset * a, startY, startZ - 12.0f * hFactor + zOffset * b));
                // Sources is a List<Vector3> and Add is a method used to add an element to this list.  
                // New Vector3 creates a new 3D vector with x, y, and z components
                // x: initial position along the X-axis plus an offset along the axis and multiplied by an iteration factor,
                // z: initial position along the Z-axis and applies a downward adjustment using scaling factor, hFactor-
                // -added to an additional offset along the Z-axis with another iteration factor
            }
            return sources;
        }
    }
