using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerPersistent : MonoBehaviour
{
    public GridInventory Inventory { get; private set; }
    public GearInventory Gear { get; private set; }

    private static PlayerPersistent instance;

    [Header("Prefabs")]
    [SerializeField] private GameObject playerPrefab = null;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            DestroyImmediate(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        // Delete a player if it exists
        GameObject existingPlayer = GameObject.FindWithTag("Player");
        if (existingPlayer != null) DestroyImmediate(existingPlayer);

        // Add listeners
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Setup inventories
        Inventory = new GridInventory(4, 3);
        Gear = new GearInventory();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SpawnPlayerAtSpawnpoint();
    }

    private void SpawnPlayerAtSpawnpoint()
    {
        // Expect a spawnpoint object
        GameObject spawnPoint = GameObject.FindWithTag("Spawnpoint");
        Debug.Assert(spawnPoint != null, "Spawnpoint not found in the scene. Please add a GameObject with the 'Spawnpoint' tag.");

        // Delete any existing players in the scene
        GameObject[] existingPlayers = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject existingPlayer in existingPlayers)
        {
            Destroy(existingPlayer);
        }

        // Instantiate the player at the spawnpoint
        GameObject playerObject = Instantiate(playerPrefab, spawnPoint.transform.position, spawnPoint.transform.rotation);
        Player player = playerObject.GetComponent<Player>();
        player.Init(this);
    }

    private void OnGUI()
    {
        // Simple buttons for changing scenes
        GUIStyle style = new GUIStyle(GUI.skin.button)
        {
            fontSize = 20,
            alignment = TextAnchor.MiddleCenter
        };
        if (GUI.Button(new Rect(10, Screen.height - 50, 250, 40), "Scene: Hub", style))
        {
            SceneManager.LoadScene("HubScene");
        }
        if (GUI.Button(new Rect(270, Screen.height - 50, 250, 40), "Scene: Area", style))
        {
            SceneManager.LoadScene("AreaScene");
        }
    }
}
