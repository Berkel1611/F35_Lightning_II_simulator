using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Główny skrypt misji samouczka, który zarządza sekwencją etapów, celami, checkpointami i interakcjami z graczem. Odpowiada za przechodzenie między etapami (start, checkpointy, cele naziemne, lądowanie), aktualizację UI z podpowiedziami i celami oraz obsługę zakończenia misji (sukces/niepowodzenie). Ten skrypt integruje wszystkie elementy misji w spójną całość, zapewniając płynne doświadczenie dla gracza podczas nauki podstaw gry.
/// </summary>
public class TutorialMission : MonoBehaviour, IMissionManager
{
    [Header("Gracz")]
    [SerializeField]
    Plane playerPlane;
    [SerializeField]
    PlayerController playerController;
    [SerializeField]
    PlaneHUD playerHUD;

    [Header("Checkpointy")]
    [SerializeField]
    List<CheckpointRing> checkpointRings = new List<CheckpointRing>();

    [Header("Cele naziemne")]
    [SerializeField]
    List<GameObject> groundTargets = new List<GameObject>();

    [Header("Strefa lądowania")]
    [SerializeField]
    LandingZone landingZone;

    [Header("UI")]
    [SerializeField]
    TextMeshProUGUI hintText;
    [SerializeField] 
    TextMeshProUGUI controlsTitleText;
    [SerializeField]
    TextMeshProUGUI controlsText;
    [SerializeField]
    GameObject overlayPanel;
    [SerializeField]
    TextMeshProUGUI resultText;
    [SerializeField] 
    TextMeshProUGUI goalStart;
    [SerializeField] 
    TextMeshProUGUI goalCheckpoints;
    [SerializeField] 
    TextMeshProUGUI goalTargets;
    [SerializeField] 
    TextMeshProUGUI goalLanding;
    [SerializeField] 
    Image iconStart;
    [SerializeField] 
    Image iconCheckpoints;
    [SerializeField] 
    Image iconTargets;
    [SerializeField] 
    Image iconLanding;
    [SerializeField] 
    Sprite spriteDone;
    [SerializeField] 
    Sprite spritePending;
    [SerializeField]
    string mainMenuScene = "MainMenu";
    [SerializeField]
    float landingObjectiveMinDistance = 3000f;

    // Stan wewnętrzny
    enum Stage { Start, Checkpoints, Targets, Landing, Completed, Failed }
    Stage currentStage = Stage.Start;
    Stage stageReached = Stage.Start;

    int currentCheckpoint = 0;
    int currentTarget = 0;
    List<IDamageable> targetDamageables = new List<IDamageable>();
    bool missionEnded = false;
    bool engineWasRunning = false;

    public bool MissionEnded => missionEnded;

    private void Start()
    {
        overlayPanel.SetActive(false);

        // Zbierz IDamageable z celów naziemnych
        foreach (var go in groundTargets)
        {
            if (go == null) continue;
            var d = go.GetComponent<IDamageable>();
            if (d != null)
                targetDamageables.Add(d);
        }

        // Dezaktywuj cele naziemne i pierścienie
        foreach (var obj in groundTargets)
            if (obj != null)
                obj.SetActive(false);
        foreach (var ring in checkpointRings)
            if (ring != null)
                ring.SetActive(false);

        // Subskrybuj zdarzenia ringów
        for (int i = 0; i < checkpointRings.Count; i++)
        {
            int idx = i;
            if (checkpointRings[idx] != null)
                checkpointRings[idx].OnRingPassed.AddListener(() => OnRingPassed(idx));
        }

        // Subskrybuj strefę lądowania
        if (landingZone != null)
            landingZone.OnLanded += OnPlayerLanded;

        EnterStage(Stage.Start);
    }

    private void Update()
    {
        if (missionEnded) return;

        // Sprawdź śmierć gracza
        if (playerPlane == null || !playerPlane.IsAlive)
        {
            EnterStage(Stage.Failed);
            return;
        }

        // Etap Start
        if (currentStage == Stage.Start)
        {
            bool isRunning = playerController != null && playerController.EngineState == PlayerController.EngineStartState.Running;
            if (isRunning != engineWasRunning)
            {
                engineWasRunning = isRunning;
                UpdateControlsUI();
            }

            if (playerPlane != null && playerPlane.Rigidbody.position.y > 100f)
                EnterStage(Stage.Checkpoints);
        }

        // Etap Targets
        if (currentStage == Stage.Targets)
        {
            if (currentTarget < targetDamageables.Count && !targetDamageables[currentTarget].IsAlive)
            {
                currentTarget++;
                if (currentTarget < groundTargets.Count)
                    ActivateNextTarget();
            }

            if (currentTarget >= targetDamageables.Count)
                EnterStage(Stage.Landing);
        }

        SetHint(GetHint());
    }

    void EnterStage(Stage stage)
    {
        currentStage = stage;
        if (stage != Stage.Failed)
            stageReached = stage;
        UpdateControlsUI();

        switch (stage)
        {
            case Stage.Checkpoints:
                ActivateNextRing();
                break;
            case Stage.Targets:
                currentTarget = 0;
                ActivateNextTarget();
                break;
            case Stage.Landing:
                if (landingZone != null)
                    landingZone.Activate();
                if (playerHUD != null && landingZone != null)
                    playerHUD.SetMissionObjective(landingZone.transform, "LOTNISKO", landingObjectiveMinDistance);
                break;
            case Stage.Completed:
                if (playerHUD != null) playerHUD.ClearMissionObjective();
                EndMission(true);
                break;
            case Stage.Failed:
                if (playerHUD != null) playerHUD.ClearMissionObjective();
                EndMission(false);
                break;
        }
    }

    // Checkpointy

    void ActivateNextRing()
    {
        if (currentCheckpoint >= checkpointRings.Count)
        {
            EnterStage(Stage.Targets);
            return;
        }

        // Aktywny ring — zielony
        var current = checkpointRings[currentCheckpoint];
        if (current != null) current.SetActive(true);

        // Preview — następny niebieski
        if (currentCheckpoint + 1 < checkpointRings.Count)
        {
            var next = checkpointRings[currentCheckpoint + 1];
            if (next != null) next.SetPreview(true);
        }
    }

    public void OnRingPassed(int index)
    {
        if (index != currentCheckpoint) return;

        if (currentCheckpoint + 1 < checkpointRings.Count)
            checkpointRings[currentCheckpoint + 1].Hide();

        currentCheckpoint++;
        ActivateNextRing();
    }

    // Cele naziemne

    void ActivateNextTarget()
    {
        if (currentTarget >= groundTargets.Count) return;
        var target = groundTargets[currentTarget];
        if (target != null)
            target.SetActive(true);
    }

    // Lądowanie
    public void OnPlayerLanded()
    {
        if (currentStage != Stage.Landing) return;
        EnterStage(Stage.Completed);
    }

    // Zakończenie
    void EndMission(bool success)
    {
        if (missionEnded) return;
        missionEnded = true;

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        resultText.text = success ? "MISSION COMPLETE" : "MISSION FAILED";
        resultText.color = success
            ? new Color(0.29f, 0.60f, 0.73f)
            : new Color(0.75f, 0.27f, 0.23f);

        UpdateOverlayGoals();
        overlayPanel.SetActive(true);
    }

    // UI

    void SetHint(string text)
    {
        if (hintText != null)
            hintText.text = text;
    }

    string GetHint()
    {
        switch (currentStage)
        {
            case Stage.Start:
                if (playerController != null && playerController.EngineState != PlayerController.EngineStartState.Running)
                    return "Etap 1/4\nOdpal silnik (R).";
                return "Etap 1/4\nUstaw się na pasie, zwiększ przepustnicę i wystartuj.";
            case Stage.Checkpoints:
                return $"Etap 2/4\nPrzelec przez okręgi ({currentCheckpoint}/{checkpointRings.Count}).";
            case Stage.Targets:
                return $"Etap 3/4\nZniszcz cele naziemne ({CountDestroyed()}/{targetDamageables.Count}).";
            case Stage.Landing:
                return "Etap 4/4\nWróć na lotnisko i wyląduj w wyznaczonej strefie.";
            case Stage.Completed:
                return "Misja zakończona!";
            case Stage.Failed:
                return "Misja nieudana.";
            default:
                return "";
        }
    }

    void UpdateControlsUI()
    {
        if (controlsText == null) return;

        switch (currentStage)
        {
            case Stage.Start:
                controlsTitleText.text = "STEROWANIE";
                if (playerController != null && playerController.EngineState != PlayerController.EngineStartState.Running)
                {
                    controlsText.text = "R    odpal silnik";
                }
                else
                {
                    controlsText.text =
                        "W / S      przepustnica\n" +
                        "↑ ↓ ← →    lotki / skok\n" +
                        "Q / E      ster kierunku\n" +
                        "G          podwozie";
                }
                break;
            case Stage.Checkpoints:
                controlsTitleText.text = "STEROWANIE";
                controlsText.text =
                    "↑ ↓ ← →    lotki / skok\n" +
                    "Q / E      ster kierunku\n" +
                    "W / S      przepustnica";
                break;
            case Stage.Targets:
                controlsTitleText.text = "UZBROJENIE";
                controlsText.text =
                    "M          master arm\n" +
                    "Tab        zmiana broni\n" +
                    "Spacja     ogień";
                break;
            case Stage.Landing:
                controlsTitleText.text = "LĄDOWANIE";
                controlsText.text =
                    "W / S      przepustnica\n" +
                    "↑ ↓ ← →    lotki / skok\n" +
                    "G          podwozie\n" +
                    "W          hamulec";
                break;
        }
    }

    void UpdateOverlayGoals()
    {
        bool startDone = stageReached >= Stage.Checkpoints;
        bool checkpointsDone = stageReached >= Stage.Targets;
        bool targetsDone = stageReached >= Stage.Landing;
        bool landingDone = stageReached == Stage.Completed;

        SetGoal(goalStart, iconStart, spriteDone, spritePending, "start i wznoszenie", startDone);
        SetGoal(goalCheckpoints, iconCheckpoints, spriteDone, spritePending, $"checkpointy ({Mathf.Min(currentCheckpoint, checkpointRings.Count)}/{checkpointRings.Count})", checkpointsDone);
        SetGoal(goalTargets, iconTargets, spriteDone, spritePending, $"cele naziemne ({CountDestroyed()}/{targetDamageables.Count})", targetsDone);
        SetGoal(goalLanding, iconLanding, spriteDone, spritePending, "lądowanie", landingDone);
    }

    void SetGoal(TextMeshProUGUI tmp, Image icon, Sprite iconDone, Sprite iconPending, string label, bool done)
    {
        if (tmp != null)
        {
            tmp.text = label;
            tmp.color = done
                ? new Color(0.29f, 0.60f, 0.73f)
                : new Color(0.33f, 0.50f, 0.60f);
        }
        if (icon != null)
        {
            icon.sprite = done ? iconDone : iconPending;
            icon.color = done
                ? new Color(0.29f, 0.60f, 0.73f)
                : new Color(0.33f, 0.50f, 0.60f);
        }
    }

    int CountDestroyed()
    {
        int n = 0;
        foreach (var d in targetDamageables)
            if (!d.IsAlive)
                n++;
        return n;
    }

    // Przyciski

    public void OnReturnToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }

    public void OnRestartMission()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
