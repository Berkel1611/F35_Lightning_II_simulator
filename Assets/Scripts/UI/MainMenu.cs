using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Zarz¹dza g³ównym menu gry, umo¿liwiaj¹c graczowi wybór misji do rozegrania lub wyjœcie z gry. Odpowiada za ³adowanie odpowiednich scen na podstawie wyboru gracza oraz zapewnia, ¿e kursor jest widoczny i odblokowany w menu.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("Nazwy scen")]
    [SerializeField]
    string tutorialScene = "Tutorial";
    [SerializeField]
    string mission1Scene = "Mission1";
    [SerializeField]
    string mission2Scene = "Mission2";
    [SerializeField] 
    string freeFlightScene = "FreeFlight";

    private void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void LoadTutorial() => SceneManager.LoadScene(tutorialScene);
    public void LoadMission1() => SceneManager.LoadScene(mission1Scene);
    public void LoadMission2() => SceneManager.LoadScene(mission2Scene);
    public void LoadFreeFlight() => SceneManager.LoadScene(freeFlightScene);

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
