using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Drives the player on the cave map: movement, arrow shooting, hazard detection, economy and HUD.
/// <para>
/// Encounters (Wumpus, pit, and the two shops) are handled by loading a dedicated scene additively
/// on top of the map. While that scene is up the map is hidden and input is frozen; the encounter
/// scene reports its outcome back through <see cref="CorrectAnswer"/> and <see cref="WrongAnswer"/>.
/// </para>
/// </summary>
public class PlayerScript : MonoBehaviour
{
    /// <summary>Arrows the player starts the run with.</summary>
    private const int StartingArrows = 3;

    /// <summary>Number of moves that still earn a coin. After this the cave stops paying out.</summary>
    private const int CoinEarningMoves = 100;

    /// <summary>Flat score every run starts from before turn, coin and arrow adjustments.</summary>
    private const int BaseScore = 100;

    /// <summary>Score awarded per unused arrow at the end of a run.</summary>
    private const int ScorePerArrow = 5;

    /// <summary>Score awarded for killing the Wumpus.</summary>
    private const int WumpusKillBonus = 50;

    /// <summary>Arrows granted for clearing the arrow-shop trivia round.</summary>
    private const int ArrowsPerPurchase = 2;

    /// <summary>
    /// Seconds an arrow flight, a bat flight, or the shooting-mode wind-up takes.
    /// Matched to the length of the corresponding animation clip.
    /// </summary>
    private const float ActionAnimationSeconds = 1.075f;

    /// <summary>Maximum gap between two taps for them to count as a double tap.</summary>
    private const float DoubleTapWindowSeconds = 0.3f;

    /// <summary>Fingers required, alongside a double tap, to reveal hazards for testing.</summary>
    private const int TestModeFingerCount = 3;

    /// <summary>Number of distinct hints the secret shop can hand out.</summary>
    private const int SecretHintKinds = 3;

    /// <summary>Pits and bat colonies are always placed in pairs, so hints pick one of two.</summary>
    private const int HazardPairSize = 2;

    /// <summary>One-in-three chance that the Wumpus shuffles rooms after the player fires.</summary>
    private const int WumpusRoamChanceDenominator = 3;

    /// <summary>Draw from <see cref="WumpusRoamChanceDenominator"/> that triggers a Wumpus move.</summary>
    private const int WumpusRoamTriggerValue = 2;

    /// <summary>Scene loaded when the player buys arrows.</summary>
    private const string ArrowShopSceneName = "Cave_01";

    /// <summary>Scene loaded when the player buys a hint.</summary>
    private const string SecretShopSceneName = "Cave_02";

    /// <summary>Scene loaded when the player falls into a pit.</summary>
    private const string PitSceneName = "Cave_03";

    /// <summary>Scene loaded when the player walks into the Wumpus.</summary>
    private const string WumpusSceneName = "wumpusRoom";

    /// <summary>Scene loaded after the Wumpus is killed.</summary>
    private const string WinSceneName = "Win";

    /// <summary>Scene loaded on any losing condition.</summary>
    private const string LoseSceneName = "Lose";

    /// <summary>Prompt used for the ambient trivia shown after each move.</summary>
    private const string RandomFactPrompt = "Give me a random fact.";

    /// <summary>True while a bat is carrying the player to a random room.</summary>
    [FormerlySerializedAs("ishooting")]
    public bool isBatEncounterActive = false;

    /// <summary>The cave generator that owns the grid, the Wumpus and the hazards.</summary>
    [FormerlySerializedAs("tp")]
    public CellGenerator cellGenerator;

    /// <summary>Root canvas for the map HUD, hidden while an encounter scene is open.</summary>
    [FormerlySerializedAs("c")]
    public Canvas gameCanvas;

    /// <summary>The room the player currently occupies.</summary>
    public Cell player;

    /// <summary>False while an encounter scene is open or an animation is playing.</summary>
    public bool canMove = true;

    /// <summary>True once the player has armed an arrow and is choosing a direction.</summary>
    public bool isShooting = false;

    /// <summary>Arrows left in the quiver. Reaching zero ends the run.</summary>
    public int numArrows = StartingArrows;

    /// <summary>Coins collected. Going negative ends the run.</summary>
    public int coins = 0;

    /// <summary>Moves still eligible to earn a coin.</summary>
    [FormerlySerializedAs("coinsleft")]
    public int coinsRemaining = CoinEarningMoves;

    /// <summary>Current score, recomputed every frame by <see cref="UpdateHud"/>.</summary>
    public int score = 0;

    /// <summary>Number of rooms the player has moved through.</summary>
    public int numTurns = 0;

    /// <summary>1 once the Wumpus has been killed, 0 otherwise. Feeds the score bonus.</summary>
    public int wumpusDead = 0;

    /// <summary>Guards against re-entering the Wumpus encounter for the room already resolved.</summary>
    public bool wumpusRoom = false;

    /// <summary>Guards against re-entering the pit encounter for the room already resolved.</summary>
    public bool pitRoom = false;

    /// <summary>Guards against re-triggering the bat encounter for the room already resolved.</summary>
    public bool batRoom = false;

    /// <summary>Button that opens the arrow shop.</summary>
    public Button arrowButton;

    /// <summary>Button that opens the hint shop.</summary>
    public Button secretButton;

    /// <summary>Button that toggles shooting mode.</summary>
    public Button shootButton;

    /// <summary>Leaderboard store that persists the result of the run.</summary>
    [FormerlySerializedAs("gd")]
    public GameData gameData;

    /// <summary>Field holding the player's name; defaults to the device name.</summary>
    [FormerlySerializedAs("ifield")]
    public InputField playerNameField;

    /// <summary>Map backdrop, hidden alongside the grid during encounters.</summary>
    public GameObject background;

    /// <summary>Single-line status banner used for warnings, hints and generated trivia.</summary>
    [FormerlySerializedAs("n1")]
    public TextMeshProUGUI statusText;

    /// <summary>HUD readout for the current score.</summary>
    [FormerlySerializedAs("t1")]
    public TextMeshProUGUI scoreText;

    /// <summary>HUD readout for the current coin count.</summary>
    [FormerlySerializedAs("t2")]
    public TextMeshProUGUI coinsText;

    /// <summary>HUD readout for the remaining arrows.</summary>
    [FormerlySerializedAs("t3")]
    public TextMeshProUGUI arrowsText;

    /// <summary>HUD readout for the number of turns taken.</summary>
    [FormerlySerializedAs("t4")]
    public TextMeshProUGUI turnsText;

    /// <summary>Bat animation shown while the player is carried to a new room.</summary>
    [FormerlySerializedAs("BatPrefab")]
    public GameObject batPrefab;

    /// <summary>Random source for hints, bat destinations and Wumpus roaming.</summary>
    public System.Random rnd = new System.Random();

    /// <summary>The player's sprite, hidden while an encounter scene is open.</summary>
    [FormerlySerializedAs("sr")]
    public SpriteRenderer playerSprite;

    private string playerName = "Player";
    private Cell[,] cells;
    private Vector2 touchStartPosition;
    private float touchStartTime;

    /// <summary>Wires up the shop buttons and caches the generated grid.</summary>
    private void Start()
    {
        arrowButton.onClick.AddListener(BuyArrows);
        secretButton.onClick.AddListener(BuySecret);
        shootButton.onClick.AddListener(EnterShootingMode);

        batPrefab.SetActive(false);
        cells = cellGenerator.cells;
        player = cells[0, 0];
        playerNameField.text = SystemInfo.deviceName;
    }

    /// <summary>
    /// Called by the trivia scene (via <c>SendMessage</c>) when the player clears a challenge.
    /// The reward depends on which challenge was being played.
    /// </summary>
    /// <param name="usage">
    /// Challenge identifier: <c>"arrow"</c>, <c>"secret"</c>, <c>"wumpus"</c> or <c>"pit"</c>.
    /// </param>
    private void CorrectAnswer(string usage)
    {
        playerName = playerNameField.text;

        switch (usage)
        {
            case "arrow":
                numArrows += ArrowsPerPurchase;
                break;
            case "secret":
                RevealSecret();
                break;
            case "wumpus":
                wumpusRoom = true;
                cellGenerator.moveWumpus();
                break;
            case "pit":
                pitRoom = true;
                cellGenerator.SetHazards();
                break;
        }

        RestoreMapView();
    }

    /// <summary>
    /// Called by the trivia scene (via <c>SendMessage</c>) when the player fails a challenge.
    /// Failing a Wumpus or pit challenge ends the run.
    /// </summary>
    /// <param name="usage">
    /// Challenge identifier: <c>"arrow"</c>, <c>"secret"</c>, <c>"wumpus"</c> or <c>"pit"</c>.
    /// </param>
    private void WrongAnswer(string usage)
    {
        if (usage == "wumpus")
        {
            EndRun(false, "Died to the wumpus. Wump wump...");
            return;
        }

        if (usage == "pit")
        {
            EndRun(false, "Had a great fall, just like Humpty Dumpty!");
            return;
        }

        RestoreMapView();
    }

    /// <summary>
    /// Called by the trivia scene (via <c>SendMessage</c>) to charge the player for an attempt.
    /// </summary>
    private void PayCoin()
    {
        coins--;
    }

    /// <summary>
    /// Pays out a hint: either a fresh generated fact, or the exact room number of one hazard.
    /// </summary>
    private void RevealSecret()
    {
        if (rnd.Next(2) == 0)
        {
            StartCoroutine(RequestRandomFact());
            return;
        }

        switch (rnd.Next(SecretHintKinds))
        {
            case 0:
                statusText.text = "Wumpus is at room " + cellGenerator.wumpus.GetCellIndex();
                break;
            case 1:
                statusText.text = "Pit is at room " + cellGenerator.pits[rnd.Next(HazardPairSize)].GetCellIndex();
                break;
            default:
                statusText.text = "Bats are at room " + cellGenerator.bats[rnd.Next(HazardPairSize)].GetCellIndex();
                break;
        }
    }

    /// <summary>Debug aid: prints the location of every hazard to the status banner.</summary>
    private void RevealHazardsForTesting()
    {
        statusText.text = "Wumpus: " + cellGenerator.wumpus.GetCellIndex()
            + " Pit: " + cellGenerator.pits[0].GetCellIndex() + " " + cellGenerator.pits[1].GetCellIndex()
            + " Bats: " + cellGenerator.bats[0].GetCellIndex() + " " + cellGenerator.bats[1].GetCellIndex();
    }

    /// <summary>
    /// Requests a one-line generated fact and shows it in the status banner. Does nothing when the
    /// API is unavailable, leaving the previous banner text in place.
    /// </summary>
    /// <returns>A coroutine to be driven with <c>StartCoroutine</c>.</returns>
    private IEnumerator RequestRandomFact()
    {
        return OpenAIClient.SendChatCompletion(
            RandomFactPrompt,
            OpenAIClient.ShortResponseTokens,
            response =>
            {
                string fact = OpenAIClient.ExtractMessageContent(response);
                if (!string.IsNullOrEmpty(fact))
                {
                    statusText.text = fact;
                }
            });
    }

    /// <summary>
    /// Recomputes the score, refreshes the HUD, prints the proximity warning for the current room,
    /// and ends the run if the player has run out of arrows or coins.
    /// </summary>
    private void UpdateHud()
    {
        playerName = playerNameField.text;

        ShowProximityWarning();

        if (numArrows <= 0 || coins < 0)
        {
            EndRun(false, "Went broke :(");
        }

        score = BaseScore - numTurns + coins + ScorePerArrow * numArrows + WumpusKillBonus * wumpusDead;
        scoreText.text = score.ToString();
        coinsText.text = coins.ToString();
        arrowsText.text = numArrows.ToString();
        turnsText.text = numTurns.ToString();
    }

    /// <summary>
    /// Prints a warning when a hazard sits in an adjacent room. Bats take priority over the
    /// Wumpus, which takes priority over pits, so only one hint is ever shown at a time.
    /// </summary>
    private void ShowProximityWarning()
    {
        if (player.IsNearBats())
        {
            statusText.text = "Bats nearby?";
        }
        else if (player.IsNearWumpus())
        {
            statusText.text = "I smell a wumpus!";
        }
        else if (player.IsNearPits())
        {
            statusText.text = "I feel a breeze...";
        }
    }

    /// <summary>Records the run on the leaderboard and loads the win or lose scene.</summary>
    /// <param name="won">True when the player killed the Wumpus.</param>
    /// <param name="epitaph">Short message shown next to the score on the leaderboard.</param>
    private void EndRun(bool won, string epitaph)
    {
        gameData.AddOrUpdatePlayerData(playerName, score, numTurns, coins, numArrows, won, epitaph);
        SceneManager.LoadScene(won ? WinSceneName : LoseSceneName);
    }

    /// <summary>Opens the arrow shop, if the player is not mid-encounter.</summary>
    private void BuyArrows()
    {
        if (canMove)
        {
            EnterEncounterScene(ArrowShopSceneName);
        }
    }

    /// <summary>Opens the hint shop, if the player is not mid-encounter.</summary>
    private void BuySecret()
    {
        if (canMove)
        {
            EnterEncounterScene(SecretShopSceneName);
        }
    }

    /// <summary>Toggles shooting mode on or off.</summary>
    private void EnterShootingMode()
    {
        if (!isShooting)
        {
            canMove = false;
            statusText.text = "Entering shooting mode. Select a direction to shoot";
            StartCoroutine(ActivateShootingAfterWindUp());
        }
        else
        {
            statusText.text = "Exiting shooting mode...";
            isShooting = false;
            RestoreMapView();
        }
    }

    /// <summary>Waits out the wind-up animation before accepting a shooting direction.</summary>
    private IEnumerator ActivateShootingAfterWindUp()
    {
        yield return new WaitForSeconds(ActionAnimationSeconds);
        isShooting = true;
    }

    /// <summary>
    /// Per-frame loop: keeps the sprite on the current room, restores the map after an encounter,
    /// refreshes the HUD, then dispatches input to either movement or shooting.
    /// </summary>
    private void Update()
    {
        transform.position = player.gameObject.transform.position;

        RestoreMapViewIfBatFlightEnded();
        UpdateHud();

        if (canMove)
        {
            background.SetActive(true);
            HandleMovementInput();
            HandleHazardsInCurrentRoom();
        }
        else if (isShooting)
        {
            HandleShootingInput();
        }

        CheckKeyboardTestModeShortcut();
    }

    /// <summary>
    /// Brings the map back once the bat animation has finished and no encounter scene is loaded.
    /// </summary>
    private void RestoreMapViewIfBatFlightEnded()
    {
        if (SceneManager.sceneCount == 1 && !isShooting && isBatEncounterActive)
        {
            RestoreMapView();
        }
    }

    /// <summary>Reads a swipe or key press and moves the player through the chosen tunnel.</summary>
    private void HandleMovementInput()
    {
        Cell destination = player;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                BeginTouchAndCheckTestMode(touch);
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                destination = player.neighbors[DirectionFromSwipe(touch.position)];
            }
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            isShooting = true;
            canMove = false;
        }
        else if (TryGetKeyboardDirection(out string direction))
        {
            destination = player.neighbors[direction];
        }

        if (destination != player)
        {
            MoveTo(destination);
        }
    }

    /// <summary>
    /// Records the start of a swipe and, on a triple-finger double tap, reveals hazards for testing.
    /// </summary>
    /// <param name="touch">The touch that just began.</param>
    private void BeginTouchAndCheckTestMode(Touch touch)
    {
        touchStartPosition = touch.position;

        float now = Time.time;
        if (now - touchStartTime < DoubleTapWindowSeconds && Input.touchCount >= TestModeFingerCount)
        {
            Debug.Log("Entering test mode");
            RevealHazardsForTesting();
        }
        touchStartTime = now;
    }

    /// <summary>
    /// Moves the player into an adjacent room, advances the turn counter and the coin payout,
    /// clears the per-room encounter guards, and requests a new ambient fact.
    /// </summary>
    /// <param name="destination">Room to move into.</param>
    private void MoveTo(Cell destination)
    {
        player.hasPlayer = false;
        destination.hasPlayer = true;
        player = destination;

        numTurns++;
        if (coinsRemaining > 0)
        {
            coins++;
        }
        coinsRemaining--;

        wumpusRoom = false;
        batRoom = false;
        pitRoom = false;

        StartCoroutine(RequestRandomFact());
    }

    /// <summary>Triggers the Wumpus, bat or pit encounter for the room the player just entered.</summary>
    private void HandleHazardsInCurrentRoom()
    {
        if (player.hasWumpus && !wumpusRoom)
        {
            EnterEncounterScene(WumpusSceneName);
            wumpusRoom = true;
        }

        if (player.hasBat && !batRoom)
        {
            batRoom = true;
            StartCoroutine(CarryPlayerWithBats());
        }

        if (player.hasPit && !pitRoom)
        {
            EnterEncounterScene(PitSceneName);
            pitRoom = true;
        }
    }

    /// <summary>W plus U on the keyboard is the desktop equivalent of the triple-finger double tap.</summary>
    private void CheckKeyboardTestModeShortcut()
    {
        if (Input.GetKeyDown(KeyCode.W) && Input.GetKey(KeyCode.U))
        {
            Debug.Log("Entering test mode");
            RevealHazardsForTesting();
        }
    }

    /// <summary>
    /// Hides the map and opens an encounter or shop scene additively on top of it.
    /// </summary>
    /// <param name="sceneName">Name of the scene to load.</param>
    private void EnterEncounterScene(string sceneName)
    {
        HideMapView();
        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
    }

    /// <summary>Hides the grid, player and HUD, and freezes input.</summary>
    private void HideMapView()
    {
        gameCanvas.gameObject.SetActive(false);
        cellGenerator.makeDisappear();
        playerSprite.enabled = false;
        background.SetActive(false);
        canMove = false;
    }

    /// <summary>Shows the grid, player and HUD again, and unfreezes input.</summary>
    private void RestoreMapView()
    {
        cellGenerator.makeAppear();
        playerSprite.enabled = true;
        gameCanvas.gameObject.SetActive(true);
        canMove = true;
    }

    /// <summary>
    /// Plays the bat animation, then drops the player in a random room and reshuffles the hazards.
    /// </summary>
    /// <returns>A coroutine to be driven with <c>StartCoroutine</c>.</returns>
    private IEnumerator CarryPlayerWithBats()
    {
        isBatEncounterActive = true;
        batPrefab.SetActive(true);
        canMove = false;

        yield return new WaitForSeconds(ActionAnimationSeconds);

        batPrefab.SetActive(false);
        isBatEncounterActive = false;
        canMove = true;

        player.hasPlayer = false;
        player = cells[rnd.Next(CellGenerator.GridWidth), rnd.Next(CellGenerator.GridHeight)];
        player.hasPlayer = true;

        cellGenerator.SetHazards();
    }

    /// <summary>Reads a swipe or key press and fires an arrow down the chosen line.</summary>
    private void HandleShootingInput()
    {
        Cell target = player;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                touchStartPosition = touch.position;
                touchStartTime = Time.time;
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                target = player.next[DirectionFromSwipe(touch.position)];
            }
        }
        else if (TryGetKeyboardDirection(out string direction))
        {
            target = player.next[direction];
        }

        if (target != player)
        {
            StartCoroutine(FireArrowAt(target));
        }
    }

    /// <summary>
    /// Flies an arrow into the target room. A hit ends the run as a win; a miss costs an arrow and
    /// may prompt the Wumpus to move.
    /// </summary>
    /// <param name="target">Room the arrow travels to.</param>
    /// <returns>A coroutine to be driven with <c>StartCoroutine</c>.</returns>
    private IEnumerator FireArrowAt(Cell target)
    {
        target.hasArrow = true;
        yield return new WaitForSeconds(ActionAnimationSeconds);

        if (target.hasWumpus)
        {
            wumpusDead = 1;
            score += WumpusKillBonus;
            EndRun(true, "Beat the wumpus!");
        }

        target.hasArrow = false;
        numArrows--;
        isShooting = false;
        RestoreMapView();

        if (rnd.Next(WumpusRoamChanceDenominator) == WumpusRoamTriggerValue)
        {
            cellGenerator.moveWumpusAdj();
        }
    }

    /// <summary>
    /// Maps the angle of the swipe that just ended onto one of the six hex directions.
    /// </summary>
    /// <param name="touchEndPosition">Screen position where the swipe finished.</param>
    /// <returns>The matching <see cref="Direction"/> constant.</returns>
    private string DirectionFromSwipe(Vector2 touchEndPosition)
    {
        float radians = Mathf.Atan2(
            touchEndPosition.y - touchStartPosition.y,
            touchEndPosition.x - touchStartPosition.x);
        float degrees = radians * 180 / Mathf.PI;

        if (degrees >= 0 && degrees <= 60)
        {
            return Direction.UpRight;
        }
        if (degrees > 60 && degrees < 120)
        {
            return Direction.Up;
        }
        if (degrees >= 120 && degrees < 180)
        {
            return Direction.UpLeft;
        }
        if (degrees >= -180 && degrees <= -120)
        {
            return Direction.DownLeft;
        }
        if (degrees > -120 && degrees < -60)
        {
            return Direction.Down;
        }
        return Direction.DownRight;
    }

    /// <summary>
    /// Reads the arrow keys, where holding Left or Right alongside Up or Down selects a diagonal.
    /// </summary>
    /// <param name="direction">Receives the chosen <see cref="Direction"/> constant.</param>
    /// <returns>True when a direction key was pressed this frame.</returns>
    private static bool TryGetKeyboardDirection(out string direction)
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (Input.GetKey(KeyCode.RightArrow))
            {
                direction = Direction.UpRight;
            }
            else if (Input.GetKey(KeyCode.LeftArrow))
            {
                direction = Direction.UpLeft;
            }
            else
            {
                direction = Direction.Up;
            }
            return true;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (Input.GetKey(KeyCode.RightArrow))
            {
                direction = Direction.DownRight;
            }
            else if (Input.GetKey(KeyCode.LeftArrow))
            {
                direction = Direction.DownLeft;
            }
            else
            {
                direction = Direction.Down;
            }
            return true;
        }

        direction = null;
        return false;
    }
}
