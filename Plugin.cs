using BepInEx;
using BepInEx.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.IO;
using DiskCardGame;
using HarmonyLib;
using UnityEngine;
using Pixelplacement;

namespace ActivatedLatch
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        private const string PluginGuid = "AnthonyPython.inscryption.AnthonyActivatedLatch";
        private const string PluginName = "AnthonysActivatedLatch";
        private const string PluginVersion = "1.0.0.0";

        public static GameObject clawPrefab;

        private void Awake()
        {
            Logger.LogInfo($"Loaded {PluginName}!");

            clawPrefab = ResourceBank.Get<GameObject>("Prefabs/Cards/SpecificCardModels/LatchClaw");


        }

        public static void AimWeaponAnim(GameObject TweenObj, Vector3 target)
        {
            Tween.LookAt(TweenObj.transform, target, Vector3.up, 0.075f, 0f, Tween.EaseInOut, Tween.LoopType.None, null, null, true);
        }


        public abstract class ActivatedLatch : ActivatedAbilityBehaviour
        {
            public override Ability Ability
            {
                get
                {
                    return ability;
                }
            }

            public static Ability ability;

            protected abstract Ability LatchAbility { get; }
            public override IEnumerator Activate()
            {
                bool flag = SceneLoader.ActiveSceneName == "Part1_Cabin";
                if (flag)
                {

                    List<CardSlot> validTargets = Singleton<BoardManager>.Instance.AllSlotsCopy;
                    validTargets.RemoveAll((CardSlot x) => x.Card == null || x.Card.Dead || CardHasLatchMod(x.Card) || x.Card == Card);



                    if (validTargets.Count <= 0)
                    {
                        yield break;
                    }


                    Singleton<ViewManager>.Instance.SwitchToView(View.Board);
                    Card.Anim.PlayHitAnimation();
                    yield return new WaitForSeconds(0.1f);


                    CardAnimationController cardAnim = Card.Anim as CardAnimationController;


                    GameObject LatchPreParent = new GameObject();

                    LatchPreParent.name = "LatchParent";
                    LatchPreParent.transform.position = cardAnim.transform.position;
                    LatchPreParent.gameObject.transform.parent = cardAnim.transform;

                    Transform Latchparent = LatchPreParent.transform;
                    GameObject claw = UnityEngine.Object.Instantiate(Plugin.clawPrefab, Latchparent);


                    CardSlot selectedSlot = null;




                    if (Card.OpponentCard)
                    {
                        yield return new WaitForSeconds(0.3f);
                        yield return AISelectTarget(validTargets, delegate (CardSlot s)
                        {
                            selectedSlot = s;
                        });


                        if (selectedSlot != null && selectedSlot.Card != null)
                        {

                            Plugin.AimWeaponAnim(Latchparent.gameObject, selectedSlot.transform.position);
                            yield return new WaitForSeconds(0.3f);
                        }
                    }
                    else
                    {
                        List<CardSlot> allSlotsCopy = Singleton<BoardManager>.Instance.AllSlotsCopy;
                        allSlotsCopy.Remove(Card.Slot);
                        yield return Singleton<BoardManager>.Instance.ChooseTarget(allSlotsCopy, validTargets, delegate (CardSlot s)
                        {
                            selectedSlot = s;
                        }, OnInvalidTarget, delegate (CardSlot s)
                        {
                            if (s.Card != null)
                            {

                                Plugin.AimWeaponAnim(Latchparent.gameObject, s.transform.position);
                            }
                        }, null, CursorType.Target);
                    }

                    claw.SetActive(true);
                    CustomCoroutine.FlickerSequence(delegate
                    {
                        claw.SetActive(true);
                    }, delegate
                    {
                        claw.SetActive(false);
                    }, startOn: true, endOn: false, 0.05f, 2);


                    if (selectedSlot != null && selectedSlot.Card != null)
                    {
                        CardModificationInfo cardModificationInfo = new CardModificationInfo(LatchAbility);


                        cardModificationInfo.fromCardMerge = true;



                        cardModificationInfo.fromLatch = true;


                        if (selectedSlot.Card.Info.name == "!DEATHCARD_BASE")
                        {
                            selectedSlot.Card.AddTemporaryMod(cardModificationInfo);
                        }
                        else
                        {

                            CardInfo targetCardInfo = selectedSlot.Card.Info.Clone() as CardInfo;

                            targetCardInfo.Mods.Add(cardModificationInfo);

                            selectedSlot.Card.SetInfo(targetCardInfo);
                        }



                        selectedSlot.Card.Anim.PlayTransformAnimation();




                        OnSuccessfullyLatched(selectedSlot.Card);
                        yield return new WaitForSeconds(0.75f);
                        yield return LearnAbility();
                    }
                }
                else
                {
                    List<CardSlot> validTargets = Singleton<BoardManager>.Instance.AllSlotsCopy;
                    validTargets.RemoveAll((CardSlot x) => x.Card == null || x.Card.Dead || CardHasLatchMod(x.Card) || x.Card == Card);
                    if (validTargets.Count <= 0)
                    {
                        yield break;
                    }
                    Singleton<ViewManager>.Instance.SwitchToView(View.Board);
                    Card.Anim.PlayHitAnimation();
                    yield return new WaitForSeconds(0.1f);
                    DiskCardAnimationController cardAnim = Card.Anim as DiskCardAnimationController;
                    GameObject claw = UnityEngine.Object.Instantiate(clawPrefab, cardAnim.WeaponParent.transform);
                    CardSlot selectedSlot = null;
                    if (Card.OpponentCard)
                    {
                        yield return new WaitForSeconds(0.3f);
                        yield return AISelectTarget(validTargets, delegate (CardSlot s)
                        {
                            selectedSlot = s;
                        });
                        if (selectedSlot != null && selectedSlot.Card != null)
                        {
                            cardAnim.AimWeaponAnim(selectedSlot.transform.position);
                            yield return new WaitForSeconds(0.3f);
                        }
                    }
                    else
                    {
                        List<CardSlot> allSlotsCopy = Singleton<BoardManager>.Instance.AllSlotsCopy;
                        allSlotsCopy.Remove(Card.Slot);
                        yield return Singleton<BoardManager>.Instance.ChooseTarget(allSlotsCopy, validTargets, delegate (CardSlot s)
                        {
                            selectedSlot = s;
                        }, OnInvalidTarget, delegate (CardSlot s)
                        {
                            if (s.Card != null)
                            {
                                cardAnim.AimWeaponAnim(s.transform.position);
                            }
                        }, null, CursorType.Target);
                    }
                    CustomCoroutine.FlickerSequence(delegate
                    {
                        claw.SetActive(value: true);
                    }, delegate
                    {
                        claw.SetActive(value: false);
                    }, startOn: true, endOn: false, 0.05f, 2);
                    if (selectedSlot != null && selectedSlot.Card != null)
                    {
                        CardModificationInfo cardModificationInfo = new CardModificationInfo(LatchAbility);
                        cardModificationInfo.fromLatch = true;
                        selectedSlot.Card.Anim.ShowLatchAbility();
                        selectedSlot.Card.AddTemporaryMod(cardModificationInfo);
                        OnSuccessfullyLatched(selectedSlot.Card);
                        yield return new WaitForSeconds(0.75f);
                        yield return LearnAbility();
                    }
                }
            }

            protected virtual void OnSuccessfullyLatched(PlayableCard target)
            {
            }

            private IEnumerator AISelectTarget(List<CardSlot> validTargets, Action<CardSlot> chosenCallback)
            {
                if (validTargets.Count > 0)
                {
                    bool positiveAbility = AbilitiesUtil.GetInfo(LatchAbility).PositiveEffect;
                    validTargets.Sort((CardSlot a, CardSlot b) => AIEvaluateTarget(b.Card, positiveAbility) - AIEvaluateTarget(a.Card, positiveAbility));
                    chosenCallback(validTargets[0]);
                    yield return new WaitForSeconds(0.1f);
                }
                else
                {
                    Card.Anim.LightNegationEffect();
                    yield return new WaitForSeconds(0.2f);
                }
                yield break;
            }


            private int AIEvaluateTarget(PlayableCard card, bool positiveEffect)
            {
                int num = card.PowerLevel;
                if (card.Info.HasTrait(Trait.Terrain))
                {
                    num = 10 * (positiveEffect ? -1 : 1);
                }
                if (card.OpponentCard == positiveEffect)
                {
                    num += 1000;
                }
                return num;
            }

            private void OnInvalidTarget(CardSlot slot)
            {
                if (slot.Card != null && this.CardHasLatchMod(slot.Card) && !Singleton<TextDisplayer>.Instance.Displaying)
                {
                    base.StartCoroutine(Singleton<TextDisplayer>.Instance.ShowThenClear("It's already latched...", 2.5f, 0f, Emotion.Anger, TextDisplayer.LetterAnimation.Jitter, DialogueEvent.Speaker.Single, null));
                }
            }
            private bool CardHasLatchMod(PlayableCard card)
            {
                return card.TemporaryMods.Exists((CardModificationInfo m) => m.fromLatch);
            }


        }


        public class ActivatedLatchNanoShield : ActivatedLatch
        {
            public override Ability Ability
            {
                get
                {
                    return ability;
                }
            }

            protected override int EnergyCost
            {
                get
                {
                    return 1;
                }
            }

            protected override int BonesCost
            {
                get
                {
                    return 2;
                }
            }

            protected override Ability LatchAbility
            {
                get
                {
                    return Ability.DeathShield;
                }
            }

            public static Ability ability;

            protected override void OnSuccessfullyLatched(PlayableCard target)
            {
                target.ResetShield();

                //Singleton<ViewManager>.Instance.SwitchToView(View.Board, false, true);
                //yield return new WaitForSeconds(0.15f);
                Card.SetCardbackSubmerged();
                Card.SetFaceDown(true, false);
                //yield return new WaitForSeconds(0.3f);
                // yield return base.LearnAbility(0f);
                //this.triggerPriority = int.MaxValue;
                // yield break;
            }

            public override bool RespondsToUpkeep(bool playerUpkeep)
            {
                return Card.OpponentCard != playerUpkeep;
            }

            public override IEnumerator OnUpkeep(bool playerUpkeep)
            {

                Singleton<ViewManager>.Instance.SwitchToView(View.Board, false, true);
                yield return new WaitForSeconds(0.15f);
                yield return base.PreSuccessfulTriggerSequence();
                Card.SetFaceDown(false, false);
                Card.UpdateFaceUpOnBoardEffects();
                // this.OnResurface();
                yield return new WaitForSeconds(0.3f);
                //this.triggerPriority = int.MinValue;
                yield break;
            }


        }

    }
}
