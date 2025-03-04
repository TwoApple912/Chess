using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

public class MoveWhenHoverStartMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Vector3 positionOffset = new Vector3(0.037f, 0, 0);
    [SerializeField] private float scaleOffset = 1;
    [SerializeField] private float duration = 0.25f;

    private Vector3 position;
    private Vector3 hoveredPosition;
    private float scale;
    private float hoveredScale;
    
    private Coroutine currentPositionCoroutine;
    private Coroutine currentScaleCoroutine;

    private void Start()
    {
        position = transform.position;
        hoveredPosition = position + positionOffset;
        scale = transform.localScale.x;
        hoveredScale = scale * scaleOffset;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentPositionCoroutine != null) StopCoroutine(currentPositionCoroutine);
        if (currentScaleCoroutine != null) StopCoroutine(currentScaleCoroutine);
        
        currentPositionCoroutine = StartCoroutine(MoveButton(hoveredPosition));
        currentScaleCoroutine = StartCoroutine(ScaleButton(hoveredScale));
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (currentPositionCoroutine != null) StopCoroutine(currentPositionCoroutine);
        if (currentScaleCoroutine != null) StopCoroutine(currentScaleCoroutine);
        
        currentPositionCoroutine = StartCoroutine(MoveButton(position));
        currentScaleCoroutine = StartCoroutine(ScaleButton(scale));
    }

    IEnumerator MoveButton(Vector3 targetPosition)
    {
        Vector3 startPosition = transform.position;

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            t = 1f - Mathf.Pow(1f - t, 3f);
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPosition;

        currentPositionCoroutine = null;
    }

    IEnumerator ScaleButton(float targetScale)
    {
        float startScale = transform.localScale.x;

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            t = 1f - Mathf.Pow(1f - t, 3f);
            float newScale = Mathf.Lerp(startScale, targetScale, t);
            transform.localScale = new Vector3(newScale, newScale, newScale);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.localScale = new Vector3(targetScale, targetScale, targetScale);

        currentScaleCoroutine = null;
    }
}