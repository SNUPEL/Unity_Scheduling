using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

// FloatManager class manages the display and update of a float value in a Unity UI text element
public class FloatManager : MonoBehaviour
{
    public string text = "";                                                    // Initial string (currently unused in the implementation)
    private float floatValue;                                                   // The float value that will be displayed and updated
    public bool btn_active;                                                     // Flag indicating whether updates to the UI are active
    public Text text_ui;                                                        // UI Text element where the float value will be displayed
    float time;                                                                 // Time variable (currently unused)
    //private Action onValueChanged;                                              // Action delegate that will be triggered when the value changes

    string ShowUI()                                                             // Generates a formatted string to display the current float value with 2 decimal places
    {
        string text = string.Format("Tardiness:{0:F2}", floatValue);            // Format the string to show the float value with two decimal places
        return text;                                                            // Returns a string in the format "Tardiness: {floatValue:F2}"
    }
    public void UpdateValue(float delta)                                        // Updates the float value by adding the specified delta
    {
        floatValue += delta;                                                    // Increment the current float value by delta
        //onValueChanged?.Invoke();                                               // Trigger the onValueChanged delegate to handle any dependent logic
    }

    private void Start()                                                        //Initializes the FloatManager by setting the UI update flag to active
    {
        btn_active = true;                                                      // Enable UI updates by default
        //onValueChanged += () =>                                                 // Subscribe a lambda function to the onValueChanged delegate
        //{
        //    if (btn_active) text_ui.text = ShowUI();                            // The lambda checks if the button is active and updates the UI text accordingly
        //};                                                                                   
        //onValueChanged.Invoke();                                                // Invoke the onValueChanged delegate to initialize UI text on startup
    }
    
    private void Update()                                                       // Updates the UI text with the current float value every frame if updates are active
    {
        if (btn_active)
        {
            text_ui.text = ShowUI();                                            // Update the UI text element with the formatted float value
        }
        else
        {
            return;                                                             // If updates are inactive, do nothing
        }
        
    }
}