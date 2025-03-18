using System;
using TMPro;
using UnityEngine;

public class GameTimerManager : MonoBehaviour
{
    public static GameTimerManager Instance;
    
    [SerializeField] private bool timerEnabled;
    [SerializeField] private int increment;
    [Space]
    public float whiteTimer;
    public float blackTimer;
    [SerializeField] private bool isRunning;

    [Header("References")]
    [SerializeField] private TMP_Text whiteTimerText;
    [SerializeField] private TMP_Text blackTimerText;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        if (!whiteTimerText) whiteTimerText = GameObject.Find("Canvas/Timer/White Timer/Text").GetComponent<TMP_Text>();
        if (!blackTimerText) blackTimerText = GameObject.Find("Canvas/Timer/Black Timer/Text").GetComponent<TMP_Text>();
    }
    
    private void OnDestroy()
    {
        ChessManager.Instance.OnTurnEnd -= AddIncrement;
        
        RulesCanvas.Instance.onRulesOpened -= StopTimer;
        RulesCanvas.Instance.onRulesClosed -= StartTimer;
    }

    private void Start()
    {
        ChessManager.Instance.OnTurnEnd += AddIncrement;
        RulesCanvas.Instance.onRulesOpened += StopTimer;
        RulesCanvas.Instance.onRulesClosed += StartTimer;
        
        InitializeTimer();
    }

    void Update()
    {
        RunningTimer();
    }

    void InitializeTimer()
    {
        if (GameConfigurations.PlayerTimerMinute != 0)
        {
            timerEnabled = true;
            whiteTimer = GameConfigurations.PlayerTimerMinute * 60;
            blackTimer = GameConfigurations.PlayerTimerMinute * 60;
            increment = GameConfigurations.PlayerIncrementSecond;
            
            whiteTimerText.text = FormatTime(whiteTimer);
            blackTimerText.text = FormatTime(blackTimer);
        }
        else
        {
            timerEnabled = false;
            
            whiteTimerText.transform.parent.gameObject.SetActive(false);
            blackTimerText.transform.parent.gameObject.SetActive(false);
        }
    }

    void StartTimer()
    {
        StartTimer(ChessManager.Instance.CurrentTurn == ChessPieceTeam.White);
    }
    
    public void StartTimer(bool whiteTurn = true)
    {
        if (timerEnabled)
        {
            isRunning = true;
        }
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    void RunningTimer()
    {
        if (timerEnabled && isRunning)
        {
            if (ChessManager.Instance.CurrentTurn == ChessPieceTeam.White)
            {
                whiteTimer -= Time.deltaTime;
                whiteTimerText.text = FormatTime(whiteTimer);
                
                if (whiteTimer <= 0)
                {
                    whiteTimer = 0;
                    isRunning = false;
                    
                    EndGameManager.Instance.WinEndGame(EndGameManager.WinType.TimeOut, ChessPieceTeam.Black);
                }
            }
            else
            {
                blackTimer -= Time.deltaTime;
                blackTimerText.text = FormatTime(blackTimer);
                
                if (blackTimer <= 0)
                {
                    blackTimer = 0;
                    isRunning = false;
                    
                    EndGameManager.Instance.WinEndGame(EndGameManager.WinType.TimeOut, ChessPieceTeam.White);
                }
            }
        }
    }

    string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        minutes = Mathf.Max(0, minutes);
        seconds = Mathf.Max(0, seconds);
        return $"{minutes:D2}:{seconds:D2}";
    }
    
    public void AddIncrement()
    {
        if (ChessManager.Instance.CurrentTurn == ChessPieceTeam.White)
        {
            whiteTimer += increment;
            whiteTimerText.text = FormatTime(whiteTimer);
        }
        else
        {
            blackTimer += increment;
            blackTimerText.text = FormatTime(blackTimer);
        }
    }

    public void ChangeTimer(float newWhiteTimer, float newBlackTimer)
    {
        whiteTimer = newWhiteTimer;
        blackTimer = newBlackTimer;
    }
}
