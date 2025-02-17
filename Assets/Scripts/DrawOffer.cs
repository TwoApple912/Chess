using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawOffer : MonoBehaviour
{
    [SerializeField] private bool? acceptOffer;
    [Space]
    [SerializeField] private List<Animator> animators;
    [SerializeField] private Animator acceptButtonAnimator;
    [SerializeField] private Animator denyButtonAnimator;

    private void Awake()
    {
        animators = new List<Animator>(transform.GetComponentsInChildren<Animator>());
        acceptButtonAnimator = transform.Find("Accept Button").GetComponent<Animator>();
        denyButtonAnimator = transform.Find("Deny Button").GetComponent<Animator>();
    }

    private void Start()
    {
        gameObject.SetActive(false);
    }

    public void AcceptOffer()
    {
        ChessManager.Instance.DrawOfferAccepted();
        
        acceptButtonAnimator.SetTrigger("Pressed");
        StartCoroutine(TurnOffDrawOfferWindow());
    }

    public void DenyOffer()
    {
        ChessManager.Instance.DrawOfferRejected();
        
        denyButtonAnimator.SetTrigger("Pressed");
        StartCoroutine(TurnOffDrawOfferWindow());
    }
    
    IEnumerator TurnOffDrawOfferWindow()
    {
        yield return new WaitForSeconds(0.5f);
        
        foreach (var animator in animators)
        {
            animator.SetTrigger("closeWindow");
        }

        yield return new WaitForSeconds(2);
        gameObject.SetActive(false);
    }
}