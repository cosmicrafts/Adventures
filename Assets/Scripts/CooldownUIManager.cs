using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CooldownUIManager : MonoBehaviour
{
    public TextMeshProUGUI timerText;               // Reference to the TMP text for the timer
    public TextMeshProUGUI energyCostText;          // Reference to the TMP text for energy cost
    public Image radialTimer;                        // Reference to the Image for radial timer

    private float cooldownDuration;                  // Duration of the cooldown
    private float currentCooldown;                   // Current cooldown time
    private bool isCooldownActive = false;          // Is the cooldown active

    public void StartCooldown(float duration, float energyCost)
    {
        cooldownDuration = duration;
        currentCooldown = duration;
        energyCostText.text = $"{energyCost}";
        isCooldownActive = true;

        // Reset radial timer fill
        radialTimer.fillAmount = 1f; 
    }

    private void Update()
    {
        if (isCooldownActive)
        {
            currentCooldown -= Time.deltaTime;
            timerText.text = $"{currentCooldown:F1}"; // Update timer display
            radialTimer.fillAmount = currentCooldown / cooldownDuration; // Update radial fill

            if (currentCooldown <= 0)
            {
                isCooldownActive = false;
                timerText.text = ""; // Clear timer text
            }
        }
    }
}
