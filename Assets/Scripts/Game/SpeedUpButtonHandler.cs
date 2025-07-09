using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpeedUpButtonHandler : MonoBehaviour
{
    public GameBoard gameBoard;      // Reference to the script that contains refillDelay and destroyDelay
    public TMP_Text buttonText;     // Reference to the Button component
    public Button speedUpButton;

    private float originalRefillDelay;
    private float originalDestroyDelay;

    private bool isSpeedUp = false;

    void Start()
    {
        if (gameBoard == null)
            Debug.LogError("GameBoard reference not set!");

        if (speedUpButton == null || buttonText == null)
            Debug.LogError("Button or Text reference not set!");

        // Save original values
        originalRefillDelay = gameBoard.refillDelay;
        originalDestroyDelay = gameBoard.destroyDelay;

        // Set default text
        buttonText.text = ">";

        // Add click listener
        speedUpButton.onClick.AddListener(ToggleSpeed);
    }

    private void ToggleSpeed()
    {
        isSpeedUp = !isSpeedUp;

        if (isSpeedUp)
        {
            // Speed up
            gameBoard.refillDelay = originalRefillDelay/2;
            gameBoard.destroyDelay = originalDestroyDelay/2;
            buttonText.text = ">>";
        }
        else
        {
            // Revert to original speed
            gameBoard.refillDelay = originalRefillDelay;
            gameBoard.destroyDelay = originalDestroyDelay;
            buttonText.text = ">";
        }
    }
}

