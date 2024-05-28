using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NewBehaviourScript : MonoBehaviour
{
    public TMP tp;
    public Vector2[,] tm;
    public GameObject[,] k;
    public string[,] caveList;
    public int i = 0;
    public int j = 0;
    public bool canMove = true;
    // Start is called before the first frame update
    void Start()
    {
        tm = tp.allPositions;
        caveList = tp.locToCave;
        k = tp.kids;
    }

    // Update is called once per frame
    void Update()
    {
        k[i,j].GetComponent<Cell>().hasPlayer = true;
        if (SceneManager.sceneCount == 1) {
            canMove = true;
            SpriteRenderer s = GetComponent<SpriteRenderer>();
            s.enabled = true;
        }
        if (canMove) {
            bool changed = false;
            if (i % 2 == 1) {
                if (Input.GetKeyDown(KeyCode.W) && j < 5) {
                    k[i,j].GetComponent<Cell>().hasPlayer = false;
                    Debug.Log("UP");
                    j++;
                    changed = true;
                }
                if (Input.GetKeyDown(KeyCode.S) && j > 0) {
                    k[i,j].GetComponent<Cell>().hasPlayer = false;
                    j--;
                    changed = true;
                }
                if (Input.GetKeyDown(KeyCode.D) && i < 5) {
                    k[i,j].GetComponent<Cell>().hasPlayer = false;
                    i++;
                    changed = true;
                }
                if (Input.GetKeyDown(KeyCode.A) && i > 0) {
                    k[i,j].GetComponent<Cell>().hasPlayer = false;
                    i--;
                    changed = true;
                }
                if (Input.GetKeyDown(KeyCode.E) && i < 5 && j < 5) {
                    k[i,j].GetComponent<Cell>().hasPlayer = false;
                    i++;
                    j++;
                    changed = true;
                }
                if (Input.GetKeyDown(KeyCode.Q) && i > 0 && j < 5) {
                    k[i,j].GetComponent<Cell>().hasPlayer = false;
                    i--;
                    j++;
                    changed = true;
                }
                
            }
            else {
                if (Input.GetKeyDown(KeyCode.W) && j < 5) {
                    k[i,j].GetComponent<Cell>().hasPlayer = false;
                    Debug.Log("UP");
                    j++;
                    changed = true;
                }
                if (Input.GetKeyDown(KeyCode.S) && j > 0) {
                    k[i,j].GetComponent<Cell>().hasPlayer = false;
                    j--;
                    changed = true;
                }
                if (Input.GetKeyDown(KeyCode.D) && i < 5 && j > 0) {
                    k[i,j].GetComponent<Cell>().hasPlayer = false;
                    i++;
                    j--;
                    changed = true;
                }
                if (Input.GetKeyDown(KeyCode.A) && i > 0 && j > 0) {
                    k[i,j].GetComponent<Cell>().hasPlayer = false;
                    i--;
                    j--;
                    changed = true;
                }
                if (Input.GetKeyDown(KeyCode.E) && i < 5) {
                    k[i,j].GetComponent<Cell>().hasPlayer = false;
                    i++;
                    changed = true;
                }
                if (Input.GetKeyDown(KeyCode.Q) && i > 0) {
                    k[i,j].GetComponent<Cell>().hasPlayer = false;
                    i--;
                    changed = true;
                }
            }
            transform.position = new Vector3(tm[i,j].x, tm[i,j].y, 0);
            // if (changed && i == tp.wi && j == tp.wj) {
            //     SpriteRenderer s = GetComponent<SpriteRenderer>();
            //     s.enabled = false;
            //     canMove = false;
            //     SceneManager.LoadScene("wumpusRoom", LoadSceneMode.Additive);
            //     SceneManager.SetActiveScene(SceneManager.GetSceneByName("wumpusRoom"));
            // }
            // else if (changed) {
            //     SpriteRenderer s = GetComponent<SpriteRenderer>();
            //     s.enabled = false;
            //     canMove = false;
            //     SceneManager.LoadScene(caveList[i,j], LoadSceneMode.Additive);
            //     SceneManager.SetActiveScene(SceneManager.GetSceneByName(caveList[i,j]));
            // }
        }
        
    }
}
