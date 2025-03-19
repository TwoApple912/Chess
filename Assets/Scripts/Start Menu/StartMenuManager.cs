using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class StartMenuManager : MonoBehaviour
{
    private enum menuMenus { Start, NewGame, Rules }
    [SerializeField] private menuMenus currentMenu = 0;
    [Space]
    [SerializeField] private string gameSceneName = "Game";
    
    [Header("New Game Parameters")]
    [SerializeField] private TMP_Dropdown timeDropdown;
    [SerializeField] private TMP_Dropdown incrementDropdown;
    [Space]
    [SerializeField] private bool customTimerInputFieldEnabled;
    [SerializeField] private bool customIncrementInputFieldEnabled;
    [SerializeField] private TMP_InputField customTimerInputField;
    [SerializeField] private TMP_InputField customIncrementInputField;

    [Header("Piece Spawning")]
    [SerializeField] private float initialDelay = 3f;
    [SerializeField] private Vector3 pawnPosition = new Vector3(-1.5f, 0, 1.5f);
    [Space]
    [SerializeField] private Vector3 carrierOffset = new Vector3(-0.2f, 0f, 0.15f);
    [SerializeField] private Vector3 adjunctOffset = new Vector3(0.25f, 0f, -0.2f);
    [SerializeField] private float adjunctSize = 0.85f;
    [SerializeField] private GameObject[] chessPiecesPrefab;
    
    private ChessPiece pawn;
    
    [Header("References")]
    [SerializeField] private CinemachineVirtualCamera startMenuCamera;
    [SerializeField] private CinemachineVirtualCamera newGameCamera;
    [SerializeField] private CinemachineVirtualCamera gameSceneCamera;
    [Space]
    [SerializeField] private CanvasGroup startMenuCanvasGroup;
    [SerializeField] private CanvasGroup newGameCanvasGroup;
    [SerializeField] private Button quitGameButton;
    [SerializeField] private Toggle chess960Toggle;
    [Space]
    [SerializeField] private List<GameObject> chessPieces;
    
    private void Awake()
    {
        if (!timeDropdown) timeDropdown = GameObject.Find("New Game Canvas/Time Configuration/Time Dropdown").GetComponent<TMP_Dropdown>();
        if (!incrementDropdown) incrementDropdown = GameObject.Find("New Game Canvas/Time Configuration/Increment Dropdown").GetComponent<TMP_Dropdown>();

        if (!customTimerInputField)
            customIncrementInputField = GameObject.Find("New Game Canvas/Time Configuration/Custom Timer Input Field").GetComponent<TMP_InputField>();
        if (!customIncrementInputField)
            customIncrementInputField = GameObject.Find("New Game Canvas/Time Configuration/Custom Increment Input Field").GetComponent<TMP_InputField>();
        
        chessPiecesPrefab = new GameObject[4]
        {
            Resources.Load<GameObject>("Prefabs/Chess Pieces/Bishop"),
            Resources.Load<GameObject>("Prefabs/Chess Pieces/Knight"),
            Resources.Load<GameObject>("Prefabs/Chess Pieces/Rook"),
            Resources.Load<GameObject>("Prefabs/Chess Pieces/Queen")
        };
        
        if (!pawn) pawn = GameObject.Find("Pawn").GetComponent<ChessPiece>();
        
        if (!startMenuCamera) startMenuCamera = GameObject.Find("Start Menu Camera").GetComponent<CinemachineVirtualCamera>();
        if (!newGameCamera) newGameCamera = GameObject.Find("New Game Camera").GetComponent<CinemachineVirtualCamera>();
        if (!gameSceneCamera) gameSceneCamera = GameObject.Find("Game Scene Camera").GetComponent<CinemachineVirtualCamera>();
        
        if (!startMenuCanvasGroup) startMenuCanvasGroup = GameObject.Find("Start Menu Canvas").GetComponent<CanvasGroup>();
        if (!newGameCanvasGroup) newGameCanvasGroup = GameObject.Find("New Game Canvas").GetComponent<CanvasGroup>();
        if (!quitGameButton) quitGameButton = GameObject.Find("Start Menu Canvas/Quit Button").GetComponent<Button>();
        if (!chess960Toggle) chess960Toggle = GameObject.Find("New Game Canvas/Chess960 Toggle").GetComponent<Toggle>();
        
        chessPieces.Add(pawn.gameObject);
    }

    private void OnDestroy()
    {
        RulesCanvas.Instance.onRulesClosed -= NGBack;
    }

    void Start()
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer) quitGameButton.interactable = false;
        
        RulesCanvas.Instance.onRulesClosed += NGBack;
        
        customTimerInputField.gameObject.SetActive(false);
        customIncrementInputField.gameObject.SetActive(false);
        
        SetGameConfigurations();
        
        StartCoroutine(PieceSequence());
    }

    void Update()
    {
        DropdownAutoCollapse();
    }
    
    void ChangeMenuState(menuMenus menu)
    {
        switch (menu)
        {
            case menuMenus.Start:
                currentMenu = menuMenus.Start;
                
                startMenuCanvasGroup.interactable = true;
                newGameCanvasGroup.interactable = false; newGameCamera.Priority = -1;
                // TODO: Rules
                break;
            case menuMenus.NewGame:
                currentMenu = menuMenus.NewGame;
                
                startMenuCanvasGroup.interactable = false;
                newGameCanvasGroup.interactable = true; newGameCamera.Priority = 1;
                // TODO: Rules
                break;
            case menuMenus.Rules:
                currentMenu = menuMenus.Rules;
                
                startMenuCanvasGroup.interactable = false;
                newGameCanvasGroup.interactable = false; newGameCamera.Priority = -1;
                // TODO: Rules
                break;
        }
    }

    #region Start Menu Buttons
    
    public void NewGame()
    {
        ChangeMenuState(menuMenus.NewGame);
    }

    public void OpenRules()
    {
        ChangeMenuState(menuMenus.Rules);
        
        RulesCanvas.Instance.ShowRules();
    }
    
    public void QuitGame()
    {
        MenuManager.QuitGame();
    }
    
    #endregion
    
    #region New Game Buttons

    public void NGClassicMode()
    {
        GameConfigurations.isWildMode = false;
        StartCoroutine(LoadGameScene());
    }

    public void NGWildMode()
    {
        GameConfigurations.isWildMode = true;
        StartCoroutine(LoadGameScene(true));
    }

    public void NGBack()
    {
        ChangeMenuState(menuMenus.Start);
        Debug.Log("Nigga " + currentMenu);
    }
    
    public void NGOnTimeDropDownValueChanged()
    {
        switch (timeDropdown.value)
        {
            case 0:
                GameConfigurations.PlayerTimerMinute = 0;
                EnableIncrementChange(false);
                DisableCustomTimerInputField();
                break;
            case 1:
                GameConfigurations.PlayerTimerMinute = 3;
                EnableIncrementChange(true);
                DisableCustomTimerInputField();
                break;
            case 2:
                GameConfigurations.PlayerTimerMinute = 10;
                EnableIncrementChange(true);
                DisableCustomTimerInputField();
                break;
            case 3:
                GameConfigurations.PlayerTimerMinute = int.Parse(customTimerInputField.text);
                EnableCustomTimerInputField();
                EnableIncrementChange(true);
                break;
        }
    }

    public void NGOnIncrementDropDownValueChanged()
    {
        switch (incrementDropdown.value)
        {
            case 0:
                GameConfigurations.PlayerIncrementSecond = 0;
                DisableCustomIncrementInputField();
                break;
            case 1:
                GameConfigurations.PlayerIncrementSecond = 2;
                DisableCustomIncrementInputField();
                break;
            case 2:
                GameConfigurations.PlayerIncrementSecond = 5;
                DisableCustomIncrementInputField();
                break;
            case 3:
                GameConfigurations.PlayerIncrementSecond = int.Parse(customIncrementInputField.text);
                EnableCustomIncrementInputField();
                break;
        }
    }

    void EnableIncrementChange(bool enable)
    {
        if (enable)
        {
            incrementDropdown.interactable = true;
        }
        else
        {
            incrementDropdown.value = 0;
            NGOnIncrementDropDownValueChanged();
            incrementDropdown.interactable = false;
        }
    }
    
    #region Custom Input for Timer and Increment

    void EnableCustomTimerInputField()
    {
        if (customTimerInputFieldEnabled) return;
        
        customTimerInputFieldEnabled = true;
        customTimerInputField.gameObject.SetActive(true);
    }

    void DisableCustomTimerInputField()
    {
        if (!customTimerInputFieldEnabled) return;
        
        customTimerInputFieldEnabled = false;
        customTimerInputField.gameObject.SetActive(false);
    }

    public void ApplyCustomTimerValue()
    {
        int value = int.Parse(customTimerInputField.text);

        GameConfigurations.PlayerTimerMinute = value;
        
        switch (value)
        {
            case 0:
                timeDropdown.value = 0;
                NGOnTimeDropDownValueChanged();
                DisableCustomTimerInputField();
                break;
            case 3:
                timeDropdown.value = 1;
                NGOnTimeDropDownValueChanged();
                DisableCustomTimerInputField();
                break;
            case 10:
                timeDropdown.value = 2;
                NGOnTimeDropDownValueChanged();
                DisableCustomTimerInputField();
                break;
        }
    }
    
    void EnableCustomIncrementInputField()
    {
        if (customIncrementInputFieldEnabled) return;
        
        customIncrementInputFieldEnabled = true;
        customIncrementInputField.gameObject.SetActive(true);
    }

    void DisableCustomIncrementInputField()
    {
        if (!customIncrementInputFieldEnabled) return;
        
        customIncrementInputFieldEnabled = false;
        customIncrementInputField.gameObject.SetActive(false);
    }

    public void ApplyCustomIncrementValue()
    {
        int value = int.Parse(customIncrementInputField.text);

        GameConfigurations.PlayerIncrementSecond = value;
        
        switch (value)
        {
            case 0:
                incrementDropdown.value = 0;
                NGOnIncrementDropDownValueChanged();
                DisableCustomIncrementInputField();
                break;
            case 2:
                incrementDropdown.value = 1;
                NGOnIncrementDropDownValueChanged();
                DisableCustomIncrementInputField();
                break;
            case 5:
                incrementDropdown.value = 2;
                NGOnIncrementDropDownValueChanged();
                DisableCustomIncrementInputField();
                break;
        }
    }
    
    #endregion
    
    void DropdownAutoCollapse()
    {
        if (Input.GetMouseButtonDown(0))
            if (timeDropdown.IsExpanded && !EventSystem.current.IsPointerOverGameObject())
                timeDropdown.Hide();
    }
    
    public void OnChess960ToggleValueChanged()
    {
        GameConfigurations.isChess960 = chess960Toggle.isOn;
    }

    #endregion

    void SetGameConfigurations()
    {
        switch (GameConfigurations.PlayerTimerMinute)
        {
            case 0:
                timeDropdown.value = 0;
                break;
            case 3:
                timeDropdown.value = 1;
                break;
            case 10:
                timeDropdown.value = 2;
                break;
            default:
                timeDropdown.value = 3;
                customTimerInputField.text = GameConfigurations.PlayerTimerMinute.ToString();
                break;
        }
        
        EnableIncrementChange(true);
        switch (GameConfigurations.PlayerIncrementSecond)
        {
            case 0:
                incrementDropdown.value = 0;
                EnableIncrementChange(false);
                break;
            case 2:
                incrementDropdown.value = 1;
                break;
            case 5:
                incrementDropdown.value = 2;
                break;
            default:
                incrementDropdown.value = 3;
                break;
        }
    }
    
    IEnumerator LoadGameScene(bool isWildMode = false)
    {
        newGameCanvasGroup.interactable = false;
        gameSceneCamera.Priority = 2;
        
        StartCoroutine(FadeOutCanvasGroup(startMenuCanvasGroup, 1f));
        StartCoroutine(FadeOutCanvasGroup(newGameCanvasGroup, 1f));
        DisableChessPieces();

        yield return new WaitForSeconds(2.01f);
        SceneManager.LoadScene(gameSceneName);
    }

    IEnumerator FadeOutCanvasGroup(CanvasGroup canvasGroup, float duration)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / duration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }
    
    void DisableChessPieces()
    {
        foreach (GameObject piece in chessPieces) piece.SetActive(false);
    }
    
    #region Opening Piece Movement

    IEnumerator PieceSequence()
    {
        int randomChessPieceIndex = Random.Range(0, chessPiecesPrefab.Length);
        ChessPiece cp = Instantiate(chessPiecesPrefab[randomChessPieceIndex]).GetComponent<ChessPiece>();
        cp.transform.position = GetPieceLocationBasedOnPiece(randomChessPieceIndex);
        chessPieces.Add(cp.gameObject);

        yield return new WaitForSeconds(initialDelay);
        
        cp.AnimatedMovePieceTo(pawnPosition + carrierOffset, 1.75f);
        pawn.AnimatedMovePieceTo(pawnPosition + adjunctOffset, 1.5f);
    }

    Vector3 GetPieceLocationBasedOnPiece(int pieceIndex)
    {
        switch (pieceIndex)
        {
            case 0:
                return new Vector3(0.5f, 0f, -0.5f);
            case 1:
                int randomIndex1 = Random.Range(0, 1);
                return (randomIndex1 == 0) ? new Vector3(0.5f, 0f, 0.5f) : new Vector3(-0.5f, 0f, -0.5f);
            case 2:
                int randomIndex2 = Random.Range(0, 1);
                return (randomIndex2 == 0) ? new Vector3(0.5f, 0f, 1.5f) : new Vector3(-1.5f, 0f, -0.5f);
            case 3:
                int randomIndex3 = Random.Range(0, 2);
                return (randomIndex3 == 0) ? new Vector3(0.5f, 0f, -0.5f) :
                    (randomIndex3 == 1) ? new Vector3(0.5f, 0f, 1.5f) : new Vector3(-1.5f, 0f, -0.5f);
        }

        return new Vector3(0.5f, 0f, -0.5f);
    }
    
    #endregion
}