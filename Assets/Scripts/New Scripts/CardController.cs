﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;

public class CardController : Singleton<CardController>
{
    // Properties + Component References
    #region
    [Header("Component References")]
    public Transform discardPilePosition;
    public Transform drawPilePosition;
    #endregion

    // Build Cards, Decks, View Models and Data
    #region
    public void BuildDefenderDeckFromDeckData(CharacterEntityModel defender, List<CardDataSO> deckData)
    {
        Debug.Log("CardController.BuildDefenderDeckFromDeckData() called...");

        // Convert each cardDataSO into a card object
        foreach (CardDataSO cardData in deckData)
        {
            AddCardToDrawPile(defender, BuildCardFromCardData(cardData, defender));
        }

        // Shuffle the characters draw pile
        ShuffleCards(defender.drawPile);
    }
    public Card BuildCardFromCardData(CardDataSO data, CharacterEntityModel owner)
    {
        Debug.Log("CardController.BuildCardFromCardData() called...");

        Card card = new Card();

        card.cardName = data.cardName;
        card.cardDescription = data.cardDescription;
        card.cardBaseEnergyCost = data.cardEnergyCost;
        card.cardCurrentEnergyCost = data.cardEnergyCost;
        card.cardSprite = data.cardSprite;
        card.cardType = data.cardType;
        card.targettingType = data.targettingType;
        card.talentSchool = data.talentSchool;
        card.owner = owner;
        card.cardEffects.AddRange(data.cardEffects);

        return card;
    }    
    public CardViewModel BuildCardViewModelFromCard(Card card, Vector3 position)
    {
        Debug.Log("CardController.BuildCardViewModelFromCard() called...");

        CardViewModel cardVM = null;
        if(card.targettingType == TargettingType.NoTarget)
        {
            cardVM = Instantiate(PrefabHolder.Instance.noTargetCard, position, Quaternion.identity).GetComponent<CardViewModel>();
        }
        else
        {
            cardVM = Instantiate(PrefabHolder.Instance.targetCard, position, Quaternion.identity).GetComponent<CardViewModel>();
        }       

        // Cache references
        ConnectCardWithCardViewModel(card, cardVM);

        // Set texts and images
        cardVM.SetNameText(card.cardName);
        cardVM.SetDescriptionText(card.cardDescription);
        cardVM.SetEnergyText(card.cardCurrentEnergyCost.ToString());
        cardVM.SetGraphicImage(card.cardSprite);
        cardVM.SetTalentSchoolImage(SpriteLibrary.Instance.GetTalentSchoolSpriteFromEnumData(card.talentSchool));
        cardVM.SetCardTypeImage(card.cardType);

        return cardVM;
    }    
    public void ConnectCardWithCardViewModel(Card card, CardViewModel cardVM)
    {
        card.cardVM = cardVM;
        cardVM.card = card;
    }
    public void DisconnectCardAndCardViewModel(Card card, CardViewModel cardVM)
    {
        if(card != null)
        {
            card.cardVM = null;
        }
        if(cardVM != null)
        {
            cardVM.card = null;
        }       
        
    }

    #endregion

    // Card draw Logic
    #region
    public void DrawACardFromDrawPile(CharacterEntityModel defender, int drawPileIndex = 0)
    {
        Debug.Log("CardController.DrawACardFromDrawPile() called...");

        // Shuffle discard pile back into draw pile if draw pile is empty
        if (IsDrawPileEmpty(defender))
        {
            MoveAllCardsFromDiscardPileToDrawPile(defender);
        }
        if (IsCardDrawValid(defender))
        {
            // Get card and remove from deck
            Card cardDrawn = defender.drawPile[drawPileIndex];
            RemoveCardFromDrawPile(defender, cardDrawn);

            // Add card to hand
            defender.hand.Add(cardDrawn);

            Debug.Log("ABOUT TO START VISUAL EVENT!");
            // Create and queue card drawn visual event
            VisualEventManager.Instance.CreateVisualEvent(() => DrawCardFromDeckVisualEvent(cardDrawn, defender), QueuePosition.Back, 0, 0.2f);
        }


    }
    public void DrawCardsOnActivationStart(CharacterEntityModel defender)
    {
        Debug.Log("CardController.DrawCardsOnActivationStart() called...");

        for (int i = 0; i < EntityLogic.GetTotalDraw(defender); i++)
        {
            DrawACardFromDrawPile(defender);
        }
    }
    #endregion

    // Card Discard + Removal Logic
    #region
    public void DiscardHandOnActivationEnd(CharacterEntityModel defender)
    {
        Debug.Log("CardController.DiscardHandOnActivationEnd() called, hand size = " + defender.hand.Count.ToString());

        List<Card> cardsToDiscard = new List<Card>();
        cardsToDiscard.AddRange(defender.hand);

        foreach(Card card in cardsToDiscard)
        {
            DiscardCardFromHand(defender, card);
        }
    }
    public void DiscardCardFromHand(CharacterEntityModel defender, Card card)
    {
        Debug.Log("CardController.DiscardCardFromHand() called...");

        // Get handle to the card VM
        CardViewModel cvm = card.cardVM;

        // remove from hand
        defender.hand.Remove(card);

        // place on top of discard pile
        AddCardToDiscardPile(defender, card);

        // does the card have a cardVM linked to it?
        if (cvm)
        {
            VisualEventManager.Instance.CreateVisualEvent(() => DiscardCardFromHandVisualEvent(cvm, defender), 0, 0.1f);
        }                         

    }   
    public void DestroyCardViewModel(CardViewModel cvm)
    {
        Debug.Log("CardController.DestroyCardViewModel() called...");

        // Destoy script + GO
        Destroy(cvm.gameObject);
    }

    #endregion

    // Conditional Checks
    #region
    public bool IsCardDrawValid(CharacterEntityModel defender)
    {
        if(IsDrawPileEmpty(defender))
        {
            return false;
        }
        else if(IsHandFull(defender))
        {
            return false;
        }
        else
        {
            return true;
        }
    }
    public bool IsCardPlayable(Card card, CharacterEntityModel owner)
    {
        Debug.Log("CardController.IsCardPlayable() called, checking if '" +
            card.cardName + "' is playable by '" + owner.myName +"'");

        bool boolReturned = false;

        if(HasEnoughEnergyToPlayCard(card, owner))// &&
           //ActivationManager.Instance.IsEntityActivated(owner))

           // TO DO: here we check for specifics on card type 
           // (e.g. M attack cards not playable when disarmed)
        {
            boolReturned = true;
        }

        if (boolReturned == true)
        {
            Debug.Log("CardController.IsCardPlayable() detected that '" +
            card.cardName + "' is playable by '" + owner.myName + "', returning true...");
        }
        else
        {
            Debug.Log("CardController.IsCardPlayable() detected that '" +
            card.cardName + "' is NOT playable by '" + owner.myName + "', returning false...");
        }     

        return boolReturned;
    }
    public bool HasEnoughEnergyToPlayCard(Card card, CharacterEntityModel owner)
    {
        Debug.Log("CardController.HasEnoughEnergyToPlayCard(), checking '" +
            card.cardName +"' owned by '" + owner.myName +"'");
        return card.cardCurrentEnergyCost <= owner.energy;
    }
    public bool IsDrawPileEmpty(CharacterEntityModel character)
    {
        return character.drawPile.Count == 0;
    }
    public bool IsHandFull(CharacterEntityModel character)
    {
        return character.hand.Count >= 10;
    }
    #endregion

    // Hand Visual Logic
    #region
    public void MoveCardVmFromDeckToHand(CardViewModel cardVM, Defender defender)
    {
        OldCoroutineData action = new OldCoroutineData(true);
        StartCoroutine(MoveCardVmFromDeckToHandCoroutine(cardVM, defender, action));
    }
    private IEnumerator MoveCardVmFromDeckToHandCoroutine(CardViewModel cardVM, Defender defender, OldCoroutineData action)
    {
        bool tweenFinished = false;
        // Update slot positions
        AddCardVmToDefenderHandVisual(cardVM.gameObject, defender);

        // Bring card to front while it travels from draw spot to hand
        CardLocationTracker w = cardVM.GetComponent<CardLocationTracker>();
        w.BringToFront();
        w.Slot = 0;
        w.VisualState = VisualStates.Transition;

        // Declare new dotween sequence
        Sequence s = DOTween.Sequence();
        
        // Displace the card so that we can select it in the scene easier.
        s.Append(cardVM.transform.DOLocalMove(defender.handVisual.slots.Children[0].transform.localPosition, GlobalSettings.Instance.CardTransitionTimeFast));
        s.Insert(0f, cardVM.transform.DORotate(Vector3.zero, GlobalSettings.Instance.CardTransitionTimeFast));

        // Resolve on anim event finished events
        s.OnComplete(() => tweenFinished = true);
        s.OnComplete(() => defender.handVisual.ChangeLastCardStatusToInHand(w));

        // Yield until anim sequence is finished
        yield return new WaitUntil(() => tweenFinished == true);

        // Resolve event
        action.coroutineCompleted = true;
    }
    public void AddCardVmToDefenderHandVisual(GameObject card, Defender defender)
    {
        // we always insert a new card as 0th element in CardsInHand List 
        defender.handVisual.CardsInHand.Insert(0, card);

        // parent this card to our Slots GameObject
        card.transform.SetParent(defender.handVisual.slots.transform);

        // re-calculate the position of the hand
        defender.handVisual.PlaceCardsOnNewSlots();
        defender.handVisual.UpdatePlacementOfSlots();
    }
    #endregion

    // Playing Cards Logic
    #region
    public void OnCardPlayedStart(Card card)
    {
        // Setup
        CharacterEntityModel owner = card.owner;

        // Pay Energy Cost
        CharacterEntityController.Instance.ModifyEnergy(owner, -card.cardCurrentEnergyCost);

        // Remove from hand
        owner.hand.Remove(card);

        // check for specific on card play effects 
        // Infuriated 
        if(card.cardType == CardType.Skill)
        {
            foreach (Enemy enemy in EnemyManager.Instance.allEnemies)
            {
                if (enemy.myPassiveManager.infuriated)
                {
                    StatusController.Instance.ApplyStatusToLivingEntity(enemy, StatusIconLibrary.Instance.GetStatusIconByName("Bonus Strength"), enemy.myPassiveManager.infuriatedStacks);
                }
            }
        }
       

        // TO DO: Add to discard pile, or exhaust pile?

        // Add to discard pile
        AddCardToDiscardPile(owner, card);
    }
    public void OnCardPlayedFinish(Card card)
    {
        // called at the very end of card play
    }
    public void PlayCardFromHand(Card card, CharacterEntityModel target = null)
    {
        Debug.Log("CardController.PlayCardFromHand() called, playing: " + card.cardName);

        // Setup
        CharacterEntityModel owner = card.owner;
        CardViewModel cardVM = card.cardVM;

        // Pay energy cost, remove from hand, etc
        OnCardPlayedStart(card);

        // Remove references between card and its view
        DisconnectCardAndCardViewModel(card, cardVM);

        // Create visual event and enqueue
        VisualEventManager.Instance.CreateVisualEvent(()=> PlayACardFromHandVisualEvent(cardVM, owner.characterEntityView));

        // Trigger all effects on card
        foreach (CardEffect effect in card.cardEffects)
        {
            TriggerEffectFromCard(card, effect, target);
        }

        // On end events
        OnCardPlayedFinish(card);
       
    }
    private void TriggerEffectFromCard(Card card, CardEffect cardEffect, CharacterEntityModel target)
    {
        // Stop and return if target of effect is dying        
        if(target != null && target.livingState == LivingState.Dead)
        {
            Debug.Log("CardController.TriggerEffectFromCardCoroutine() cancelling: target is dying");
            return;
        }        

        Debug.Log("CardController.PlayCardFromHand() called, effect: '" + cardEffect.cardEffectType.ToString() + 
        "' from card: '" + card.cardName);

        CharacterEntityModel owner = card.owner;
        bool hasMovedOffStartingNode = false;

        // Gain Block
        if (cardEffect.cardEffectType == CardEffectType.GainBlock)
        {
            if(target == null)
            {
                target = owner;
            }

            CharacterEntityController.Instance.ModifyBlock(target, CombatLogic.Instance.CalculateBlockGainedByEffect(cardEffect.blockGainValue, owner));
        }

        // Deal Damage
        else if (cardEffect.cardEffectType == CardEffectType.DealDamage)
        {
            // Attack animation stuff
            if(card.cardType == CardType.MeleeAttack && target != null)
            {
                hasMovedOffStartingNode = true;

                // Move towards target visual event
                CoroutineData cData = new CoroutineData();
                VisualEventManager.Instance.CreateVisualEvent(() => MovementLogic.Instance.MoveAttackerToTargetNodeAttackPosition(owner, target, cData), cData);
               
                // Animation visual event
                VisualEventManager.Instance.CreateVisualEvent(() => CharacterEntityController.Instance.TriggerMeleeAttackAnimation(owner.characterEntityView));
              
            }

            // Calculate damage
            string damageType = CombatLogic.Instance.CalculateFinalDamageTypeOfAttack(owner, cardEffect, card);
            int finalDamageValue = CombatLogic.Instance.GetFinalDamageValueAfterAllCalculations(owner, target, damageType, false, cardEffect.baseDamageValue, card, cardEffect);

            // Start damage sequence
            CombatLogic.Instance.HandleDamage(finalDamageValue, owner, target, damageType);

            // Move back to starting node pos, if we moved off 
            if (hasMovedOffStartingNode && owner.livingState == LivingState.Alive) 
            {
                CoroutineData cData = new CoroutineData();
                VisualEventManager.Instance.CreateVisualEvent(() => MovementLogic.Instance.MoveEntityToNodeCentre(owner, owner.levelNode, cData), cData, QueuePosition.Back, 0, 0);
            }

        }

        // Lose Health
        else if (cardEffect.cardEffectType == CardEffectType.LoseHealth)
        {
            // VFX
            VisualEffectManager.Instance.CreateBloodSplatterEffect(owner.characterEntityView.transform.position);

            // Start self damage sequence
            CombatLogic.Instance.HandleDamage(cardEffect.healthLost, owner, owner, "None");
        }

        // Gain Energy
        else if (cardEffect.cardEffectType == CardEffectType.GainEnergy)
        {
            // Gain Energy
            CharacterEntityController.Instance.ModifyEnergy(owner, cardEffect.energyGained);
            VisualEventManager.Instance.CreateVisualEvent(() => VisualEffectManager.Instance.CreateGainEnergyBuffEffect2(owner.characterEntityView.transform.position));
        }

        // Draw Cards
        else if (cardEffect.cardEffectType == CardEffectType.DrawCards)
        {
            // Determine target
            if (target == null)
            {
                target = owner;
            }

            // Draw cards
            for(int draws = 0; draws < cardEffect.cardsDrawn; draws++)
            {
                DrawACardFromDrawPile(target);
            }           
        }

        // Apply Burning
        else if (cardEffect.cardEffectType == CardEffectType.ApplyBurning)
        {
            //StatusController.Instance.ApplyStatusToLivingEntity(target, StatusIconLibrary.Instance.GetStatusIconByName("Burning"), cardEffect.burningApplied);
        }
    }
    #endregion

    // Deck + Discard Pile Functions
    #region
    public void ShuffleCards(List<Card> cards)
    {
        System.Random rng = new System.Random();

        int n = cards.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            Card value = cards[k];
            cards[k] = cards[n];
            cards[n] = value;
        }
    }
    public void MoveAllCardsFromDiscardPileToDrawPile(CharacterEntityModel defender)
    {
        Debug.Log("CardController.MoveAllCardsFromDiscardPileToDrawPile() called for character: " + defender.myName);

        // Create temp list for safe iteration
        List<Card> tempDiscardList = new List<Card>();
        tempDiscardList.AddRange(defender.discardPile);

        // Remove each card from discard pile, then add to draw pile
        foreach (Card card in tempDiscardList)
        {
            RemoveCardFromDiscardPile(defender,card);
            AddCardToDrawPile(defender,card);
        }

        // Re-shuffle the draw pile
        ShuffleCards(defender.drawPile);

    }
    public void AddCardToDrawPile(CharacterEntityModel defender, Card card)
    {
        defender.drawPile.Add(card);
    }
    public void RemoveCardFromDrawPile(CharacterEntityModel defender, Card card)
    {
        defender.drawPile.Remove(card);
    }
    public void AddCardToDiscardPile(CharacterEntityModel defender, Card card)
    {
        defender.discardPile.Add(card);
    }
    public void RemoveCardFromDiscardPile(CharacterEntityModel defender, Card card)
    {
        defender.discardPile.Remove(card);
    }
    #endregion

    // Visual Events
    #region
    private void DrawCardFromDeckVisualEvent(Card card, CharacterEntityModel character)
    {
        Debug.Log("CardController.DrawCardFromDeckVisualEvent() called...");
        CharacterEntityView characterView = character.characterEntityView;

        GameObject cardVM;        
        cardVM = BuildCardViewModelFromCard(card, characterView.handVisual.DeckTransform.position).gameObject;

        // pass this card to HandVisual class
        characterView.handVisual.AddCard(cardVM);

        // Bring card to front while it travels from draw spot to hand
        CardLocationTracker w = cardVM.GetComponent<CardLocationTracker>();
        w.BringToFront();
        w.Slot = 0;
        w.VisualState = VisualStates.Transition;

        // move card to the hand;
        Sequence s = DOTween.Sequence();

        // displace the card so that we can select it in the scene easier.
        s.Append(cardVM.transform.DOLocalMove(characterView.handVisual.slots.Children[0].transform.localPosition, GlobalSettings.Instance.CardTransitionTimeFast));

        s.OnComplete(() => w.SetHandSortingOrder());
    }
    private void DiscardCardFromHandVisualEvent(CardViewModel cvm, CharacterEntityModel character)
    {
        // remove from hand visual
        character.characterEntityView.handVisual.RemoveCard(cvm.gameObject);

        // move card to the discard pile
        Sequence s = MoveCardVmFromHandToDiscardPile(cvm, character.characterEntityView.handVisual.DiscardPileTransform);
        //s.Append(cvm.transform.DOMove(character.characterEntityView.handVisual.DiscardPileTransform, 0.5f));

        // Once the anim is finished, destroy the CVM 
        s.OnComplete(() => DestroyCardViewModel(cvm));
    }
    private Sequence MoveCardVmFromHandToDiscardPile(CardViewModel cvm, Transform discardPileLocation)
    {
        Debug.Log("CardController.MoveCardVmFromHandToDiscardPile() called...");

        // move card to the hand;
        Sequence s = DOTween.Sequence();
        // displace the card so that we can select it in the scene easier.
        s.Append(cvm.transform.DOMove(discardPileLocation.position, 0.5f));

        return s;
    }
    private void PlayACardFromHandVisualEvent(CardViewModel cvm, CharacterEntityView view)
    {
        Debug.Log("CardController.PlayACardFromHandVisualEvent() called...");

        cvm.locationTracker.VisualState = VisualStates.Transition;
        view.handVisual.RemoveCard(cvm.gameObject);

        cvm.transform.SetParent(null);

        Sequence seqOne = DOTween.Sequence();
        seqOne.Append(cvm.transform.DOMove(view.handVisual.DiscardPileTransform.position, 0.5f));
        seqOne.OnComplete(() =>
        {
            DestroyCardViewModel(cvm);
        });
    }

    #endregion

}
