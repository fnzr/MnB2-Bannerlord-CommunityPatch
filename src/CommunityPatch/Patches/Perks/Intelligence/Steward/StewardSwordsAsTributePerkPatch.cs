using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.GameComponents.Party;
using TaleWorlds.Core;
using static System.Reflection.BindingFlags;
using static CommunityPatch.HarmonyHelpers;

namespace CommunityPatch.Patches.Perks.Intelligence.Steward {

  public sealed class SwordsAsTributePatch : PerkPatchBase<SwordsAsTributePatch> {

    public override bool Applied { get; protected set; }

    private static readonly MethodInfo TargetMethodInfo = typeof(DefaultPartySizeLimitModel).GetMethod("CalculateMobilePartyMemberSizeLimit", NonPublic | Instance | DeclaredOnly);

    private static readonly MethodInfo PatchMethodInfo = typeof(SwordsAsTributePatch).GetMethod(nameof(Postfix), NonPublic | Static | DeclaredOnly);

    public override IEnumerable<MethodBase> GetMethodsChecked() {
      yield return TargetMethodInfo;
    }

    public static readonly byte[][] Hashes = {
      new byte[] {
        // e1.1.0.225664
        0x41, 0xDD, 0x60, 0x12, 0x52, 0xAC, 0x6C, 0xA7,
        0x8A, 0xB4, 0x95, 0x75, 0xA5, 0x51, 0x90, 0x95,
        0x66, 0x1D, 0x76, 0x3F, 0xB1, 0xC5, 0x73, 0x74,
        0xDE, 0x3B, 0xD3, 0xB2, 0xF6, 0x30, 0x4E, 0xC5
      },
      new byte[] {
        // e1.1.0.224785
        0x75, 0x7C, 0x73, 0x06, 0xD6, 0xBB, 0xF3, 0xFC,
        0xA6, 0x65, 0xEF, 0x79, 0xDA, 0x11, 0x04, 0x75,
        0x23, 0x28, 0xBD, 0x4E, 0xC5, 0x95, 0x0F, 0x5E,
        0x71, 0xD6, 0x8C, 0x75, 0xC4, 0xDF, 0x52, 0x5F
      },
      new byte[] {
        // e1.0.6
        0x5A, 0xFE, 0xC3, 0xB1, 0x6A, 0xFC, 0xE0, 0xE0,
        0x43, 0x83, 0x5B, 0x73, 0x2F, 0x7D, 0x29, 0x9F,
        0x63, 0xAE, 0xD2, 0xC1, 0x6B, 0xE0, 0x0F, 0x32,
        0x38, 0x4E, 0x81, 0x18, 0xE2, 0xF3, 0x61, 0x18
      },
      new byte[] {
        // e1.4.1.229326
        0x2A, 0x25, 0x16, 0xFA, 0x67, 0xCB, 0x5E, 0xBB,
        0x9E, 0x32, 0x1E, 0x19, 0x72, 0xBB, 0x42, 0xE1,
        0xEC, 0x1D, 0x23, 0xA6, 0x4E, 0x2B, 0xAA, 0x36,
        0x1B, 0xC6, 0x64, 0x00, 0x01, 0x75, 0x90, 0xB1
      }
    };

    public SwordsAsTributePatch() : base("7fHHThQr") {
    }

    public override void Apply(Game game) {
      if (Applied) return;

      CommunityPatchSubModule.Harmony.Patch(TargetMethodInfo,
        postfix: new HarmonyMethod(PatchMethodInfo));
      Applied = true;
    }

    public override bool? IsApplicable(Game game) {
      var patchInfo = Harmony.GetPatchInfo(TargetMethodInfo);
      if (AlreadyPatchedByOthers(patchInfo))
        return false;

      var hash = TargetMethodInfo.MakeCilSignatureSha256();
      if (!hash.MatchesAnySha256(Hashes))
        return false;

      return base.IsApplicable(game);
    }

    // ReSharper disable once InconsistentNaming

    private static void Postfix(ref int __result, MobileParty party, StatExplainer explanation) {
      var perk = ActivePatch.Perk;
      var hero = party.LeaderHero;

      if (hero == null || hero.Clan?.Kingdom?.RulingClan?.Leader != hero)
        return;

      if (!hero.GetPerkValue(perk))
        return;

      var kingdomClans = hero.Clan?.Kingdom?.Clans;
      if (kingdomClans == null)
        return;

      var extra = (int) Math.Max(0, (kingdomClans.Count() - 1) * perk.PrimaryBonus);
      if (extra <= 0)
        return;

      var explainedNumber = new ExplainedNumber(__result, explanation);
      var baseLine = explanation?.Lines.Find(x => x.Name == "Base");
      if (baseLine != null)
        explanation.Lines.Remove(baseLine);

      explainedNumber.Add(extra, perk.Name);
      __result = (int) explainedNumber.ResultNumber;
    }

  }

}