using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Automatycznie sadzi drzewa na Unity Terrain na podstawie koloru tekstury satelitarnej.
/// Losuje gatunek drzewa z listy z wagami — jeden przebieg sadzi wszystkie gatunki.
/// </summary>
[ExecuteInEditMode]
public class SatelliteTreePainter : MonoBehaviour
{
    [Header("Tekstura satelitarna")]
    public Texture2D satelliteTexture;

    [Header("Filtr koloru zieleni")]
    [Range(0f, 1f)] public float minGreen = 0.17f;
    [Range(0f, 1f)] public float maxRed = 0.23f;
    [Range(0f, 1f)] public float maxBlue = 0.24f;
    [Range(0f, 0.3f)] public float minGreenDominance = 0.04f;

    [Header("Gęstość i losowość")]
    [Range(1, 20)] public int samplingStep = 5;
    [Range(0f, 1f)] public float plantingProbability = 0.3f;
    public float positionJitter = 5f;

    [Header("Ochrona strefy lotniska")]
    public bool excludeAirportZone = true;
    public Vector2 airportCenter = new Vector2(0.5f, 0.5f);
    [Range(0f, 0.5f)] public float airportExclusionRadius = 0.15f;

    [Header("Gatunki drzew (indeksy z listy Terrain)")]
    public int[] treePrototypeIndices = new int[] { 0, 1, 2, 3, 4, 5 };
    public float[] treeWeights = new float[] { 1f, 1f, 1f, 1f, 1f, 1f };

    [Header("Skala drzew")]
    public float minTreeHeight = 0.8f;
    public float maxTreeHeight = 1.2f;

#if UNITY_EDITOR
    // Losuje gatunek drzewa z wagami
    private int PickTreeIndex()
    {
        float totalWeight = 0f;
        foreach (float w in treeWeights) totalWeight += w;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        for (int i = 0; i < treePrototypeIndices.Length; i++)
        {
            cumulative += (i < treeWeights.Length ? treeWeights[i] : 1f);
            if (roll <= cumulative)
                return treePrototypeIndices[i];
        }
        return treePrototypeIndices[0];
    }

    [ContextMenu("Sadź drzewa na podstawie satelity")]
    public void PlantTrees()
    {
        Terrain terrain = GetComponent<Terrain>();
        if (terrain == null) { Debug.LogError("Brak komponentu Terrain."); return; }
        if (satelliteTexture == null) { Debug.LogError("Przypisz teksturę satelitarną."); return; }

        TerrainData td = terrain.terrainData;
        if (td.treePrototypes.Length == 0) { Debug.LogError("Dodaj gatunki drzew do Terrain."); return; }
        if (treePrototypeIndices == null || treePrototypeIndices.Length == 0)
        {
            Debug.LogError("Podaj przynajmniej jeden indeks gatunku.");
            return;
        }

        // Walidacja indeksów
        for (int i = 0; i < treePrototypeIndices.Length; i++)
        {
            if (treePrototypeIndices[i] >= td.treePrototypes.Length)
            {
                Debug.LogWarning($"Indeks {treePrototypeIndices[i]} poza zakresem — zamieniono na 0.");
                treePrototypeIndices[i] = 0;
            }
        }

        int texWidth = satelliteTexture.width;
        int texHeight = satelliteTexture.height;
        Color[] pixels = satelliteTexture.GetPixels();

        // Zachowaj istniejące drzewa
        List<TreeInstance> trees = new List<TreeInstance>(td.treeInstances);

        int planted = 0;
        Random.InitState(System.Environment.TickCount);

        for (int py = 0; py < texHeight; py += samplingStep)
        {
            for (int px = 0; px < texWidth; px += samplingStep)
            {
                Color c = pixels[py * texWidth + px];

                bool isGreen = c.g >= minGreen
                    && c.r <= maxRed
                    && c.b <= maxBlue
                    && (c.g - c.r) >= minGreenDominance;

                if (!isGreen) continue;

                float normX = (float)px / texWidth;
                float normZ = (float)py / texHeight;

                if (excludeAirportZone)
                {
                    float dx = normX - airportCenter.x;
                    float dz = normZ - airportCenter.y;
                    if (Mathf.Sqrt(dx * dx + dz * dz) > airportExclusionRadius)
                        continue;
                }

                if (Random.value > plantingProbability) continue;

                float jitterX = Random.Range(-positionJitter, positionJitter) / td.size.x;
                float jitterZ = Random.Range(-positionJitter, positionJitter) / td.size.z;
                float finalX = Mathf.Clamp01(normX + jitterX);
                float finalZ = Mathf.Clamp01(normZ + jitterZ);
                float terrainY = td.GetInterpolatedHeight(finalX, finalZ) / td.size.y;

                TreeInstance tree = new TreeInstance
                {
                    position = new Vector3(finalX, terrainY, finalZ),
                    prototypeIndex = PickTreeIndex(),
                    heightScale = Random.Range(minTreeHeight, maxTreeHeight),
                    widthScale = Random.Range(minTreeHeight, maxTreeHeight),
                    color = Color.white,
                    lightmapColor = Color.white,
                    rotation = Random.Range(0f, Mathf.PI * 2f)
                };

                trees.Add(tree);
                planted++;
            }
        }

        td.treeInstances = trees.ToArray();
        terrain.Flush();
        Debug.Log($"Posadzono {planted} drzew. Łącznie na terenie: {td.treeInstances.Length}");
        EditorUtility.SetDirty(terrain);
    }

    [ContextMenu("Usuń wszystkie drzewa z terenu")]
    public void ClearAllTrees()
    {
        Terrain terrain = GetComponent<Terrain>();
        if (terrain == null) return;
        terrain.terrainData.treeInstances = new TreeInstance[0];
        terrain.Flush();
        Debug.Log("Usunięto wszystkie drzewa.");
        EditorUtility.SetDirty(terrain);
    }
#endif
}