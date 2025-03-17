using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class DropdownManager : MonoBehaviour
{
    public static DropdownManager Instance;

    private TMP_Dropdown activeDropdown;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (activeDropdown != null && Input.GetMouseButtonDown(0))
        {
            if (!IsPointerOverUIElement())
            {
                CloseActiveDropdown();
            }
        }
    }
    
    public void SetActiveDropdown(TMP_Dropdown dropdown)
    {
        if (activeDropdown != null && activeDropdown != dropdown)
        {
            activeDropdown.Hide();
        }
        
        activeDropdown = dropdown;
    }

    void CloseActiveDropdown()
    {
        if (activeDropdown != null)
        {
            activeDropdown.Hide();
            activeDropdown = null;
        }
    }

    private bool IsPointerOverUIElement()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        
        return results.Count > 0;
    }
}