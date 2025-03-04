using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonAnimation : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Animator animator;
    [SerializeField] private string triggerName = "Pressed";

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        PlayAnimation();
    }

    void PlayAnimation()
    {
        animator.SetTrigger(triggerName);
    }
}
