using System;
using System.Collections;
using UnityEngine;

public class PromotionBehavior : MonoBehaviour
{
    private Light globalLight;
    
    private Light spotLight;
    private Transform[] pieces = new Transform[4];
    
    [Header("Spawning Pieces")]
    [SerializeField] private float initialDelay = 0.25f;
    [Space]
    [SerializeField] private float lightUpDuration = 0.25f;
    [SerializeField] private float spotLightIntensity = 100f;
    [Space]
    // [SerializeField] private float pieceSpawnHeight = 0.25f;
    // [SerializeField] private float pieceSpawnDuration = 0.15f;
    [SerializeField] private float pieceSpawnDelay = 0.1f;
    [Space]
    [SerializeField] private int layerMask = 1 << 5;
    
    [Header("Reconfirmation Message")]
    [SerializeField] private GameObject reconfirmationMessage;

    private float initialGlobalLightIntensity;
    private Transform selectedPiece;
    
    void Awake()
    {
        globalLight = GameObject.Find("Directional Light").GetComponent<Light>();
        
        spotLight = transform.Find("Spot Light").GetComponent<Light>();
        pieces = new Transform[]
            { transform.Find("Bishop"), transform.Find("Knight"), transform.Find("Rook"), transform.Find("Queen") };
        
        reconfirmationMessage = transform.Find("Canvas/Reconfirmation Message").gameObject;
    }

    private void OnEnable()
    {
        foreach (Transform piece in pieces) piece.gameObject.SetActive(false);
        spotLight.intensity = 0;
        initialGlobalLightIntensity = globalLight.intensity;
        reconfirmationMessage.SetActive(false);
    }

    public IEnumerator InitiatePromotionSequence(ChessPieceTeam team, Action<Transform> onPieceSelected)
    {
        foreach (var piece in pieces)
        {
            ChessPiece pieceScript = piece.GetComponent<ChessPiece>();
            pieceScript.chessPieceTeam = team;
        }
        
        yield return StartCoroutine(PromotionCoroutine(onPieceSelected));
    }

    public IEnumerator ExitPromotionSequence()
    {
        StartCoroutine(AdjustLightIntensity(spotLight, 0, lightUpDuration));
        yield return new WaitForSeconds(lightUpDuration);
        StartCoroutine(AdjustLightIntensity(globalLight, initialGlobalLightIntensity, 1.25f));
        yield return new WaitForSeconds(1.25f);
    }

    public void SetInactiveCamera()
    {
        transform.Find("Promotion Camera").gameObject.SetActive(false);
    }

    IEnumerator PromotionCoroutine(Action<Transform> onPieceSelected)
    {
        StartCoroutine(AdjustLightIntensity(globalLight, 0, 1.25f));
        yield return new WaitForSeconds(1.25f);
        StartCoroutine(AdjustLightIntensity(spotLight, spotLightIntensity, lightUpDuration));
        yield return new WaitForSeconds(lightUpDuration * 3/4);
        StartCoroutine(SpawnPieces());
        
        yield return StartCoroutine(HandlePieceSelection(onPieceSelected));
    }

    IEnumerator AdjustLightIntensity(Light light, float targetIntensity, float duration)
    {
        float elapsedTime = 0f;
        float initialIntensity = light.intensity;

        while (elapsedTime < duration)
        {
            light.intensity = Mathf.Lerp(initialIntensity, targetIntensity, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        light.intensity = targetIntensity;
    }

    IEnumerator SpawnPieces()
    {
        foreach (Transform piece in pieces)
        {
            piece.gameObject.SetActive(true);
            
            yield return new WaitForSeconds(pieceSpawnDelay);
        }
    }

    IEnumerator HandlePieceSelection(Action<Transform> onPieceSelected)
    {
        bool selectionConfirmed = false;
        while (!selectionConfirmed)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, layerMask))
                {
                    Transform hitTransform = hit.transform;
                    if (Array.Exists(pieces, piece => piece == hitTransform))
                    {
                        if (selectedPiece == hitTransform)
                        {
                            // Second press
                            selectionConfirmed = true;
                            onPieceSelected?.Invoke(selectedPiece);
                            
                            reconfirmationMessage.SetActive(false);
                        }
                        else
                        {
                            // First press
                            selectedPiece = hitTransform;
                            
                            reconfirmationMessage.SetActive(false);
                            reconfirmationMessage.SetActive(true);
                            var vector3 = reconfirmationMessage.transform.parent.position;
                            vector3.x = selectedPiece.position.x;
                            reconfirmationMessage.transform.parent.position = vector3;
                        }
                    }
                    else
                    {
                        // Reset selection
                        selectedPiece = null;
                        
                        reconfirmationMessage.SetActive(false);
                    }
                }
            }
            yield return null;
        }
    }
}
