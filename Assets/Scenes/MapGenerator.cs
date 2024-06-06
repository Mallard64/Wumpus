using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    public int numberOfCaves = 30;
    private System.Random rnd = new System.Random();
    public TriviaDisplay td;
    public Dictionary<string, Dictionary<string,string>> caveConnections = new Dictionary<string, Dictionary<string,string>>();
    public static MapGenerator instance = null;
    void Awake()
    {
        if (instance == null)
        {
            // If instance is not set, then set instance to this GameManager and make it persistent
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            // If instance already exists and it's not this, then destroy this to enforce a singleton pattern
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("Duplicate GameManager instance was destroyed.");
        }
    }

    void Start()
    {
        GenerateMapConnections();
    }

    void GenerateMapConnections()
    {
        // Initialize the list of connections for each cave
        for (int i = 1; i <= numberOfCaves; i++)
        {
            string caveName = $"Cave_{i:00}";
            caveConnections[caveName] = new Dictionary<string,string>();
        }

        // First, create a cycle to ensure all caves are connected in a loop
        for (int i = 1; i <= numberOfCaves; i++)
        {
            string currentCave = $"Cave_{i:00}";
            string nextCave = i == numberOfCaves ? "Cave_01" : $"Cave_{(i + 1):00}";
            string ind1 = "Left";
            string ind2 = "Right";
            if (caveConnections[nextCave].ContainsKey(ind2)) {
                ind1 = "Up";
                ind2 = "Down";
                if (caveConnections[nextCave].ContainsKey(ind2)) {
                    ind1 = "Right";
                    ind2 = "Left";
                    if (caveConnections[nextCave].ContainsKey(ind2)) {
                        ind1 = "Down";
                        ind2 = "Up";   
                    }       
                }
            }
            caveConnections[currentCave].Add(ind1, nextCave);
            caveConnections[nextCave].Add(ind2, currentCave);
        }
        for (int i = 1; i <= numberOfCaves; i++) {
            for (int j = 1; j <= numberOfCaves; j++) {
                if (i == j) { // || rnd.Next() > 2147483647/4) {
                    continue;
                }
                //random generation doesn't work lmao
                string currentCave = $"Cave_{i:00}";
                string nextCave = i == numberOfCaves ? "Cave_01" : $"Cave_{(i + 1):00}";
                string ind1 = "Left";
                string ind2 = "Right";
                if (caveConnections[nextCave].ContainsKey(ind2)) {
                    ind1 = "Up";
                    ind2 = "Down";
                    if (caveConnections[nextCave].ContainsKey(ind2)) {
                        ind1 = "Right";
                        ind2 = "Left";
                        if (caveConnections[nextCave].ContainsKey(ind2)) {
                            ind1 = "Down";
                            ind2 = "Up";
                            if (caveConnections[nextCave].ContainsKey(ind2)) {
                                ind1 = "bad";
                                ind2 = "bad";
                            }       
                        }       
                    }
                }
                if (ind1 != "bad") {
                    caveConnections[currentCave].Add(ind1, nextCave);
                    caveConnections[nextCave].Add(ind2, currentCave);
                    Debug.Log(ind1);
                }
            }
        }
    }

    public void LoadCave(string caveName)
    {
        if (!caveName.Contains("Cave"))
        {
            return;
        }
        td.gameObject.SendMessage("StartAnswer");
        SceneManager.LoadScene(caveName);
    }

    // Example method to get connections for the current cave
    public Dictionary<string,string> GetConnectionsForCave(string caveName)
    {
        if (caveConnections.ContainsKey(caveName))
        {
            return caveConnections[caveName];
        }
        return new Dictionary<string,string>();
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.LeftArrow)) {
            Debug.Log(GetConnectionsForCave(SceneManager.GetActiveScene().name)["Left"]);
            LoadCave(GetConnectionsForCave(SceneManager.GetActiveScene().name)["Left"]);
        }
        if (Input.GetKeyDown(KeyCode.RightArrow)) {
            LoadCave(GetConnectionsForCave(SceneManager.GetActiveScene().name)["Right"]);

        }
        if (Input.GetKeyDown(KeyCode.UpArrow)) {
            Debug.Log(GetConnectionsForCave(SceneManager.GetActiveScene().name)["Up"]);
            LoadCave(GetConnectionsForCave(SceneManager.GetActiveScene().name)["Up"]);
        }
        if (Input.GetKeyDown(KeyCode.DownArrow)) {
            Debug.Log(GetConnectionsForCave(SceneManager.GetActiveScene().name)["Down"]);
            LoadCave(GetConnectionsForCave(SceneManager.GetActiveScene().name)["Down"]);
        }
    }
}
