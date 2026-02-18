using UnityEngine;

public class SaveButtons : MonoBehaviour
{
    public void Save()
    {
        var um = UserManager.Instance;
        if (um == null) return;

        um.SaveToManualSlot(um.SelectedManualSlot);
    }

    public void Load()
    {
        var um = UserManager.Instance;
        if (um == null) return;

        if (!um.HasAnyManualSave())
        {
            Debug.Log("[Load] No manual saves available");
            return;
        }
        um.LoadLatestManualSave();
    }

    public void Delete()
    {
        var saveId = UserManager.Instance?.CurrentUser?.LastSaveFile;
        if (!string.IsNullOrEmpty(saveId))
            SaveManager.DeleteSave(saveId);
    }
}