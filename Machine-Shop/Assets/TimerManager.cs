using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

// TimerManager class handles a simple stopwatch timer that updates a UI text element in Unity
public class TimerManager : MonoBehaviour
{
    public string m_Timer = @"00:00:00.000";                                    // Initial timer value displayed as a string in the format "hh:mm:ss.fff"

    public bool btn_active;                                                     // Flag indicating whether the timer is active or paused.
    public Text text_time;                                                      // UI Text element where the timer value will be displayed
    public float m_TotalSeconds;                                                // Total elapsed time in seconds since the timer started 
    public float endtime;                                                       // Reserved for a future feature where a target end time might be used
    float time;                                                                 // Accumulated time (not currently used in this implementation)

    // Updates the timer value as a formatted string using TimeSpan
    // Formatted string representing the elapsed time in "mm:ss.fff" format
    string StopwatchTimer()
    {
        m_TotalSeconds += Time.deltaTime;                                       // Increment total elapsed time by the time passed since the last frame
        TimeSpan timespan = TimeSpan.FromSeconds(m_TotalSeconds);               // Create a TimeSpan object from the total elapsed seconds
        string timer = string.Format("{0:00}:{1:00}.{2:000}",
            timespan.Minutes, timespan.Seconds, timespan.Milliseconds);         // Format the time into "mm:ss.fff" format

        return timer;                                                           // Return the formatted string
    }

    private void Start()
    {
        m_TotalSeconds = 0.0f;                                                  // Initialize total elapsed time to zero
        btn_active = true;                                                      // Set the timer to active initially
        // SetTimerOn();                                                        // The timer by calling SetTimerOn explicitly
    }

    private void Update()                                                       // Updates the UI text with the current timer value on every frame if the timer is active
    {
        if (btn_active)
        {
            time += Time.deltaTime;                                             // Increment time and update the text element with the current formatted timer value
            text_time.text = StopwatchTimer();
        }
        else
        {
            return;                                                             // If the timer is inactive, do nothing
        }
        
    }
}