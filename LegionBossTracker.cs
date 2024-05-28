using ExileCore;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using SharpDX;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Color = SharpDX.Color;
using Vector2 = System.Numerics.Vector2;

namespace LegionBossTracker;


class LegionBoss
{

    public Entity Entity;
    public bool IsNeedReset;
    public bool CoroutinesRunning;
    public LegionBoss(Entity entity)
    {

        Entity = entity;
        IsNeedReset = entity.IsHidden;
        CoroutinesRunning = false;
    }
    public string GetStatus()
    {
        return IsNeedReset ? "Alive" : Entity.IsAlive ? "Alive" : "Dead";
    }
    public Color GetStatusColor()
    {
        return IsNeedReset ? Color.Orange : Entity.IsAlive ? Color.Green : Color.Red;
    }


}

public class LegionBossTracker : BaseSettingsPlugin<Settings>
{
    const string KARUI_FISH = "Metadata/Monsters/LegionLeague/LegionKaruiGeneralFish";
    const string KARUI_GENMERAL = "Metadata/Monsters/LegionLeague/LegionKaruiGeneral";
    const string MARAKETH_GENERAL = "Metadata/Monsters/LegionLeague/LegionMarakethGeneral";
    const string ETERNAL_GENERAL = "Metadata/Monsters/LegionLeague/LegionEternalEmpireGeneral";
    const string VAAL_GENERAL = "Metadata/Monsters/LegionLeague/LegionVaalGeneral";
    const string TEMPLARE_GENERAL = "Metadata/Monsters/LegionLeague/LegionTemplarGeneral";

    private protected string[] WATCH_TARGETS = { KARUI_GENMERAL, MARAKETH_GENERAL, ETERNAL_GENERAL, VAAL_GENERAL, TEMPLARE_GENERAL };

    Dictionary<string, LegionBoss> _bosses = [];


    public override bool Initialise()
    {
        Name = "LegionBossTracker";
        return base.Initialise();
    }



    public override void DrawSettings()
    {
        base.DrawSettings();

    }


    public override void Render()
    {
        var inGameUi = GameController.Game.IngameState.IngameUi;
        if (inGameUi.FullscreenPanels.Any(x => x.IsVisible))
        {
            return;
        }
        if (GameController.Area.CurrentArea.Area.Name != "Domain of Timeless Conflict")
        {
            return;
        }
        var hight = _bosses.Count * 20 + 20;
        Graphics.DrawBox(new RectangleF(Settings.BoxPositionY, Settings.BoxPositionX, 280, hight), Settings.BoxBackgroundColor);

        var positionTextY = Settings.BoxPositionY + 10;
        var positionTextX = Settings.BoxPositionX + 10;

        foreach (var boss in _bosses)
        {
            Graphics.DrawText($"{boss.Key}: ", new Vector2(positionTextX, positionTextY), Color.White, 11);
            Graphics.DrawText($"{boss.Value.GetStatus()}", new Vector2(positionTextX + 200, positionTextY), boss.Value.GetStatusColor(), 11);
            positionTextY += 20;
        }
        if (Settings.Debug)
        {
            int posY = 300;
            foreach (var boss in _bosses)
            {
                Graphics.DrawText(boss.Value.ToString(), new Vector2(50, posY += 20), Color.White, 11);
                Graphics.DrawText($"IsAlive:{boss.Value.Entity.IsAlive} " +
                                    $"IsDead:{boss.Value.Entity.IsDead} " +
                                    $"IsTargetable:{boss.Value.Entity.IsTargetable} " +
                                    $"IsValid:{boss.Value.Entity.IsValid}" +
                                    $"IsHidden:{boss.Value.Entity.IsHidden}"
                                   , new Vector2(50, posY += 20), Color.White, 11);

            }
        }

    }

    public override Job Tick()
    {

        if (GameController.Area.CurrentArea.Area.Name == "Domain of Timeless Conflict")
        {
            foreach (var entity in GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Monster].Where(x => WATCH_TARGETS.Contains(x.Metadata)))
            {
                if (string.IsNullOrEmpty(entity.RenderName)) continue;


                if (!_bosses.TryAdd(entity.RenderName, new LegionBoss(entity)))
                {
                    _bosses[entity.RenderName].Entity = entity;
                    if (!_bosses[entity.RenderName].IsNeedReset && entity.IsDead && !_bosses[entity.RenderName].CoroutinesRunning)
                    {
                        _bosses[entity.RenderName].CoroutinesRunning = true;
                        var coroutine = new Coroutine(WaitAndResetStatus(entity.RenderName), this);
                        Core.ParallelRunner.Run(coroutine);
                        continue;
                    }
                    if (_bosses[entity.RenderName].IsNeedReset && entity.IsAlive && !entity.IsHidden)
                    {
                        _bosses[entity.RenderName].IsNeedReset = false;
                    }

                }
            }

        }
        else
        {
            _bosses.Clear();
        }


        return null;
    }

    private IEnumerator WaitAndResetStatus(string key)
    {

        yield return new WaitTime(25000);
        _bosses[key].IsNeedReset = true;
        _bosses[key].CoroutinesRunning = false;

    }
}
