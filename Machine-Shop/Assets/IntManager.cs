using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

 // IntManager class handles displaying and updating an integer value in a Unity UI text element
public class IntManager : MonoBehaviour
{
    public string text = "";                                                    // Initial string (not used directly in this implementation)
    private float intValue;                                                     // The integer value that will be displayed and updated
    public bool btn_active;                                                     // Flag indicating whether updates to the UI are active
    public Text text_ui;                                                        // UI Text element where the integer value will be displayed
    float time;                                                                 // Time variable (currently unused)
    private Action onValueChanged;                                              // Action delegate that will be triggered when the value changes

    string ShowUI()                                                             //Generates a formatted string to display the current integer value
    {
        string text = string.Format("Setup:{0}", intValue);                     // Format the string with the current integer value
        return text;                                                            // Returns formatted string as "Setup: {intValue}"
    }
    public void UpdateValue(float delta)
    {
        intValue += delta;                                                      // Increment the current value by delta
        onValueChanged?.Invoke();                                               // Trigger the onValueChanged delegate to handle any dependent logic
    }

    private void Start()                                                        // Initializes the IntManager by setting the UI update flag to active        
    {
        btn_active = true;                                                      // Enable UI updates by default
        onValueChanged += () =>                                                 // Subscribe a lambda function to the onValueChanged delegate
        {
            if (btn_active) text_ui.text = ShowUI();                            // The lambda checks if the button is active and updates the UI text accordingly
        };
        onValueChanged.Invoke();                                                // Invoke the onValueChanged delegate to initialize UI text on startup
    }

    private void Update()                                                       // Updates the UI text with the current integer value every frame if updates are active
    {
        if (btn_active)
        {
            text_ui.text = ShowUI();                                            // Update the UI text element with the formatted integer value
        }
    }
}


