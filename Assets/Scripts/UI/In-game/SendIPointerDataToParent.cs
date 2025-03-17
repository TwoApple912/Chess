using UnityEngine;
using UnityEngine.EventSystems;

public class SendIPointerDataToParent : MonoBehaviour, IPointerEnterHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.parent.SendMessage("OnPointerEnter", eventData);
    }
}