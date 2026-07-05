using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
public class TargetGroup
{
    public string label;
    public List<GameObject> targets = new List<GameObject>();
}

/// <summary>
/// Odpowiada za zarządzanie misją, celami, stanem gry i interfejsem końcowym. Monitoruje stan gracza i celów, aktualizuje UI oraz obsługuje zakończenie misji (sukces/niepowodzenie).
/// </summary>
public class MissionManager : MonoBehaviour, IMissionManager
{
    [Header("Gracz")]
    [SerializeField]
    Plane playerPlane;
    [SerializeField]
    PlaneHUD playerHUD;

    [Header("Cele misji")]
    [SerializeField]
    List<TargetGroup> targetGroups = new List<TargetGroup>();
    [SerializeField]
    GameObject baseObject; 

    [Header("UI")]
    [SerializeField]
    GameObject overlayPanel;
    [SerializeField]
    TextMeshProUGUI resultText;
    [SerializeField] 
    float overlayDelay = 2f;
    [SerializeField]
    TextMeshProUGUI goalsTitle;
    [SerializeField]
    Transform goalsContainer;
    [SerializeField]
    GameObject goalRowPrefab;
    [SerializeField]
    Sprite spriteDone;
    [SerializeField]
    Sprite spritePending;
    [SerializeField]
    float objectiveMinDistance = 5000f;

    [Header("Sceny")]
    [SerializeField]
    string mainMenuScene = "MainMenu";

    // Stan wewnętrzny
    bool missionEnded = false;

    // Dane runtime dla każdej grupy
    class GroupRuntime
    {
        public List<IDamageable> damageables = new List<IDamageable>();
        public Image icon;
        public TextMeshProUGUI label;
        public string baseLabel;
    }

    readonly List<GroupRuntime> groups = new List<GroupRuntime>();

    public bool MissionEnded => missionEnded;

    void Start()
    {
        overlayPanel.SetActive(false);

        foreach (var group in targetGroups)
        {
            var runtime = new GroupRuntime();
            runtime.baseLabel = group.label;

            foreach (var go in group.targets)
                if (go != null)
                {
                    var d = go.GetComponent<IDamageable>();
                    if (d != null) runtime.damageables.Add(d);
                }

            (runtime.icon, runtime.label) = CreateRow(BuildLabel(runtime));
            groups.Add(runtime);
        }

        UpdateGoalsUI();

        if (goalsTitle != null)
            goalsTitle.text = "CELE MISJI";

        if (playerHUD != null && baseObject != null)
            playerHUD.SetMissionObjective(baseObject.transform, "BAZA WROGA", objectiveMinDistance);
    }

    void Update()
    {
        if (missionEnded) return;

        CheckPlayerDead();
        CheckAllTargetsDestroyed();
    }

    void CheckPlayerDead()
    {
        if (playerPlane == null) return;
        if (!playerPlane.IsAlive)
            EndMission(false);
    }

    void CheckAllTargetsDestroyed()
    {
        UpdateGoalsUI();

        foreach (var g in groups)
            foreach (var d in g.damageables)
                if (d.IsAlive) return;

        EndMission(true);
    }

    void EndMission(bool success)
    {
        if (missionEnded) return;
        missionEnded = true;

        Time.timeScale = 0f;

        resultText.text = success ? "MISSION COMPLETE" : "MISSION FAILED";
        resultText.color = success
            ? new Color(0.2f, 1f, 0.2f)
            : new Color(1f, 0.2f, 0.2f);

        overlayPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        StartCoroutine(ShowOverlayDelayed());
    }

    IEnumerator ShowOverlayDelayed()
    {
        yield return new WaitForSeconds(overlayDelay);
        Time.timeScale = 0f;
        overlayPanel.SetActive(true);
    }

    void UpdateGoalsUI()
    {
        foreach (var g in groups)
        {
            bool done = g.damageables.TrueForAll(d => !d.IsAlive);
            SetRow(g.icon, g.label, BuildLabel(g), done);
        }
    }

    string BuildLabel(GroupRuntime g)
    {
        if (g.damageables.Count <= 1)
            return g.baseLabel;

        int total = g.damageables.Count;
        int destroyed = g.damageables.FindAll(d => !d.IsAlive).Count;
        return $"{g.baseLabel} ({destroyed}/{total})";
    }

    (Image, TextMeshProUGUI) CreateRow(string label)
    {
        var row = Instantiate(goalRowPrefab, goalsContainer);
        var icon = row.GetComponentInChildren<Image>();
        var tmp = row.GetComponentInChildren<TextMeshProUGUI>();
        tmp.text = label;
        return (icon, tmp);
    }

    void SetRow(Image icon, TextMeshProUGUI tmp, string label, bool done)
    {
        tmp.text = label;
        Color col = done
            ? new Color(0.29f, 0.73f, 0.29f)
            : new Color(0.70f, 0.70f, 0.70f);
        icon.sprite = done ? spriteDone : spritePending;
        icon.color = col;
        tmp.color = col;
    }

    public void OnReturnToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }

    public void OnRestartMission()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ForceSuccess() => EndMission(true);
    public void ForceFail() => EndMission(false);
}