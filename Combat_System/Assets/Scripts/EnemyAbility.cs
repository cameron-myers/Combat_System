/*******************************************************************************
Author:    Benjamin Ellinger
DP Email:  bellinge@digipen.edu
Date:      2/1/2022
Course:    DES 212

Description:
	This file is part of the framework for the 1D Combat Simulator assignment in
	DES 212 (System Design Methods). It can be freely used and modified by students
	for that assignment.
	
    This component makes a game object an enemy ability. The game object then must
    be parented to the actual enemy game object in order to work. Should this really
    be a different class than the hero ability? It doesn duplicate some functionality,
    but often hero and enemy abilities end up subtly different, so this can be okay to do.
	
*******************************************************************************/

//Standard Unity component libraries
using System.Collections; //Not needed in this file, but here just in case.
using System.Collections.Generic; //Not needed in this file, but here just in case.
using UnityEngine; //The library that lets your access all of the Unity functionality.
using UnityEngine.UI; //This is here so we don't have to type out longer names for UI components.

//Inherits from MonoBehavior like all normal Unity components do...
//Remember that the class name MUST be identical to the file name!
public class EnemyAbility : MonoBehaviour
{

    public enum Effect
    {
        StunSelf,
        StunOther,
        DamageSelf,
        DamageTarget,
    }

    [HideInInspector]
    public float DamageEnemy = 1.0f;
    [HideInInspector]
    public float DamageSelf = 1.0f;
    [HideInInspector]
    public float StunSelf = 1.0f;
    [HideInInspector]
    public float StunEnemy = 1.0f;


    [SerializeField] public List<Effect> EffectsList = new List<Effect>();

    //Properties that define the ability's cooldown time, damage done, range, etc.
    public float CooldownTime = 1.0f;
    public float MaximumRange = 10.0f;
    public bool Inactive = false; //Make an ability inactive to temporarily or permanently not have it used.

    [HideInInspector]
    public float CooldownLeft = 0.0f; //How much of the cooldown time is actually left.

    [HideInInspector]
    public BarScaler CooldownBar; //Reference to the cooldown timer bar, so we don't have to look it up all the time.

    [HideInInspector]
    public Enemy ParentEnemy; //Reference to the parent enemy, so we don't have to look it up all the time.

    //Start is called before the first frame update
    void Start()
    {
        //Get the parent.
        ParentEnemy = transform.parent.GetComponent<Enemy>();
        //Find the cooldown timer gameobject, which must be a child of this object.
        CooldownBar = transform.Find("Cooldown").GetComponent<BarScaler>();
    }

    //Update is called once per frame
    void Update()
    {
        //Don't let the cooldown amount left go below zero.
        CooldownLeft = Mathf.Clamp(CooldownLeft - SimControl.DT, 0.0f, CooldownTime);
        //clamp stun

        //Since cooldowns update every frame, no need to worry about interpolating over time.
        if (Inactive || CooldownTime == 0.0f) //Either doesn't have a cooldown or is inactive, so scale it to nothing.
            CooldownBar.InterpolateToScale(0.0f, 0.0f);
        else
            CooldownBar.InterpolateToScale(CooldownLeft / CooldownTime, 0.0f);

    }

    //Don't let a cooldown affect the next fight
    public void ResetCooldown()
    {
        CooldownLeft = CooldownTime;
    }
    //reset stun

    //Get the distance to the target along the X axis (1D not 2D).
    public float DistanceToTarget()
    {
        return Mathf.Abs(ParentEnemy.transform.position.x - ParentEnemy.Target.transform.position.x);
    }

    //Is an ability ready for use?
    public bool IsReady()
    {
        //It's inactive.
        if (Inactive)
            return false;
        //I'm dead.
        if (ParentEnemy.HitPoints == 0.0f)
            return false;
        //No target.
        if (ParentEnemy.Target == null)
            return false;
        //Dead target.
        if (ParentEnemy.Target.HitPoints == 0.0f)
            return false;
        //Target too far away.
        if (DistanceToTarget() > MaximumRange)
            return false;
        //Still on cooldown.
        if (CooldownLeft > 0.0f)
            return false;
        if (ParentEnemy.StunLeft > 0.0f)
            return false;
        //Ready to go.
        return true;
    }

    //Use the ability if it is ready.
    public bool Use()
    {
        //Is it ready?
        if (IsReady() == false)
            return false;

        /*//Apply the damage (or healing is the damage is negative).
        if (ParentEnemy.Target.TakeDamage(DamageDone) == true)
            ParentEnemy.Target = null; //If the target is dead, set it to null.*/

        //TODO: Add needed flags or other functionality for abilities that don't just do
        //damage or affect more than one target (AoE, heals, dodges, blocks, stuns, etc.)

        foreach (Effect fx in EffectsList)
        {
            DoEffect(fx);
        }


        //Put the ability on cooldown.
        CooldownLeft = CooldownTime;
        return true;
    }

    public void DoEffect(Effect fx)
    {
        //do each effect based on the enum passed in
        if (fx == Effect.DamageTarget)
        {
            if (ParentEnemy.Target.TakeDamage(DamageEnemy) == true)
                ParentEnemy.Target = null; //If the target is dead, find a new one.

        }
        else if (fx == Effect.DamageSelf)
        {
            if (ParentEnemy.TakeDamage(DamageSelf) == true)
                ParentEnemy.Target.Target = null; //If the parent is dead, set enemies target to null

        }
        else if (fx == Effect.StunSelf)
        {
            ParentEnemy.Stun(StunSelf);
        }
        else if (fx == Effect.StunOther)
        {
            ParentEnemy.Target.Stun(StunEnemy);
        }

    }

}
