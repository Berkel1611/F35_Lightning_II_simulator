using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [SerializeField]
    GameObject pausePanel;
    [SerializeField] 
    MonoBehaviour missionManagerObject;
    IMissionManager missionManager;
    [SerializeField] 
    PlayerInput playerInput;
    [SerializeField]
    string mainMenuScene = "MainMenu";

    bool paused = false;

    void Awake()
    {
        if (missionManagerObject != null)
            missionManager = missionManagerObject as IMissionManager;
    }

    private void Start()
    {
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void TogglePause()
    {
        paused = !paused;
        pausePanel.SetActive(paused);
        Time.timeScale = paused ? 0f : 1f;
        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = paused;

        if (playerInput != null)
        {
            if (paused) playerInput.DeactivateInput();
            else playerInput.ActivateInput();
        }
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        if (context.performed && !missionManager.MissionEnded)
            TogglePause();
    }

    public void OnResume() => TogglePause();

    public void OnReturnToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }
}
