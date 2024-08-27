using ExileCore;
using ExileCore.PoEMemory.Components;
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
using Vector3 = System.Numerics.Vector3;
using ExileCore.PoEMemory;
using MoreLinq;
using System.Threading.Tasks;

namespace LegionBossTracker;


class LegionBoss(Entity entity)
{
    public Entity Entity = entity;
    public bool IsNeedReset = true;
    public bool IsAlive = true;
    public bool IsDead = false;
    public Coroutine coroutine;
    public Color ColorText = BossInfo.Colors[entity.Metadata];
    public string GetStatus()
    {
        return IsNeedReset ? "Need reset" : IsAlive ? "Alive" : "Dead";
    }
    public Color GetStatusColor()
    {
        return IsNeedReset ? Color.Orange : IsAlive ? Color.Green : Color.Red;
    }

}
public static class BossInfo

{
    public const string KARUI_FISH = "Metadata/Monsters/LegionLeague/LegionKaruiGeneralFish";
    public const string KARUI_GENMERAL = "Metadata/Monsters/LegionLeague/LegionKaruiGeneral";
    public const string MARAKETH_GENERAL = "Metadata/Monsters/LegionLeague/LegionMarakethGeneral";
    public const string ETERNAL_GENERAL = "Metadata/Monsters/LegionLeague/LegionEternalEmpireGeneral";
    public const string VAAL_GENERAL = "Metadata/Monsters/LegionLeague/LegionVaalGeneral";
    public const string TEMPLARE_GENERAL = "Metadata/Monsters/LegionLeague/LegionTemplarGeneral";
    public const string MARAKETH_GENERAL_DISMOUNTED = "Metadata/Monsters/LegionLeague/LegionMarakethGeneralDismounted";
    public static Dictionary<string, Color> Colors { get; set; } = new()
    {
        { KARUI_GENMERAL, Color.Orange },
        { MARAKETH_GENERAL, Color.Yellow },
        { MARAKETH_GENERAL_DISMOUNTED, Color.Yellow },
        { ETERNAL_GENERAL, Color.White },
        { VAAL_GENERAL, Color.Red },
        { TEMPLARE_GENERAL, Color.Green }
    };
}


public class LegionBossTracker : BaseSettingsPlugin<Settings>
{
    private Entity _legionEndlessInitiator;
    private protected string[] WATCH_TARGETS = [BossInfo.KARUI_GENMERAL, BossInfo.MARAKETH_GENERAL, BossInfo.ETERNAL_GENERAL, BossInfo.VAAL_GENERAL, BossInfo.TEMPLARE_GENERAL, BossInfo.MARAKETH_GENERAL_DISMOUNTED];
    private readonly Dictionary<string, LegionBoss> _bosses = [];
    public override bool Initialise()
    {
        Name = "LegionBossTracker";
        return base.Initialise();
    }


    public override void Render()
    {
        IngameUIElements inGameUi = GameController.Game.IngameState.IngameUi;
        if (inGameUi.FullscreenPanels.Any(x => x.IsVisible))
        {
            return;
        }
        if (GameController.Area.CurrentArea.Area.Name != "Domain of Timeless Conflict")
        {
            return;
        }
        int hight = _bosses.Count * 20 + 20;
        Graphics.DrawBox(new RectangleF(Settings.BoxPositionX, Settings.BoxPositionY, 280, hight), Settings.BoxBackgroundColor);

        int positionTextY = Settings.BoxPositionY + 10;
        int positionTextX = Settings.BoxPositionX + 10;

        foreach (KeyValuePair<string, LegionBoss> boss in _bosses)
        {
            Color color = Settings.EnableColorization ? BossInfo.Colors[boss.Value.Entity.Metadata] : Color.White;
            Graphics.DrawText($"{boss.Key}: ", new Vector2(positionTextX, positionTextY), color);
            Graphics.DrawText($"{boss.Value.GetStatus()}", new Vector2(positionTextX + 200, positionTextY), boss.Value.GetStatusColor());
            positionTextY += 20;
        }

        if (Settings.DrawWorldLine)
        {
            Entity player = GameController?.Player;
            Vector2 playerPos = GameController.IngameState.Data.GetGridScreenPosition(player.GridPosNum);
            foreach (KeyValuePair<string, LegionBoss> boss in _bosses)
            {
                if (boss.Value.IsAlive && boss.Value.Entity.IsValid)
                {
                    Color color = Settings.EnableColorization ? boss.Value.ColorText : Color.White;
                    boss.Value.Entity.TryGetComponent<Render>(out Render renderComp);
                    Vector2 bossPos = GameController.IngameState.Data.GetGridScreenPosition(boss.Value.Entity.GridPosNum);
                    Graphics.DrawLine(playerPos, bossPos, 2, color);
                    DrawWorldCircle(boss.Value.Entity.PosNum, renderComp.BoundsNum.X, color);
                }
            }

        }
        if (Settings.Debug)
        {
            int posY = 300;
            var test = GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Monster].Where(x =>
            x.Rarity == MonsterRarity.Unique
            ).ToList();
            foreach (Entity entity in test)
            {

                Graphics.DrawText($"{entity.RenderName} {entity.Metadata}", new Vector2(50, posY += 20), Color.White);
                Graphics.DrawText($"IsAlive:{entity.IsAlive}   " +
                                       $"IsDead:{entity.IsDead}   " +
                                       $"IsTargetable:{entity.IsTargetable} " +
                                       $"IsValid:{entity.IsValid}   " +
                                       $"IsHidden:{entity.IsHidden}  "
                                      , new Vector2(50, posY += 20), Color.White);
                if (entity.TryGetComponent(out StateMachine state))
                {
                    state.States.ForEach(x =>
                                  {
                                      _ = Graphics.DrawText($"{x.Name}  :: {x.Value} ", new Vector2(50, posY += 20), Color.White);
                                  });
                }
            }
            Entity player = GameController?.Player;
            Vector2 playerPos = GameController.IngameState.Data.GetGridScreenPosition(player.GridPosNum);
            GameController.EntityListWrapper.OnlyValidEntities.Where(x =>
             x.Type is not EntityType.Monster and not EntityType.Daemon and not EntityType.Player and not EntityType.Effect and not EntityType.MiscellaneousObjects).ToList()
               .ForEach(x =>
            {
                if (x.Type is EntityType.Terrain)
                {

                    Vector2 bossPos = GameController.IngameState.Data.GetGridScreenPosition(x.GridPosNum);
                    Graphics.DrawLine(playerPos, bossPos, 2, Color.BlueViolet);

                    if (x.TryGetComponent(out StateMachine state))
                    {
                        state.States.ForEach(x =>
                        {
                            Graphics.DrawText($"Crystal {x.Name}  :: {x.Value} ", new Vector2(50, posY += 20), Color.White);
                        });
                    }


                }
            });
        }

    }
    private void DrawWorldCircle(Vector3 position, float radius, Color color)
    {
        RectangleF screenSize = new()
        {
            X = 0,
            Y = 0,
            Width = GameController.Window.GetWindowRectangleTimeCache.Size.Width,
            Height = GameController.Window.GetWindowRectangleTimeCache.Size.Height
        };

        Vector2 entityPos = RemoteMemoryObject.pTheGame.IngameState.Camera.WorldToScreen(position);
        if (IsEntityWithinScreen(entityPos, screenSize, 50))
        {
            Graphics.DrawCircleInWorld(position, radius, color, 2);
        }
    }
    private static bool IsEntityWithinScreen(Vector2 entityPos, RectangleF screenSize, float allowancePX)
    {
        float leftBound = screenSize.Left - allowancePX;
        float rightBound = screenSize.Right + allowancePX;
        float topBound = screenSize.Top - allowancePX;
        float bottomBound = screenSize.Bottom + allowancePX;
        return entityPos.X >= leftBound && entityPos.X <= rightBound &&
               entityPos.Y >= topBound && entityPos.Y <= bottomBound;
    }

    public override Job Tick()
    {
        if (GameController.Area.CurrentArea.Area.Name == "Domain of Timeless Conflict")
        {
            if (_legionEndlessInitiator == null || !_legionEndlessInitiator.IsValid)
            {
                Entity legionEndlessInitiator = GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Terrain].FirstOrDefault(x => x.Metadata.Equals("Metadata/Terrain/Leagues/Legion/Objects/LegionEndlessInitiator", StringComparison.Ordinal));
                if (legionEndlessInitiator != null)
                {
                    _legionEndlessInitiator = legionEndlessInitiator;
                }
                else if (_legionEndlessInitiator == null)
                {
                    return null;
                }
            }


            var bosses = GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Monster].Where(x => WATCH_TARGETS.Contains(x.Metadata)).ToList();
            foreach (Entity entity in bosses)
            {
                if (string.IsNullOrEmpty(entity.RenderName))
                {
                    continue;
                }
                if (!_bosses.TryAdd(entity.RenderName, new LegionBoss(entity)))
                {
                    LegionBoss boss = _bosses[entity.RenderName];
                    boss.Entity = entity;
                    if (boss.IsNeedReset && entity.IsAlive && !entity.IsHidden)
                    {
                        boss.IsNeedReset = false;
                        boss.IsAlive = true;
                        boss.IsDead = false;
                    }
                    if (entity.IsAlive && boss.coroutine != null)
                    {
                        boss.IsNeedReset = false;
                        boss.IsAlive = true;
                        boss.IsDead = false;
                        boss.coroutine = null;
                    }
                    if (entity.Metadata != BossInfo.MARAKETH_GENERAL && !boss.IsNeedReset && entity.IsDead && boss.coroutine == null)
                    {
                        boss.IsAlive = false;
                        boss.IsDead = true;
                        boss.coroutine = new Coroutine(WaitAndResetStatus(boss), this);
                        _ = Core.ParallelRunner.Run(boss.coroutine);
                    }

                }
            }
            _ = DelayedCheckControlZoneState();

        }
        else
        {
            _bosses.Clear();
            _legionEndlessInitiator = null;
        }
        return null;
    }

    private async Task DelayedCheckControlZoneState()
    {
        var stateMachineComponent = _legionEndlessInitiator?.GetComponent<StateMachine>();
        var stateObject = stateMachineComponent?.States.FirstOrDefault(s => s.Name == "checking_control_zone");
        var state = stateObject?.Value ?? 1;
        await Task.Delay(1000);
        foreach (LegionBoss boss in _bosses.Values)
        {
            if (state == 0 && boss.IsNeedReset)
            {
                boss.IsNeedReset = false;
                boss.IsAlive = true;
                boss.IsDead = false;
            }
        }
    }
    private static IEnumerator WaitAndResetStatus(LegionBoss boss)
    {
        for (int i = 0; i < 25000; i += 100)
        {
            if (boss.coroutine == null)
            {
                yield break;
            }
            yield return new WaitTime(100);
        }
        if (boss != null)
        {
            boss.IsNeedReset = true;
            boss.IsAlive = true;
            boss.IsDead = false;
            boss.coroutine = null;
        }

    }


}
