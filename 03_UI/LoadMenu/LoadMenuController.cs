using System;
using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class LoadMenuController : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown slotDropdown;
    [SerializeField] private UnityEngine.UI.Button loadButton;
    [SerializeField] private UnityEngine.UI.Button deleteButton;


    private const int SlotCount = 3;
    public void RefreshDropdown()
    {
        slotDropdown.ClearOptions();

        var options = new List<string>();

        for (int i = 1; i <= SlotCount; i++)
        {
            options.Add(BuildSlotLabel(i));
        }

        slotDropdown.AddOptions(options);
        slotDropdown.value = 0;
        slotDropdown.RefreshShownValue();
    }

    private string BuildSlotLabel(int slotIndex)
    {
        var um = UserManager.Instance;
        if (um == null || um.CurrentUser == null)
            return $"Слот {slotIndex} // пусто";

        string saveId = um.GetManualSlotId(slotIndex);
        var data = SaveManager.LoadGame(saveId);

        if (data == null)
            return $"Слот {slotIndex} // пусто";

        DateTime t = data.savedAt;
        return $"Слот {slotIndex} // {t:dd.MM.yyyy} // {t:HH:mm}";
    }
    private int GetSelectedSlotIndex()
    {
        return slotDropdown.value + 1;
    }
    public void OnLoadClicked()
    {
        int slot = GetSelectedSlotIndex();

        bool loaded = UserManager.Instance.LoadManualSlot(slot);
        if (!loaded)
        {
            Debug.Log($"[LoadMenu] Slot {slot} is empty");
            return;
        }

        // Переходим в игру
        GameManager.Instance.StartGame();
    }
    
    public void OnDeleteClicked()
    {
        int slot = GetSelectedSlotIndex();

        UserManager.Instance.DeleteManualSlot(slot);

        RefreshDropdown();
    }
    private void UpdateButtonsState()
    {
        int slot = GetSelectedSlotIndex();

        string saveId = UserManager.Instance.GetManualSlotId(slot);
        bool exists = SaveManager.LoadGame(saveId) != null;

        loadButton.interactable = exists;
        deleteButton.interactable = exists;
        UserManager.Instance.SetSelectedManualSlot(GetSelectedSlotIndex());

    }
    
    private void Awake()
    {
        slotDropdown.onValueChanged.AddListener(_ => UpdateButtonsState());
    }

    private void OnEnable()
    {
        RefreshDropdown();
        UpdateButtonsState();
    }

}