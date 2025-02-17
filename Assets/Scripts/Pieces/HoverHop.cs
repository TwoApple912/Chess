using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoverHop : MonoBehaviour
{
    [SerializeField] private float hopHeight = 0.12f;
    [SerializeField] private float hopDuration = 0.1f;

    private Vector3 originalLocalPosition;
    private Coroutine hoverCoroutine;
    private Transform modelTransform;

    private void Start()
    {
        modelTransform = transform.Find("Model");
        if (modelTransform == null)
        {
            Debug.LogError("Model child object not found.");
            return;
        }
        originalLocalPosition = modelTransform.localPosition;
    }

    private void OnMouseEnter()
    {
        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
        }
        hoverCoroutine = StartCoroutine(HoverUp());
    }

    private void OnMouseExit()
    {
        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
        }
        hoverCoroutine = StartCoroutine(HoverDown());
    }

    private IEnumerator HoverUp()
    {
        Vector3 targetLocalPosition = originalLocalPosition + Vector3.up * hopHeight;
        float elapsedTime = 0f;

        while (elapsedTime < hopDuration)
        {
            modelTransform.localPosition = Vector3.Lerp(originalLocalPosition, targetLocalPosition, elapsedTime / hopDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        modelTransform.localPosition = targetLocalPosition;
    }

    private IEnumerator HoverDown()
    {
        Vector3 startLocalPosition = modelTransform.localPosition;
        float elapsedTime = 0f;

        while (elapsedTime < hopDuration)
        {
            modelTransform.localPosition = Vector3.Lerp(startLocalPosition, originalLocalPosition, elapsedTime / hopDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        modelTransform.localPosition = originalLocalPosition;
    }
}