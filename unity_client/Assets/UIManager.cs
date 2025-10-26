using UnityEngine;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public Camera mainCamera;
    public AudioSource swipeSound;
    public UnityEngine.UI.Image panelBg;

    private bool isAnimating = false;
    private float duration = 0.3f;
    private float lastTapTime = 0f;
    private bool isDarkMode = false;

    // ======= SWIPE SCREENS =======
    public void ShowMessages() => TryStartFlip("Messages", Color.green);
    public void ShowHome() => TryStartFlip("Home", Color.blue);
    public void ShowSettings() => TryStartFlip("Settings", Color.gray);
    public void ShowNotifications() => TryStartFlip("Notifications", Color.magenta);
    public void ShowQuickActions() => TryStartFlip("Quick Actions", Color.cyan);

    private void TryStartFlip(string newText, Color targetColor)
    {
        if (!isAnimating)
            StartCoroutine(FlipText(newText, targetColor));
    }

    private IEnumerator FlipText(string newText, Color targetColor)
    {
        isAnimating = true;
        float elapsed = 0f;
        Vector3 startScale = titleText.transform.localScale;

        if (swipeSound != null)
            swipeSound.Play();

        // Shrink
        while (elapsed < duration / 2)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (duration / 2);
            titleText.transform.localScale = Vector3.Lerp(startScale, new Vector3(0, 1, 1), progress);
            yield return null;
        }

        titleText.text = newText;

        if (mainCamera != null)
            StartCoroutine(SmoothColorTransition(targetColor));

        if (panelBg != null)
            StartCoroutine(SmoothPanelTransition(targetColor));

        // Expand
        elapsed = 0f;
        while (elapsed < duration / 2)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (duration / 2);
            titleText.transform.localScale = Vector3.Lerp(new Vector3(0, 1, 1), startScale, progress);
            yield return null;
        }

        isAnimating = false;
    }

    // ======= COLOR TRANSITIONS =======
    private IEnumerator SmoothColorTransition(Color targetColor)
    {
        Color startColor = mainCamera.backgroundColor;
        float t = 0f;
        float transitionTime = 0.6f;

        while (t < 1f)
        {
            t += Time.deltaTime / transitionTime;
            mainCamera.backgroundColor = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }
    }

    private IEnumerator SmoothPanelTransition(Color targetColor)
    {
        Color startColor = panelBg.color;
        float t = 0f;
        float transitionTime = 0.6f;
        targetColor.a = 0.25f;

        while (t < 1f)
        {
            t += Time.deltaTime / transitionTime;
            panelBg.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }
    }

    // ======= ADVANCED GESTURES =======
    public void ConfirmSelection()
    {
        if (!isAnimating)
            StartCoroutine(ShowTemporaryMessage("Confirmed!", Color.yellow));
    }

    public void AdjustControl(float amount)
    {
        float normalized = Mathf.Clamp01(amount);
        titleText.text = $"Brightness: {normalized:0.00}";
        if (mainCamera != null)
            mainCamera.backgroundColor = Color.Lerp(Color.black, Color.white, normalized);
    }

    // ======= EXPLICIT MODE CONTROL =======
    public void SetDarkMode(bool dark)
    {
        isDarkMode = dark;
        Color target = dark ? Color.black : Color.white;
        mainCamera.backgroundColor = target;
        titleText.text = dark ? "Dark Mode" : "Light Mode";
    }

    // ======= DOUBLE-TAP TOGGLE (manual) =======
    public void HandleDoubleTap()
    {
        if (Time.time - lastTapTime < 0.7f)
        {
            SetDarkMode(!isDarkMode);
        }
        else
        {
            titleText.text = "Tap again...";
        }

        lastTapTime = Time.time;
    }

    // ======= TEMPORARY MESSAGES =======
    private IEnumerator ShowTemporaryMessage(string message, Color highlightColor)
    {
        string previousText = titleText.text;
        Color originalColor = titleText.color;
        titleText.text = message;
        titleText.color = highlightColor;
        titleText.alpha = 0f;

        for (float t = 0; t < 1f; t += Time.deltaTime * 3f)
        {
            titleText.alpha = t;
            yield return null;
        }

        yield return new WaitForSeconds(0.6f);

        for (float t = 1f; t > 0f; t -= Time.deltaTime * 3f)
        {
            titleText.alpha = t;
            yield return null;
        }

        titleText.text = previousText;
        titleText.color = originalColor;
        titleText.alpha = 1f;
    }
}
