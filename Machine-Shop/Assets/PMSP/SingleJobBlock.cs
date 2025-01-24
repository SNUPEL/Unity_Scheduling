using System.Collections.Generic;
using UnityEngine;

public class SingleJobBlock : MonoBehaviour
{
    public void Initialize(string _mode, JobData _jobdata, Color _color, int _blockindex)
    {
        mode = _mode;
        originalColor = _color;                                                 // Block's starting color
        blockindex = _blockindex;                                               // Block's identifier
        //T_created = _created;                                                   // The time for block's initialization
        //currentPosition = position;
        jobData = _jobdata;                                                     // Initializing an object for jobdata class
    }

    public JobData jobData;

    //This method is called when the block is created to assign initial values.
    //It sets up the block's initial state using parameters passed from another script and populates values for mode, originalColor, blockindex, and positions.
    //Logs setup information for debugging.

    //_source, _position, and _sink are positions for different lifecycle stages.
    //Timetable is an array holding lifecycle timestamps like time when object is created, time for move, time required for set up, time the process starts and finishes.
    //Along with end time for the process color to visually represent the block.
    //setupmode, setuptime, tardiness, tardLevel are some of the other properties assigned to the object initially.

    // The block starts transparent and transitions to visible colors. A darkened version of the given blocks colour is created by reducing RGB values.
    Color CreateDarkColor(Color originalColor)
    {
        // Converts r, g, b values to numbers between 0 to 255
        float r = originalColor.r * 255f;
        float g = originalColor.g * 255f;
        float b = originalColor.b * 255f;

        // Apply - 100 to non-zero values
        r = (r != 0) ? Mathf.Clamp(r - 100f, 0f, 255f) : r;
        g = (g != 0) ? Mathf.Clamp(g - 100f, 0f, 255f) : g;
        b = (b != 0) ? Mathf.Clamp(b - 100f, 0f, 255f) : b;

        // Convert back to a value between 0 to 1 and create a new darkColor and return it
        Color darkColor = new Color(r / 255f, g / 255f, b / 255f);

        return darkColor;
    }
    GameObject block;
    public Vector3 sink;
    public Vector3 source;
    public Vector3 position;

    public int currentindex = 0;                                                // Index variable to be used when referring to the value of the position or time array in the future 
    public Vector3 currentPosition;                                             // Current Location
    public Vector3 targetPosition;                                              // Target Location
    //public Color processColor = Color.green;                                  // Color when in progress
    public Color originalColor;                                                 // Original Color
    public Color transparentColor;                                              // Transparent Color
    public Color darkColor;

    public float movingTime = 2.0f;                                             // Travel Time (0.5 seconds)
    private float moveProgress = 0.0f;                                          // Progress of movement (0.0f to 1.0f)
    //private float colorchangeduration;                                        // Time when the colour changes
    //private float colorchangeprogress = 0.0f;                                 // Color change progress (0.0f to 1.0f)

    private string mode;
    private int tardLevel;
    private int setupmode;
    Vector3 positon_created;
    private float T_created;
    private float T_finish;
    private float setuptime;
    private float tardiness;
    public bool isSinkUpdated;
    private bool updated;
    public int blockindex;

    private float delta = 0.0f;                                                 // Time since last waypoint change
    private float target_delta;                                                 // Target time interval for reaching the next waypoint

    private float timer = 0.0f;
    IntManager setupmanager;
    FloatManager tardinessmanager;
    public bool isCreated;
    public bool isFinished;

    public Renderer blockrenderer;
    //The previous code block defines the public variables(Accessible from the Unity Inspector or other scripts), such as,
    // o   sink, source, position: Various key positions in the block's lifecycle.
    // o   blockindex: Identifies the block uniquely.
    // o   originalColor, transparentColor, darkColor: Handle color changes to visually indicate block states.
    // o   isFinished, isSinkUpdated: Flags to track the block's state.

    //As well as defining the private variables(Internal to the script), which are,
    // o   mode, tardiness, setupmode, etc.: Store job-specific parameters like setup time and tardiness penalties.
    // o   T_created, T_move, T_setup, T_start, etc.: Timetable values representing key lifecycle points.
    // o   moveProgress: Tracks the block's movement progress between positions.
    // o   setupmanager, tardinessmanager: External managers that handle job metrics like setup and tardiness.

    public List<JobData> waypoints = new List<JobData>();

    //Assigns a new color to the block
    void SetColor(Color _color)
    {
        originalColor = _color;
    }

    void Start()
    {

        isCreated = false;
        isFinished = false;
        updated = false;
        currentindex = 0;
        //currentindex = jobData.waypoints[jobData.waypoints.Count - 1].jobindex;

        // Setting up IntManager and FloatManager to use as set up and tardiness managers
        setupmanager = GameObject.Find(mode + "_Setup").GetComponent<IntManager>();
        tardinessmanager = GameObject.Find(mode + "_Tard").GetComponent<FloatManager>();

        transform.position = jobData.waypoints[0].position;                                 // Start at the first waypoint

        T_finish = jobData.waypoints[jobData.waypoints.Count - 1].Item8;                    // Set the finish time based on the last waypoint
        T_created = jobData.waypoints[jobData.waypoints.Count - 1].Item4;                   // Set the time of creation based on the last waypoint
        setuptime = jobData.waypoints[jobData.waypoints.Count - 1]._JobSetup;                   // Set the time of creation based on the last waypoint
        tardiness = jobData.waypoints[jobData.waypoints.Count - 1]._tardiness;                   // Set the time of creation based on the last waypoint
        tardLevel = jobData.waypoints[jobData.waypoints.Count - 1].tardLevel;

        transform.position = jobData.waypoints[jobData.waypoints.Count - 1].position;     // Set initial position and target position to the first waypoint
        targetPosition = transform.position;                                                // Setting the initial target position for the block

        transparentColor = originalColor;
        transparentColor.a = 0.0f;                                                          // Configuring the blocks initial visual appearance: transparent color  

        //Assigning Renderer
        blockrenderer = GetComponent<Renderer>();
        blockrenderer.material.color = transparentColor;
        Debug.Log("This block " + blockindex + " is now transparent until " + T_created);

        if (blockrenderer == null)
        {
            Debug.LogError("Renderer component not found on the object!");
            return;
        }
        darkColor = CreateDarkColor(originalColor);	                                        // Configuring the blocks visual appearance to darkened color  

        delta = 0.0f;                                                                       // Initialize time intervals for waypoint movement
        //target_delta = jobData.waypoints[currentindex + 1]._move - 0.0f;                  // Set the time interval until reaching the
        target_delta = jobData.waypoints[jobData.waypoints.Count - 1]._move - 0.0f;         // Set the time interval until reaching the

        Debug.Log("Block" + blockindex + "has to wait till " + target_delta);               // Logs a message indicating the block's index and the target wait time before moving to the next waypoint
        SetTarget(targetPosition);	                                                        // Setting the target position for the block

    }

    void Update()
    {

        if (isFinished)
        {
            float intensity = Mathf.Clamp01(tardLevel / 5.0f); // 0~5의 값을 0.0~1.0 사이로 변환
            Color tardColor = new Color(1.0f * intensity, 0.0f, 0.0f, 1.0f); // R 값에만 intensity를 곱하고 알파를 1로 설정

            blockrenderer.material.color = tardColor; // 색상 설정
            Sink();

            if (!updated)
            {
                setupmanager.UpdateValue(setuptime);                        // Update setup time using the setupmanager
                tardinessmanager.UpdateValue(tardiness);                    // Update tardiness using the tardinessmanager
                updated = true;                                             // Ensure this is done only once for this waypoint
            }
            else
            {
                blockrenderer.material.color = darkColor;                       // Setting the block's color dark to denote the job is finished
                currentPosition = transform.position;
                blockrenderer.material.color = Color.black;
                moveProgress = 0.0f;
                targetPosition = jobData.waypoints[jobData.waypoints.Count - 1].position;
            }
            return;
        }
        if (isCreated == false)
        {
            if (timer >= T_created)
            {
                blockrenderer.material.color = originalColor;
                isCreated = true;
            }
            else
            {
                blockrenderer.material.color = transparentColor;
            }
        }
       // if (timer >= T_finish)
      //  {
           
     //   }
        // Part 2
        if (delta > target_delta) // 단 한 번 호출되는 함수. movetime 도달을 제어
        {
            delta = 0.0f;

            currentindex += 1; // 0에서 1로 변경
            Vector3 waypoint = jobData.waypoints[currentindex].position;

            Debug.Log(timer + "Block " + blockindex + "'s currentindex now set to " + currentindex);
            Debug.Log(timer + "Block " + blockindex + " moving towards... " + waypoint);

            SetTarget(waypoint);
            moveProgress = 0.0f;
            //colorChangeProgress = 0.0f;
            //colorChangeDuration = finishtime[currentindex] - starttime[currentindex];
            //blockrenderer.material.color = originalColor;

            if (currentindex < jobData.waypoints.Count - 1) // 만약 그렇게 해서 1 더해진 currentindex가 마지막 process가 아니라면, 1 더해줌
            {
                target_delta = jobData.waypoints[currentindex + 1]._move - jobData.waypoints[currentindex]._move;
                Debug.Log(timer + "\tBlock " + blockindex + "'s new target_delta now set to " + target_delta);
            }
            else
            {
                target_delta = float.MaxValue;
            }

        }
        if (isCreated == true)
        {
            if (timer >= jobData.waypoints[currentindex]._move && timer <= jobData.waypoints[currentindex]._start)
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
                if (timer > jobData.waypoints[currentindex]._start && timer < jobData.waypoints[currentindex]._finish)
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

            // 타이머 증가
            timer += Time.deltaTime;
            delta += Time.deltaTime;
        }
    
        // if (jobData == null || jobData.waypoints == null || jobData.waypoints.Count == 0)
        //    return;
        // if (blockrenderer == null)
        // {
        //     Debug.LogError("blockrenderer is null in Update method!");
        //     return;                                                                                 // Exit if blockrenderer is null
        // }

        //  if (isCreated == true)
        //{
        //    if (isFinished)
        //    {
        //       float intensity = Mathf.Clamp01(tardLevel / 5.0f); // 0~5의 값을 0.0~1.0 사이로 변환
        //       Color tardColor = new Color(1.0f * intensity, 0.0f, 0.0f, 1.0f); // R 값에만 intensity를 곱하고 알파를 1로 설정

        //        blockrenderer.material.color = tardColor; // 색상 설정
        //        Sink();

        //        if (!updated)
        //        {
        //           setupmanager.UpdateValue(setuptime);                        // Update setup time using the setupmanager
        //           tardinessmanager.UpdateValue(tardiness);                    // Update tardiness using the tardinessmanager
        //           updated = true;                                             // Ensure this is done only once for this waypoint
        //       }
        //       else
        //        {
        //            blockrenderer.material.color = darkColor;                       // Setting the block's color dark to denote the job is finished
        //        }
        //        return;
        //    }
        //    if (transform.position != targetPosition)                           // Check if the block has reached its target position
        //    {
        //        Move();                                                         // If not, the block is moved
        //   }
        //    if (timer >= T_created)
        //    {
        //       blockrenderer.material.color = originalColor;                   // Set the block to its original color when created
        //        isCreated = true;                                               // Sets the flag to true
        ///    }
        //    else
        //    {
        //        blockrenderer.material.color = transparentColor;                // Keep the block transparent until created
        //    }
        //    if (timer > T_finish)
        // {
        //     if (isFinished == false)
        //     {
        //         currentPosition = transform.position;                                               // Stores the block's current position
        //         blockrenderer.material.color = Color.black;                                         // Changes the block's color to black
        //         moveProgress = 0.0f;                                                                // Resets moveProgress to inital progress

        //        targetPosition = jobData.waypoints[jobData.waypoints.Count - 1]._created;           // Sets the final waypoint as the targetPosition
        //    }
        //   Move();                                                                                 // Ensure the block completes its movement and mark it as finished
        ///    isFinished = true;                                                                      // Sets the flag to true
        //    return;
        //  }
    }
       // if (isCreated == false)
       // {
            
       // }

    //    if (delta > target_delta)                                               // Checks if time since last waypoint change has passed the target time for reaching the next waypoint
    //    {
   //         delta = 0.0f;                                                       // Resets delta time
   //         currentindex += 1;                                                  // Move to the next waypoint
   //         Vector3 waypoint = jobData.waypoints[currentindex - 1]._created;      // Retrieve the position of the current waypoint
//
  //          Debug.Log(timer + "Block " + blockindex + "'s currentindex now set to " + currentindex);
  //          Debug.Log(timer + "Block " + blockindex + " moving towards... " + waypoint);

  //          SetTarget(waypoint);                                                // Set the new target and reset movement progress
   //         moveProgress = 0.0f;                                                // Resets moveProgress to initial progress

            // Update target_delta for the next waypoint or stop if this is the last one
            // Checks if the current waypoint is not the last one, updates target_delta if there are more waypoints to process
    //        if (currentindex < jobData.waypoints.Count - 1)

     //       {
                // Sets target time interval for reaching the next waypoint by using the move times of the current and next waypoints.
    //            target_delta = jobData.waypoints[currentindex + 1]._move - jobData.waypoints[currentindex]._move;
     //           Debug.Log(timer + "\tBlock " + blockindex + "'s new target_delta now set to " + target_delta);
     //       }
     //       else
     //       {
     //           target_delta = float.MaxValue;                                  //Setting target_delta to float.MaxValue to depict last waypoint transition
     //       }

      //  }
        // Increment timers for movement and waypoint transitions
      //  timer += Time.deltaTime;
       // delta += Time.deltaTime;

     
    // Function to see if the block has arrived at the target position or not 
    void check_arrival()
    {
        if (transform.position == targetPosition)                                
        {
            currentPosition = transform.position;   
        }
    }
    void SetTarget(Vector3 newtarget)
    {
        targetPosition = newtarget;                                             // Sets a new target position for the block
        currentPosition = transform.position;                                   // Current position is update to the transform position
    }

    // Using Move() and Sink() functions the game object block moves smoothly between positions using Vector3.Lerp
    void Sink()
    {
        moveProgress += Time.deltaTime / movingTime;	                                        // Calculate Movement Progress (0.0-1.0)
        transform.position = Vector3.Lerp(currentPosition, sink, moveProgress);                 // Movement from Current Location to Target Location using Lerp

        Debug.Log("Block " + blockindex + " is moving towards: " + targetPosition);             //Log output while moving
    }

    void Move()                                                                                 //Function to move to specified target position for 0.5 seconds
    {
        moveProgress += Time.deltaTime / movingTime;                                            // Calculate Movement Progress (0.0-1.0)
        transform.position = Vector3.Lerp(currentPosition, targetPosition, moveProgress);       // Movement from Current Location to Target Location using Lerp
        Debug.Log("Block " + blockindex + " is moving towards: " + targetPosition);             // Log output while moving
    }

}