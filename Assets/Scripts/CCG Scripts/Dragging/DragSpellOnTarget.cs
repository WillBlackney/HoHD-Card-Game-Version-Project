﻿using UnityEngine;
using System.Collections;
using DG.Tweening;

public class DragSpellOnTarget : DraggingActions {

    [Header("Properties")]
    public TargetingOptions Targets = TargetingOptions.AllCharacters;
    private VisualStates tempVisualState;

    [Header("Component References")]
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private LineRenderer lr;
    
    
    [SerializeField] private SpriteRenderer triangleSR;    

    public override bool CanDrag
    {
        get
        {
            Debug.Log("DragSpellOnTarget.CanDrag() called...");
            return (base.CanDrag);
        }
    }

    public override void OnStartDrag()
    {
        Debug.Log("DragSpellOnTarget.OnStartDrag() called...");
        tempVisualState = locationTracker.VisualState;
        locationTracker.VisualState = VisualStates.Dragging;
        sr.enabled = true;
        lr.enabled = true;
    }

    public override void OnDraggingInUpdate()
    {
        Vector3 notNormalized = transform.position - transform.parent.position;
        Vector3 direction = notNormalized.normalized;
        float distanceToTarget = (direction*2.3f).magnitude;
        if (notNormalized.magnitude > distanceToTarget)
        {
            // draw a line between the creature and the target
            lr.SetPositions(new Vector3[]{ transform.parent.position, transform.position - direction*2.3f });
            lr.enabled = true;

            // position the end of the arrow between near the target.
            triangleSR.enabled = true;
            triangleSR.transform.position = transform.position - 1.5f*direction;

            // proper rotarion of arrow end
            float rot_z = Mathf.Atan2(notNormalized.y, notNormalized.x) * Mathf.Rad2Deg;
            triangleSR.transform.rotation = Quaternion.Euler(0f, 0f, rot_z - 90);
        }
        else
        {
            // if the target is not far enough from creature, do not show the arrow
            lr.enabled = false;
            triangleSR.enabled = false;
        }

    }
    public override void OnEndDrag()
    {
        Debug.Log("DragSpellOnTarget.OnEndDrag() called...");

        // Set up
        CharacterEntityModel target = null;
        CharacterEntityModel owner = cardVM.owner();
        Card card = cardVM.card;

        // Raycast from cam to mouse
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits;
        hits = Physics.RaycastAll(Camera.main.ScreenPointToRay(Input.mousePosition), 1000.0f);

        // Get Character views from raycast hits
        foreach (RaycastHit h in hits)
        {
            Debug.Log("Ray cast hit object called: " + h.transform.gameObject.name);
            if (h.transform.gameObject.GetComponent<CharacterEntityView>())
            {              
                target = h.transform.gameObject.GetComponent<CharacterEntityView>().character;
                Debug.Log("Hit a living entity called: " + target.myName);
            }
        }

        Debug.Log("Total targets hit with raycast = " + hits.Length.ToString());
        
        // Check for target validity
        bool targetValid = false;
        if (target != null)
        {     

            if(card.targettingType == TargettingType.AllCharacters)
            {
                targetValid = true;
            }
            else if(card.targettingType == TargettingType.Ally &&
                    target.allegiance == Allegiance.Player &&
                    target != owner)
            {
                targetValid = true;
            }
            else if(card.targettingType == TargettingType.AllyOrSelf &&
                    target.allegiance == Allegiance.Player)
            {
                targetValid = true;
            }
            else if(card.targettingType == TargettingType.Enemy &&
                    target.allegiance == Allegiance.Enemy)
            {
                targetValid = true;
            }
        }

        // Did we hit a valid target?
        if (!targetValid)
        {
            // not a valid target, return
            locationTracker.VisualState = tempVisualState;
            locationTracker.SetHandSortingOrder();
        }
        else
        {
            // Valid target, play the card against the target
            CardController.Instance.PlayCardFromHand(card, target);
        }

        // return target and arrow to original position
        // this position is special for spell cards to show the arrow on top
        transform.localPosition = new Vector3(0f, 0f, -0.1f);
        sr.enabled = false;
        lr.enabled = false;
        triangleSR.enabled = false;

    }
   
    // NOT USED IN THIS SCRIPT
    protected override bool DragSuccessful()
    {
        return true;
    }
}