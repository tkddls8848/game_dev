// 바깥 루프. 계절마다 심고, 그 계절이 끝날 때 침입을 맞는다.
//
// hybrid-harvest-deck 은 한 해에 밤이 하나고 덱은 그 밤에만 산다.
// 여기서는 밤이 넷이고 **덱이 해를 넘어 남는다** — 그래서 시간축이 생긴다.
using System;
using System.Collections.Generic;
using System.Text;

namespace HybridSiegeSeasons
{
    public sealed class SiegeOutcome
    {
        public string SeasonId;
        public string EnemyId;
        public int EnemyHp;
        public bool IsBoss;
        public bool Held;
        public int PlayerHpLeft;
        public int DeckBefore;
        public int CardsBroken;       // 다 닳아서 사라진 장수
        public int CardsTidied;       // '정리'로 없앤 장수
        public bool EmptyDeck;
    }

    public sealed class YearOutcome
    {
        public int Year;
        public int SeedsAtStart;
        public int FertilizerAtStart;
        public int UnlockedPlots;
        public FieldYear Field;
        public List<SiegeOutcome> Sieges = new List<SiegeOutcome>();
        public bool Held;             // 네 침입을 모두 막았는가
        public int HarvestLost;       // 진 침입 때문에 도로 빼앗긴 그해 수확 장수
        public int CardsDecayed;      // 겨울을 나며 삭아 없어진 장수
        public int DeckAfter;
        public int DeckUsesAfter;
        public int SeedsCarried;
        public int FertilizerCarried;
    }

    public sealed class CampaignResult
    {
        public List<YearOutcome> Years = new List<YearOutcome>();
        public int Wins;
        public int Losses;
        public int MaxSeedsSeen;
        public int MaxFertilizerSeen;
        public int MaxPlotsSeen;
        public int MaxDeckSeen;
    }

    /// <summary>성 하나의 영속 상태. 덱이 여기 산다.</summary>
    public sealed class Fortress
    {
        public readonly StandingDeck Deck = new StandingDeck();
        public int Seeds;
        public int Fertilizer;
        public int Plots;
    }

    public sealed class SeasonLoop
    {
        readonly GameData _data;
        readonly FieldSim _field;
        readonly SiegeBattleSim _battle;

        public SeasonLoop(GameData data)
        {
            _data = data;
            _field = new FieldSim(data);
            _battle = new SiegeBattleSim(data);
        }

        /// <summary>
        /// 한 해. 계절마다 심고 그 계절의 침입을 치른다. 덱은 성에 남는다.
        /// 적 체력 = 기준 × 계절 격화(%) × 해 성장(%). 둘 다 백분율 정수 + 나머지 누적이다.
        /// </summary>
        public YearOutcome RunYear(Fortress f, int year, IPlantingPolicy planting, IPlayPolicy play,
                                   Rng rng, StringBuilder transcript = null)
        {
            var e = _data.Balance.economy;
            var outcome = new YearOutcome
            {
                Year = year,
                SeedsAtStart = f.Seeds,
                FertilizerAtStart = f.Fertilizer,
                UnlockedPlots = f.Plots,
                Held = true
            };

            int yearPct = 100 + (year - 1) * _data.Balance.year.hpGrowthPctPerYear;
            if (yearPct > _data.Balance.year.hpScaleMaxPct) yearPct = _data.Balance.year.hpScaleMaxPct;
            var escAcc = new PercentAccumulator();
            var yearAcc = new PercentAccumulator();

            // 심기: 세 계절을 한 번에 돌린다. 어느 계절에 무엇이 났는지는 Plantings 에 남는다.
            outcome.Field = _field.RunYear(f.Seeds, f.Fertilizer, f.Plots, planting, f.Deck.UsesByCard());

            var seasons = new List<SeasonDef>(_data.Seasons);
            seasons.Sort((a, b) => a.order.CompareTo(b.order));

            foreach (var season in seasons)
            {
                // 그 계절의 수확을 성으로 들인다 (겨울에는 심지 못하므로 들어올 것이 없다)
                foreach (var p in outcome.Field.Plantings)
                {
                    if (p.SeasonId != season.id || p.HarvestTurn < 0 || p.Consumed) continue;
                    p.Consumed = true;
                    f.Deck.Add(_data.Card(_data.Crop(p.CropId).cardId), p.CardsYielded, year, season.id);
                }

                var siege = _data.SiegeOf(season.id);
                if (siege == null) continue;

                var baseEnemy = _data.Enemy(siege.enemyId);
                int hp = yearAcc.Apply(escAcc.Apply(baseEnemy.hp, siege.escalationPct), yearPct);
                var enemy = new EnemyDef
                {
                    id = baseEnemy.id, nameKo = baseEnemy.nameKo, nameEn = baseEnemy.nameEn,
                    glyph = baseEnemy.glyph, hp = hp < 1 ? 1 : hp, pattern = baseEnemy.pattern
                };

                var so = new SiegeOutcome
                {
                    SeasonId = season.id, EnemyId = enemy.id, EnemyHp = enemy.hp,
                    IsBoss = siege.isBoss, DeckBefore = f.Deck.Count
                };

                if (f.Deck.Count == 0)
                {
                    so.EmptyDeck = true;
                    so.Held = false;
                    so.PlayerHpLeft = _data.Year.playerHp;
                    transcript?.Append("S:empty\n");
                }
                else
                {
                    // 체력은 침입마다 가득 찬다. 이 PoC 가 쌓는 자원은 체력이 아니라 카드의 남은 횟수다.
                    var r = _battle.Run(enemy, f.Deck, _data.Year.playerHp, play, rng, transcript);
                    so.Held = r.PlayerWon;
                    so.PlayerHpLeft = r.PlayerHpLeft;
                    foreach (var gone in r.LeftTheDeck)
                    {
                        if (gone.Broken) so.CardsBroken++; else so.CardsTidied++;
                        f.Deck.Remove(gone);
                    }
                }

                outcome.Sieges.Add(so);
                if (!so.Held)
                {
                    outcome.Held = false;
                    // 지면 수확을 잃는다 — 계절 침입은 그 계절의 것만, 겨울(보스)을 놓치면 그 해 전부.
                    // 계절마다 한 해치를 통째로 빼앗으면 한 번 지는 순간 회복 불가능한 나선이 된다.
                    outcome.HarvestLost += siege.isBoss
                        ? f.Deck.RemoveHarvestOfYear(year)
                        : f.Deck.RemoveHarvestOfSeason(year, season.id);
                }
            }

            // ── 겨울나기: 창고의 모든 카드가 한 번씩 닳는다 ───────────
            // 밭이 내는 속도도 상수, 침입이 쓰는 속도도 대체로 상수다.
            // 덱 크기에 비례하는 손실이 하나도 없으면 덱은 해마다 선형으로 불어난다.
            int decay = _data.Balance.year.winterDecayUses;
            if (decay > 0)
            {
                foreach (var c in f.Deck.Cards) c.Uses -= decay;
                outcome.CardsDecayed = f.Deck.SweepBroken();
            }

            // ── 해 경계: 자원이 느는 유일한 지점 ──────────────────────
            f.Seeds = Math.Min(outcome.Field.SeedsLeft, e.seedCarryCap);
            f.Fertilizer = Math.Min(outcome.Field.FertilizerLeft, e.fertilizerCarryCap);
            if (outcome.Held)
            {
                f.Seeds += e.seedWinBonus;
                f.Fertilizer += e.fertilizerWinBonus;
                if (f.Plots < _data.MaxPlots && f.Plots < _data.Plots.Count) f.Plots++;
            }
            f.Seeds = Math.Min(f.Seeds, e.seedCeiling);
            f.Fertilizer = Math.Min(f.Fertilizer, e.fertilizerCeiling);

            outcome.SeedsCarried = f.Seeds;
            outcome.FertilizerCarried = f.Fertilizer;
            outcome.DeckAfter = f.Deck.Count;
            outcome.DeckUsesAfter = f.Deck.TotalUses;
            return outcome;
        }

        /// <summary>여러 해. 성 하나가 계속 이어진다 — 덱이 그 사이를 건넌다.</summary>
        public CampaignResult Run(int years, IPlantingPolicy planting, IPlayPolicy play, int seed)
        {
            var e = _data.Balance.economy;
            var rng = new Rng(seed);
            var f = new Fortress { Seeds = 0, Fertilizer = 0, Plots = _data.StartPlots };
            var result = new CampaignResult();

            for (int y = 1; y <= years; y++)
            {
                f.Seeds = Math.Min(f.Seeds + e.seedStipendPerYear, e.seedCeiling);

                if (f.Seeds > result.MaxSeedsSeen) result.MaxSeedsSeen = f.Seeds;
                if (f.Fertilizer > result.MaxFertilizerSeen) result.MaxFertilizerSeen = f.Fertilizer;
                if (f.Plots > result.MaxPlotsSeen) result.MaxPlotsSeen = f.Plots;

                var yr = RunYear(f, y, planting, play, rng);
                result.Years.Add(yr);
                if (yr.Held) result.Wins++; else result.Losses++;
                if (yr.DeckAfter > result.MaxDeckSeen) result.MaxDeckSeen = yr.DeckAfter;
            }
            return result;
        }
    }
}
