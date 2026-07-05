using UnityEngine;
using UnityEngine.SceneManagement;

public class FreeFlight : MonoBehaviour, IMissionManager
{
    [SerializeField] Plane playerPlane;
    [SerializeField] GameObject crashPanel;

    public bool MissionEnded { get; private set; } = false;

    void Start()
    {
        crashPanel.SetActive(false);
    }

    void Update()
    {
        if (MissionEnded) return;
        if (playerPlane != null && !playerPlane.IsAlive)
            ShowCrashPanel();
    }

    void ShowCrashPanel()
    {
        MissionEnded = true;
        crashPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OnRestart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnReturnToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}