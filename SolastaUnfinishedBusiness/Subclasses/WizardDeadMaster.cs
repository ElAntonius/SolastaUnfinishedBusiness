﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SolastaUnfinishedBusiness.Api.GameExtensions;
using SolastaUnfinishedBusiness.Api.Helpers;
using SolastaUnfinishedBusiness.Api.LanguageExtensions;
using SolastaUnfinishedBusiness.Builders;
using SolastaUnfinishedBusiness.Builders.Features;
using SolastaUnfinishedBusiness.CustomInterfaces;
using SolastaUnfinishedBusiness.CustomUI;
using SolastaUnfinishedBusiness.Properties;
using UnityEngine.AddressableAssets;
using static RuleDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.CharacterFamilyDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.ItemDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.MonsterDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.SpellDefinitions;
using static SolastaUnfinishedBusiness.Subclasses.CommonBuilders;

namespace SolastaUnfinishedBusiness.Subclasses;

internal sealed class WizardDeadMaster : AbstractSubclass
{
    private const string WizardDeadMasterName = "WizardDeadMaster";
    private const string CreateDeadTag = "DeadMasterMinion";
    internal static readonly List<SpellDefinition> DeadMasterSpells = new();

    internal WizardDeadMaster()
    {
        var autoPreparedSpellsDeadMaster = FeatureDefinitionAutoPreparedSpellsBuilder
            .Create("AutoPreparedSpellsDeadMaster")
            .SetGuiPresentation(Category.Feature)
            .SetSpellcastingClass(CharacterClassDefinitions.Wizard)
            .SetAutoTag("College")
            .SetPreparedSpellGroups(GetDeadSpellAutoPreparedGroups())
            .AddToDB();

        var featureSetDeadMasterNecromancyBonusDc = FeatureDefinitionFeatureSetBuilder
            .Create("FeatureSetDeadMasterNecromancyBonusDC")
            .SetGuiPresentation(Category.Feature)
            .AddToDB();

        foreach (var spellDefinition in DatabaseRepository.GetDatabase<SpellDefinition>()
                     .Where(x => x.SchoolOfMagic == SchoolNecromancy))
        {
            featureSetDeadMasterNecromancyBonusDc.FeatureSet.Add(
                FeatureDefinitionMagicAffinityBuilder
                    .Create($"MagicAffinityDeadMaster{spellDefinition.Name}")
                    .SetGuiPresentationNoContent(true)
                    .SetSpellWithModifiedSaveDc(spellDefinition, 1)
                    .AddToDB());
        }

        var bypassSpellConcentrationDeadMaster = FeatureDefinitionBuilder
            .Create("BypassSpellConcentrationDeadMaster")
            .SetGuiPresentation(Category.Feature)
            .SetCustomSubFeatures(new BypassSpellConcentrationDeadMaster())
            .AddToDB();

        var targetReducedToZeroHpDeadMasterStarkHarvest = FeatureDefinitionBuilder
            .Create("TargetReducedToZeroHpDeadMasterStarkHarvest")
            .SetGuiPresentation(Category.Feature)
            .AddToDB();

        targetReducedToZeroHpDeadMasterStarkHarvest.SetCustomSubFeatures(
            new StarkHarvest(targetReducedToZeroHpDeadMasterStarkHarvest));

        const string ChainsName = "SummoningAffinityDeadMasterUndeadChains";

        var hpBonus = FeatureDefinitionAttributeModifierBuilder
            .Create("AttributeModifierDeadMasterUndeadChains")
            .SetGuiPresentationNoContent(true)
            .SetModifier(FeatureDefinitionAttributeModifier.AttributeModifierOperation.AddConditionAmount,
                AttributeDefinitions.HitPoints)
            .AddToDB();

        var attackBonus = FeatureDefinitionAttackModifierBuilder
            .Create("AttackModifierDeadMasterUndeadChains")
            .SetGuiPresentation(ChainsName, Category.Feature)
            .SetAttackRollModifier(method: AttackModifierMethod.SourceConditionAmount)
            .AddToDB();

        var deadMasterUndeadChains = FeatureDefinitionSummoningAffinityBuilder
            .Create(ChainsName)
            .SetGuiPresentation(Category.Feature)
            .SetRequiredMonsterTag(CreateDeadTag)
            .SetAddedConditions(ConditionDefinitionBuilder
                    .Create("ConditionDeadMasterUndeadChainsProficiency")
                    .SetGuiPresentation(ChainsName, Category.Feature)
                    .SetSilent(Silent.WhenAddedOrRemoved)
                    .SetPossessive()
                    .SetAmountOrigin(ExtraOriginOfAmount.SourceProficiencyBonus)
                    .SetFeatures(attackBonus)
                    .AddToDB(),
                ConditionDefinitionBuilder
                    .Create("ConditionDeadMasterUndeadChainsLevel")
                    .SetGuiPresentation(ChainsName, Category.Feature)
                    .SetSilent(Silent.WhenAddedOrRemoved)
                    .SetPossessive()
                    .SetAmountOrigin(ExtraOriginOfAmount.SourceClassLevel, WizardClass)
                    .SetFeatures(hpBonus, hpBonus)
                    .AddToDB())
            .AddToDB();

        Subclass = CharacterSubclassDefinitionBuilder
            .Create(WizardDeadMasterName)
            .SetGuiPresentation(Category.Subclass,
                Sprites.GetSprite("WizardDeadMaster", Resources.WizardDeadMaster, 256))
            .AddFeaturesAtLevel(2,
                bypassSpellConcentrationDeadMaster,
                featureSetDeadMasterNecromancyBonusDc,
                autoPreparedSpellsDeadMaster,
                deadMasterUndeadChains)
            .AddFeaturesAtLevel(6,
                targetReducedToZeroHpDeadMasterStarkHarvest)
            .AddFeaturesAtLevel(10,
                DamageAffinityGenericHardenToNecrotic)
            .AddFeaturesAtLevel(14,
                PowerCasterCommandUndead)
            .AddToDB();
    }

    internal override CharacterSubclassDefinition Subclass { get; }

    internal override FeatureDefinitionSubclassChoice SubclassChoice =>
        FeatureDefinitionSubclassChoices.SubclassChoiceWizardArcaneTraditions;

    // ReSharper disable once UnassignedGetOnlyAutoProperty
    internal override DeityDefinition DeityDefinition { get; }

    [NotNull]
    private static FeatureDefinitionAutoPreparedSpells.AutoPreparedSpellsGroup[] GetDeadSpellAutoPreparedGroups()
    {
        var createDeadSpellMonsters =
            new Dictionary<(int clazz, int spell),
                List<(MonsterDefinition monster, int number, AssetReferenceSprite icon, BaseDefinition[] attackSprites
                    )>>
            {
                {
                    (3, 2), new()
                    {
                        (Skeleton, 1, Sprites.SpellRaiseSkeleton, new BaseDefinition[] { Scimitar }),
                        (Skeleton_Archer, 1, Sprites.SpellRaiseSkeletonArcher,
                            new BaseDefinition[] { Shortbow, Shortsword })
                    }
                }, //CR 0.25 x2
                {
                    (5, 3), new()
                    {
                        (Ghoul, 1, Sprites.SpellRaiseGhoul,
                            new BaseDefinition[]
                            {
                                MonsterAttackDefinitions.Attack_Wildshape_GiantEagle_Talons,
                                MonsterAttackDefinitions.Attack_Wildshape_Wolf_Bite
                            })
                    }
                }, //CR 1
                {
                    (7, 4), new()
                    {
                        (Skeleton_Enforcer, 1, Sprites.SpellRaiseSkeletonEnforcer,
                            new BaseDefinition[] { Battleaxe, MonsterAttackDefinitions.Attack_Wildshape_Ape_Toss_Rock })
                    }
                }, //CR 2
                {
                    (9, 5), new()
                    {
                        (Skeleton_Knight, 1, Sprites.SpellRaiseSkeletonKnight, new BaseDefinition[] { Longsword }),
                        (Skeleton_Marksman, 1, Sprites.SpellRaiseSkeletonMarksman,
                            new BaseDefinition[] { Longbow, Shortsword })
                    }
                }, //CR 3
                {
                    (11, 6),
                    new() { (Ghost, 1, Sprites.SpellRaiseGhost, new BaseDefinition[] { Enchanted_Dagger_Souldrinker }) }
                }, //CR 4
                {
                    (13, 7),
                    new() { (Wight, 1, Sprites.SpellRaiseWight, new BaseDefinition[] { LongswordPlus2, LongbowPlus1 }) }
                }, //CR 3 x2
                {
                    (15, 8), new()
                    {
                        (WightLord, 1, Sprites.SpellRaiseWightLord,
                            new BaseDefinition[] { Enchanted_Longsword_Frostburn, Enchanted_Shortbow_Medusa })
                    }
                } //CR 6
            };

        var result = new List<FeatureDefinitionAutoPreparedSpells.AutoPreparedSpellsGroup>();

        foreach (var kvp in createDeadSpellMonsters)
        {
            var (clazz, spell) = kvp.Key;
            var monsters = kvp.Value;
            var spells = new List<SpellDefinition>();

            foreach (var (monsterDefinition, count, icon, attackSprites) in monsters)
            {
                var monster = MakeSummonedMonster(monsterDefinition, attackSprites);
                var title = Gui.Format("Spell/&SpellRaiseDeadFormatTitle",
                    monster.FormatTitle());
                var description = Gui.Format("Spell/&SpellRaiseDeadFormatDescription",
                    monster.FormatTitle(),
                    monster.FormatDescription());

                var duration = clazz switch
                {
                    >= 15 => 24 * 60,
                    >= 13 => 8 * 60,
                    >= 9 => 60,
                    >= 5 => 10,
                    _ => 1
                };

                var createDeadSpell = SpellDefinitionBuilder
                    .Create($"CreateDead{monster.name}")
                    .SetGuiPresentation(title, description, icon)
                    .SetRequiresConcentration(true)
                    .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolNecromancy)
                    .SetSpellLevel(spell)
                    .SetMaterialComponent(MaterialComponentType.Mundane)
                    .SetVocalSpellSameType(VocalSpellSemeType.Debuff)
                    .SetCastingTime(ActivationTime.Action)
                    .SetEffectDescription(EffectDescriptionBuilder.Create()
                        .SetTargetingData(Side.All, RangeType.Distance, 6, TargetType.Position, count)
                        .SetDurationData(DurationType.Minute, duration)
                        .SetEffectAdvancement(EffectIncrementMethod.PerAdditionalSlotLevel, 2,
                            additionalSummonsPerIncrement: 1)
                        .SetParticleEffectParameters(VampiricTouch)
                        .SetEffectForms(EffectFormBuilder.Create()
                            .SetSummonCreatureForm(1, monster.Name)
                            .Build())
                        .Build())
                    .AddToDB();

                // create non concentration versions to be used whenever upcast
                _ = SpellDefinitionBuilder
                    .Create(createDeadSpell, $"CreateDead{monster.name}NoConcentration")
                    .SetRequiresConcentration(false)
                    .AddToDB();

                spells.Add(createDeadSpell);
            }

            result.Add(new FeatureDefinitionAutoPreparedSpells.AutoPreparedSpellsGroup
            {
                ClassLevel = clazz, SpellsList = spells
            });
        }

        DeadMasterSpells.SetRange(result.SelectMany(x => x.SpellsList));
        FeatureDefinitionPowers.PowerWightLordRetaliate.rechargeRate = RechargeRate.ShortRest;
        FeatureDefinitionPowers.PowerWightLordRetaliate.activationTime = ActivationTime.BonusAction;


        return result.ToArray();
    }

    private static MonsterDefinition MakeSummonedMonster(
        MonsterDefinition monster,
        IReadOnlyList<BaseDefinition> attackSprites)
    {
        var modified = MonsterDefinitionBuilder
            .Create(monster, $"Risen{monster.Name}")
            .SetDefaultFaction(FactionDefinitions.Party)
            .SetBestiaryEntry(BestiaryDefinitions.BestiaryEntry.None)
            .SetDungeonMakerPresence(MonsterDefinition.DungeonMaker.None)
            .AddCreatureTags(CreateDeadTag)
            .SetFullyControlledWhenAllied(true)
            .SetDroppedLootDefinition(null)
            .AddToDB();

        if (attackSprites == null)
        {
            return modified;
        }

        for (var i = 0; i < attackSprites.Count; i++)
        {
            var attack = modified.AttackIterations.ElementAtOrDefault(i);

            if (attack != null)
            {
                attack.MonsterAttackDefinition.GuiPresentation.spriteReference =
                    attackSprites[i].GuiPresentation.SpriteReference;
            }
        }

        return modified;
    }

    private sealed class BypassSpellConcentrationDeadMaster : IBypassSpellConcentration
    {
        public IEnumerable<SpellDefinition> SpellDefinitions()
        {
            return DeadMasterSpells;
        }

        public int OnlyWithUpcastGreaterThan()
        {
            return 1;
        }
    }

    private sealed class StarkHarvest : ITargetReducedToZeroHp
    {
        private readonly FeatureDefinition feature;

        public StarkHarvest(FeatureDefinition feature)
        {
            this.feature = feature;
        }

        public IEnumerator HandleCharacterReducedToZeroHp(
            GameLocationCharacter attacker,
            GameLocationCharacter downedCreature,
            RulesetAttackMode attackMode,
            RulesetEffect activeEffect)
        {
            if (activeEffect is not RulesetEffectSpell spellEffect || spellEffect.SpellDefinition.SpellLevel == 0)
            {
                yield break;
            }

            if (downedCreature.RulesetCharacter is not { IsDeadOrDying: true })
            {
                yield break;
            }

            var rulesetDowned = downedCreature.RulesetCharacter;
            var characterFamily = rulesetDowned.CharacterFamily;

            if (characterFamily == Construct.Name || characterFamily == Undead.Name)
            {
                yield break;
            }

            var usedSpecialFeatures = attacker.UsedSpecialFeatures;

            usedSpecialFeatures.TryAdd(feature.Name, 0);

            if (usedSpecialFeatures[feature.Name] > 0)
            {
                yield break;
            }

            usedSpecialFeatures[feature.Name]++;

            var rulesetAttacker = attacker.RulesetCharacter;
            var spell = spellEffect.SpellDefinition;
            var isNecromancy = spell.SchoolOfMagic == SchoolNecromancy;
            var healingReceived = (isNecromancy ? 3 : 2) * spell.SpellLevel;

            GameConsoleHelper.LogCharacterUsedFeature(rulesetAttacker, feature, indent: true);

            if (rulesetAttacker.MissingHitPoints > 0)
            {
                rulesetAttacker.ReceiveHealing(healingReceived, true, rulesetAttacker.Guid);
            }
            else if (rulesetAttacker.TemporaryHitPoints <= healingReceived)
            {
                rulesetAttacker.ReceiveTemporaryHitPoints(healingReceived, DurationType.Minute, 1,
                    TurnOccurenceType.EndOfTurn, rulesetAttacker.Guid);
            }
        }
    }
}
