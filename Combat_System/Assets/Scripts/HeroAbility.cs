/*******************************************************************************
Author:    Benjamin Ellinger
DP Email:  bellinge@digipen.edu
Date:      2/1/2022
Course:    DES 212

Description:
	This file is part of the framework for the 1D Combat Simulator assignment in
	DES 212 (System Design Methods). It can be freely used and modified by students
	for that assignment.
	
    This component makes a game object a hero ability. The game object then must
    be parented to the actual hero game object in order to work. Should this really
    be a different class than the enemy ability? It doesn duplicate some functionality,
    but often hero and enemy abilities end up subtly different, so this can be okay to do.
	
*******************************************************************************/

//Standard Unity component libraries

using System;
using System.Collections; //Not needed in this file, but here just in case.
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.VisualScripting; //Not needed in this file, but here just in case.
using UnityEngine; //The library that lets your access all of the Unity functionality.
using UnityEngine.UI; //This is here so we don't have to type out longer names for UI components.



//Inherits from MonoBehavior like all normal Unity components do...
//Remember that the class name MUST be identical to the file name!
public class HeroAbility : MonoBehaviour
{

    public enum Effect
    {
        StunSelf=0,
        StunOther=1,
        DamageSelf=2,
        DamageTarget=3,
        cCount
    }

    //Properties that define the ability's cooldown time, damage done, power used, range, etc.
    [HideInInspector]
    public List<float> EffectValues = new List<float>() { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };

    public List<float> effectValues
    {
        get => EffectValues;
        set => EffectValues = value;
    }

    public float AOERange = 2.0f;
    /*[HideInInspector]
    public float DamageEnemy = 1.0f;
    [HideInInspector]
    public float DamageSelf = 1.0f;
    [HideInInspector]
    public float StunSelf = 1.0f;
    [HideInInspector]
    public float StunEnemy = 1.0f;*/

    [SerializeField] public List<Effect> EffectsList = new List<Effect>();
    [SerializeField] public List<Effect> AOEList = new List<Effect>();

    public float StaminaCost = 1.0f;
    public float MaximumRange = 10.0f;
    public bool Inactive = false; //Make an ability inactive to temporarily or permanently not have it used.
    

    public float CooldownTime = 1.0f;


    [HideInInspector]
    public float CooldownLeft = 0.0f; //How much of the cooldown time is actually left.

    [HideInInspector]
    public BarScaler CooldownBar; //Reference to the cooldown timer bar, so we don't have to look it up all the time.

    [HideInInspector]
    public Hero ParentHero; //Reference to the parent hero, so we don't have to look it up all the time.

    //Start is called before the first frame update
    void Start()
    {
        //Get the parent.
        ParentHero = GameObject.Find("Hero").GetComponent<Hero>();
        //Find the cooldown timer gameobject, which must be a child of this object.
        CooldownBar = transform.Find("Cooldown").GetComponent<BarScaler>();
    }

    //Update is called once per frame
    void Update()
    {
        //Don't let the cooldown amount left go below zero.
        CooldownLeft = Mathf.Clamp(CooldownLeft - SimControl.DT, 0.0f, CooldownTime);
        //Since cooldowns update every frame, no need to worry about interpolating over time.
        if (Inactive || CooldownTime == 0.0f) //Either doesn't have a cooldown or is inactive, so scale it to nothing.
            CooldownBar.InterpolateToScale(0.0f, 0.0f);
        else
            CooldownBar.InterpolateToScale(CooldownLeft / CooldownTime, 0.0f);
    }

    //Don't let a cooldown affect the next fight
    public void ResetCooldown()
    {
        CooldownLeft = 0.0f;
    }

    //Get the distance to the target along the X axis (1D not 2D).
    public float DistanceToTarget()
    {
        return Mathf.Abs(ParentHero.transform.position.x - ParentHero.Target.transform.position.x);
    }

    // Is an ability ready for use?
    public bool IsReady()
    {
        //It's inactive.
        if (Inactive)
            return false;
        //I'm dead.
        if (ParentHero.HitPoints <= 0.0f)
            return false;
        //No target.
        if (ParentHero.Target == null)
            return false;
        //Dead target.
        if (ParentHero.Target.HitPoints == 0.0f)
            return false;
        //Target too far away.
        if (DistanceToTarget() > MaximumRange)
            return false;
        //Still on cooldown.
        if (CooldownLeft > 0.0f)
            return false;
        if (ParentHero.StunLeft > 0.0f)
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
        //Use the power.
        ParentHero.UseStamina(StaminaCost);
        //Apply the damage (or healing is the damage is negative).
        //this is now done in DoEffect
        /*if (ParentHero.Target.TakeDamage(DamageDone) == true)
            ParentHero.Target = ParentHero.FindTarget(); //If the target is dead, find a new one.
            */

        //TODO: Add needed flags or other functionality for abilities that don't just do
        //damage or affect more than one target (AoE, heals, dodges, blocks, stuns, etc.)

        //for list of effects
        foreach(Effect fx in EffectsList)
        {
            //do effect
            DoEffect(fx);
        }


        //Put the ability on cooldown.
        CooldownLeft = CooldownTime;
        return true;
    }

    
    public void DoEffect(Effect fx)
    {
        //check if fx is AOE
        if (AOEList.Contains(fx))
        {
            //generate and carryout effect
            DoAOE(GenerateAOEList(ParentHero.Target,AOERange),fx, EffectValues[(int)fx]);
            return;
        }
        //no aoe
        //do each effect based on the enum passed in
        if (fx == Effect.DamageTarget)
        {

            if (ParentHero.Target.TakeDamage(EffectValues[(int)Effect.DamageTarget]) == true)
                ParentHero.Target = ParentHero.FindTarget(); //If the target is dead, find a new one.

        }
        else if (fx == Effect.DamageSelf)
        {

            if (ParentHero.TakeDamage(EffectValues[(int)Effect.DamageSelf]) == true)
            {
                ParentHero.Target.Target = null; //If the parent is dead, set enemies target to null
            }


        }
        else if (fx == Effect.StunSelf)
        {
            

            ParentHero.Stun(EffectValues[(int)Effect.StunSelf]);
        }
        else if (fx == Effect.StunOther)
        {
            
            ParentHero.Target.Stun(EffectValues[(int)Effect.StunOther]);
        }

    }

    private List<Enemy> GenerateAOEList(Enemy target, float range)
    {
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        List<Enemy> aoeEnemies = new List<Enemy>();
        aoeEnemies.Add(target);

        if(enemies.Length <= 0) return null;
        //loop through the enemies on the screen
        foreach (Enemy enemy in enemies)
        {
            //check if its in range
            if (Vector3.Distance(target.transform.position, enemy.transform.position) <= range)
            {
                //add to the list 
                aoeEnemies.Add(enemy);
            }
        }

        return aoeEnemies;
    }

    private void DoAOE(List<Enemy> aoeList, Effect effect, float effectParam)
    {
        //invoke the effect function on each enemy in the aoe list
        ParentHero.Target = ParentHero.FindTargetAOE(this);

        if (!SimControl.FastMode)
        {
            GameObject AOEsig = Instantiate(SimControl.AOESignifierPrefab, ParentHero.Target.transform.localPosition, Quaternion.Euler(0, 0, 0));
            //set range size
            AOEsig.transform.localScale *= AOERange;
            //kill
            Destroy(AOEsig, 1.0f);

        }

        switch (effect)
        {
            case Effect.DamageTarget:
            {
                foreach (Enemy enemy in aoeList)
                {
                    enemy.TakeDamage(effectParam);
                }
                break;
            }
            case Effect.DamageSelf:
            {
                ParentHero.TakeDamage(effectParam);
                break;
            }
            case Effect.StunSelf:
            { 
                ParentHero.Stun(effectParam);
                break;
            }
            case Effect.StunOther:
            {
                foreach (Enemy enemy in aoeList)
                {
                        enemy.Stun(effectParam);
                }
                break;

            }
        }

    }
    

}

