using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class FarmGridController : MonoBehaviour
{
    public enum PlotState { Empty, Plowed, Planted, Ready }

    [Header("Scene refs")]
    [SerializeField] private Transform player;
    [SerializeField] private Tilemap groundTilemap;

    // Земля (вскопанная клетка)
    [SerializeField] private Tilemap farmingTilemap;

    // Растения (визуал)
    [SerializeField] private Tilemap cropTilemap;

    // Блокировка движения по созревшим растениям (коллайдеры)
    [SerializeField] private Tilemap cropBlockTilemap;
    [SerializeField] private TileBase cropBlockTile;

    // Запрет копания по препятствиям
    [SerializeField] private Tilemap obstaclesTilemap;
    [SerializeField] private Tilemap buildingsTilemap;

    [Header("Databases")]
    [SerializeField] private ItemDatabase itemDatabase;

    [Header("Planting")]
    [SerializeField] private int seedCost = 1;

    [Header("Growth")]
    [Tooltip("Время на одну стадию роста, если в CropData не задано или задано некорректно.")]
    [SerializeField] private float defaultSecondsPerStage = 5f;

    [Header("Revert plowed to grass")]
    [Tooltip("Через сколько секунд вскопанная клетка должна зарастать травой обратно.")]
    [SerializeField] private float plowedRevertSeconds = 60f;

    [Header("Harvest")]
    [SerializeField] private bool keepPlowedAfterHarvest = true;
    [SerializeField] private int defaultHarvestAmount = 1;

    [Header("Tiles")]
    [SerializeField] private TileBase plowedTile;
    [SerializeField] private TileBase sproutTile;

    [Header("Rules")]
    [SerializeField] private int interactRadiusCells = 1;
    [SerializeField] private bool ignoreClicksOverUI = true;

    [Header("Blocking")]
    [SerializeField] private LayerMask blockingLayers;

    private class Plot
    {
        public PlotState state;

        // таймер роста для состояния Посажено
        public float growthTimer;

        // таймер зарастания для состояния Вскопано
        public float plowedTimer;

        public string seedItemId;
        public CropData crop;
        public int growthStage;
    }

    private readonly Dictionary<Vector3Int, Plot> plots = new();

    private void Start()
    {
        if (UserManager.Instance != null &&
            UserManager.Instance.CurrentSave != null &&
            UserManager.Instance.CurrentSave.plots != null)
        {
            ApplyPlotSaveData(UserManager.Instance.CurrentSave.plots);
            Debug.Log("[FarmGrid] Applied plots from already loaded save");
        }
    }

    private void Update()
    {
        HandleClick();
        UpdateGrowth();
        UpdatePlowedRevert();
    }

    // ================= ВВОД =================

    private void HandleClick()
    {
        if (!TryGetPointer(out Vector2 screenPos, out int pointerId))
            return;

        if (ignoreClicksOverUI && IsPointerOverUI(pointerId))
            return;

        var cam = Camera.main;
        if (cam == null) return;

        Vector3 world = cam.ScreenToWorldPoint(screenPos);
        Vector3Int cell = groundTilemap.WorldToCell(world);

        if (!groundTilemap.HasTile(cell) || !IsNearPlayer(cell))
            return;

        // Запрет взаимодействия с клеткой, если на ней размещено препятствие или здание
        if ((obstaclesTilemap != null && obstaclesTilemap.HasTile(cell)) ||
            (buildingsTilemap != null && buildingsTilemap.HasTile(cell)))
        {
            return;
        }

        if (IsBlockedByObject(cell))
            return;

        Plot plot = GetOrCreatePlot(cell);

        switch (plot.state)
        {
            case PlotState.Empty:
                SetPlot(cell, plot, PlotState.Plowed, null);
                break;

            case PlotState.Plowed:
                TryPlant(cell, plot);
                break;

            case PlotState.Ready:
                Harvest(cell, plot);
                break;
        }
    }

    // ================= ПОСАДКА =================

    private void TryPlant(Vector3Int cell, Plot plot)
    {
        // Запрет посадки растения в клетку, на которой стоит игрок.
        // Это предотвращает ситуацию, когда игрок блокирует сам себя или визуально «сажает под ноги».
        if (player != null && groundTilemap != null)
        {
            Vector3Int playerCell = groundTilemap.WorldToCell(player.position);
            if (cell == playerCell)
                return;
        }

        string selectedSeedId = UserManager.Instance.SelectedSeedItemId;

        if (string.IsNullOrEmpty(selectedSeedId))
        {
            UserManager.Instance.ClearSelectedSeed();
            return;
        }

        if (itemDatabase.Get(selectedSeedId) is not SeedData seed)
            return;

        if (seed.cropToPlant == null)
            return;

        if (!UserManager.Instance.TryUseItemFromCurrentUser(seed.itemId, seedCost))
        {
            UserManager.Instance.ClearSelectedSeed();
            return;
        }

        plot.seedItemId = seed.itemId;
        plot.crop = seed.cropToPlant;

        plot.growthTimer = 0f;
        plot.growthStage = 0;
        plot.plowedTimer = 0f;

        SetPlot(cell, plot, PlotState.Planted, sproutTile);

        if (AudioManager.I != null)
            AudioManager.I.PlayPlant();

        if (QuestManagerMono.Instance != null)
            QuestManagerMono.Instance.ReportPlantedSeed();
    }

    // ================= РОСТ =================

    private void UpdateGrowth()
    {
        foreach (var kv in plots)
        {
            var cell = kv.Key;
            var plot = kv.Value;

            if (plot.state != PlotState.Planted || plot.crop == null)
                continue;

            float secondsPerStage = plot.crop.secondsPerStage > 0f ? plot.crop.secondsPerStage : defaultSecondsPerStage;

            plot.growthTimer += Time.deltaTime;
            if (plot.growthTimer < secondsPerStage)
                continue;

            plot.growthTimer = 0f;
            plot.growthStage++;

            if (plot.crop.growthStages == null || plot.crop.growthStages.Length == 0)
                continue;

            int maxStage = plot.crop.growthStages.Length - 1;

            if (plot.growthStage >= maxStage)
            {
                plot.growthStage = maxStage;
                SetPlot(cell, plot, PlotState.Ready, plot.crop.growthStages[maxStage]);
            }
            else
            {
                cropTilemap.SetTile(cell, plot.crop.growthStages[plot.growthStage]);
            }
        }
    }

    // ================= ЗАРАСТАНИЕ ВСКОПАННОЙ ЗЕМЛИ =================

    private void UpdatePlowedRevert()
    {
        if (plowedRevertSeconds <= 0f)
            return;

        foreach (var kv in plots)
        {
            var cell = kv.Key;
            var plot = kv.Value;

            if (plot.state != PlotState.Plowed)
                continue;

            plot.plowedTimer += Time.deltaTime;

            if (plot.plowedTimer >= plowedRevertSeconds)
            {
                plot.plowedTimer = 0f;
                SetPlot(cell, plot, PlotState.Empty, null);
            }
        }
    }

    // ================= СБОР УРОЖАЯ =================

    private void Harvest(Vector3Int cell, Plot plot)
    {
        if (plot.crop != null && plot.crop.harvestProduct != null)
        {
            UserManager.Instance.AddItemToCurrentUser(
                plot.crop.harvestProduct.itemId,
                plot.crop.harvestAmount > 0 ? plot.crop.harvestAmount : defaultHarvestAmount
            );
        }
        // Сообщаем в систему квестов о сборе урожая
        if (QuestManagerMono.Instance != null && plot.crop != null && plot.crop.harvestProduct != null)
        {
            string itemId = plot.crop.harvestProduct.itemId;
            int amount = plot.crop.harvestAmount > 0 ? plot.crop.harvestAmount : defaultHarvestAmount;
            QuestManagerMono.Instance.ReportTag($"harvest:{itemId}", amount);
        }

        if (AudioManager.I != null)
            AudioManager.I.PlayHarvest();

        plot.seedItemId = null;
        plot.crop = null;
        plot.growthTimer = 0f;
        plot.growthStage = 0;
        plot.plowedTimer = 0f;

        if (keepPlowedAfterHarvest)
            SetPlot(cell, plot, PlotState.Plowed, null);
        else
            SetPlot(cell, plot, PlotState.Empty, null);
    }

    // ================= СОХРАНЕНИЕ =================

    public List<PlotSaveData> BuildPlotSaveData()
    {
        var list = new List<PlotSaveData>();
        foreach (var kv in plots)
        {
            list.Add(new PlotSaveData
            {
                x = kv.Key.x,
                y = kv.Key.y,
                state = kv.Value.state,
                seedItemId = kv.Value.seedItemId,
                timer = kv.Value.growthTimer,
                growthStage = kv.Value.growthStage
            });
        }
        return list;
    }

    public void ApplyPlotSaveData(List<PlotSaveData> data)
    {
        plots.Clear();

        farmingTilemap.ClearAllTiles();
        if (cropTilemap != null) cropTilemap.ClearAllTiles();
        if (cropBlockTilemap != null) cropBlockTilemap.ClearAllTiles();

        if (data == null) return;

        foreach (var p in data)
        {
            Vector3Int cell = new(p.x, p.y, 0);
            var plot = new Plot
            {
                state = p.state,
                seedItemId = p.seedItemId,
                growthTimer = p.timer,
                growthStage = p.growthStage,
                plowedTimer = 0f
            };

            if (!string.IsNullOrEmpty(p.seedItemId) &&
                itemDatabase.Get(p.seedItemId) is SeedData seed)
            {
                plot.crop = seed.cropToPlant;
            }

            plots[cell] = plot;

            switch (plot.state)
            {
                case PlotState.Plowed:
                    farmingTilemap.SetTile(cell, plowedTile);
                    if (cropTilemap != null) cropTilemap.SetTile(cell, null);
                    SetCropBlock(cell, false);
                    break;

                case PlotState.Planted:
                case PlotState.Ready:
                    farmingTilemap.SetTile(cell, plowedTile);

                    if (cropTilemap != null && plot.crop != null && plot.crop.growthStages != null && plot.crop.growthStages.Length > 0)
                    {
                        int index = Mathf.Clamp(plot.growthStage, 0, plot.crop.growthStages.Length - 1);
                        cropTilemap.SetTile(cell, plot.crop.growthStages[index]);
                    }

                    SetCropBlock(cell, plot.state == PlotState.Ready);
                    break;

                default:
                    farmingTilemap.SetTile(cell, null);
                    if (cropTilemap != null) cropTilemap.SetTile(cell, null);
                    SetCropBlock(cell, false);
                    break;
            }
        }
    }

    // ================= ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ =================

    private Plot GetOrCreatePlot(Vector3Int cell)
    {
        if (!plots.TryGetValue(cell, out var plot))
        {
            plot = new Plot { state = PlotState.Empty };
            plots[cell] = plot;
        }
        return plot;
    }

    private bool IsNearPlayer(Vector3Int cell)
    {
        if (player == null) return true;
        Vector3Int p = groundTilemap.WorldToCell(player.position);
        return Mathf.Abs(cell.x - p.x) <= interactRadiusCells &&
               Mathf.Abs(cell.y - p.y) <= interactRadiusCells;
    }

    private bool TryGetPointer(out Vector2 pos, out int id)
    {
        pos = default;
        id = -1;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            pos = Mouse.current.position.ReadValue();
            return true;
        }
        return false;
    }

    private bool IsPointerOverUI(int id)
        => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(id);

    private bool IsBlockedByObject(Vector3Int cell)
    {
        Vector3 worldPos = groundTilemap.GetCellCenterWorld(cell);
        return Physics2D.OverlapPoint(worldPos, blockingLayers) != null;
    }

    private void SetPlot(Vector3Int cell, Plot plot, PlotState state, TileBase cropTile)
    {
        plot.state = state;

        // земля (вскопанная клетка)
        if (state == PlotState.Empty)
            farmingTilemap.SetTile(cell, null);
        else
            farmingTilemap.SetTile(cell, plowedTile);

        // растение (визуал)
        if (cropTilemap != null)
        {
            if (state == PlotState.Planted || state == PlotState.Ready)
                cropTilemap.SetTile(cell, cropTile);
            else
                cropTilemap.SetTile(cell, null);
        }

        // блокируем прохождение только по созревшим растениям
        SetCropBlock(cell, state == PlotState.Ready);

        if (state == PlotState.Plowed)
            plot.plowedTimer = 0f;

        if (state == PlotState.Empty)
        {
            plot.plowedTimer = 0f;
            plot.growthTimer = 0f;
            plot.growthStage = 0;
            plot.seedItemId = null;
            plot.crop = null;
        }
    }

    private void SetCropBlock(Vector3Int cell, bool blocked)
    {
        if (cropBlockTilemap == null)
            return;

        if (blocked)
        {
            if (cropBlockTile == null)
            {
                // Если забыли назначить тайл, просто ничего не делаем, чтобы не падало.
                return;
            }
            cropBlockTilemap.SetTile(cell, cropBlockTile);
        }
        else
        {
            cropBlockTilemap.SetTile(cell, null);
        }
    }
}
