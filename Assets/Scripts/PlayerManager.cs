using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private GameObject playerPrefab = null;

    private GameObject playerObject;

    private void Awake()
    {
        // Ensure only one instance of PlayerManager exists over the entire project
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Delete a player if it exists
        GameObject existingPlayer = GameObject.FindWithTag("Player");
        if (existingPlayer != null) DestroyImmediate(existingPlayer);

        // Add listeners
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SpawnPlayerAtSpawnpoint();
    }

    private void SpawnPlayerAtSpawnpoint()
    {
        // Expect a spawnpoint object
        GameObject spawnPoint = GameObject.FindWithTag("Spawnpoint");
        if (spawnPoint == null)
        {
            Debug.LogError("No spawnpoint found in the scene. Please add a GameObject with the tag 'Spawnpoint'.");
            return;
        }

        // Delete any existing players in the scene
        GameObject[] existingPlayers = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject existingPlayer in existingPlayers)
        {
            Destroy(existingPlayer);
        }

        // Instantiate the player at the spawnpoint
        playerObject = Instantiate(playerPrefab, spawnPoint.transform.position, spawnPoint.transform.rotation);
    }

    private void OnGUI()
    {
        // Simple buttons for changing scenes
        GUIStyle style = new GUIStyle(GUI.skin.button)
        {
            fontSize = 20,
            alignment = TextAnchor.MiddleCenter
        };
        if (GUI.Button(new Rect(10, 10, 250, 40), "Scene: Hub", style))
        {
            SceneManager.LoadScene("HubScene");
        }
        if (GUI.Button(new Rect(270, 10, 250, 40), "Scene: Area", style))
        {
            SceneManager.LoadScene("AreaScene");
        }
    }
}
