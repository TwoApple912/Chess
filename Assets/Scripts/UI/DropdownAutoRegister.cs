using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class DropdownAutoRegister : MonoBehaviour, IPointerClickHandler
{
    private TMP_Dropdown dropdown;

    private void Start()
    {
        dropdown = GetComponent<TMP_Dropdown>();
        
        dropdown.onValueChanged.AddListener(delegate { RegisterDropDown(); });
    }

    void RegisterDropDown()
    {
        DropdownManager.Instance.SetActiveDropdown(dropdown);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        RegisterDropDown();
    }
}
