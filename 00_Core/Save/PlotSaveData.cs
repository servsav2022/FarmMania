using System;
using UnityEngine;

/// <summary>
/// DTO для сохранения одной грядки
/// </summary>
[Serializable]
public class PlotSaveData
{
    public int x;
    public int y;
    public FarmGridController.PlotState state;
    public string seedItemId;
    public float timer;
    public int growthStage; // ← ДОБАВИТЬ
}
