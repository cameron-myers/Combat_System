/*******************************************************************************
Author:    Benjamin Ellinger
DP Email:  bellinge@digipen.edu
Date:      2/1/2022
Course:    DES 212

Description:
	This file is part of the framework for the 1D Combat Simulator assignment in
	DES 212 (System Design Methods). It can be freely used and modified by students
	for that assignment.
	
    This component makes a game object a hero that can be controlled by the player.
    There is only a single hero that is already placed in the scene.
	
*******************************************************************************/

//Standard Unity component libraries

using System;
using System.Collections; //Not needed in this file, but here just in case.
using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using Unity.VisualScripting; //Not needed in this file, but here just in case.
using UnityEngine; //The library that lets your access all of the Unity functionality.
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random; //This is here so we don't have to type out longer names for UI components.

//Inherits from MonoBehavior like all normal Unity components do...
//Remember that the class name MUST be identical to the file name!
public class Hero : MonoBehaviour
{
    //Properties for maximum hit points, movement speed, maximum power, and optimal range.
    public float MaxHitPoints = 200;
    public float MoveSpeed = 0.1f;
    public float MaxStamina = 100;
    public float OptimalRange = 5.0f;
    public float StaminaStun = 2.0f;

    [HideInInspector]
    public float HitPoints = 200; //Current hit points
    [HideInInspector]
    public float Stamina = 100; //Current stamina
    [HideInInspector]
    public float StunLeft = 0; //Current Stun

    private float StunTime = 0.001f;


    [HideInInspector]
    public Enemy Target; //Current target enemy.

    //References to the health and power UI bars, so we don't have to look them up all the time.
    [HideInInspector]
    public BarScaler HealthBar;
    [HideInInspector]
    public BarScaler StaminaBar;

    [HideInInspector] public BarScaler StunBar; // reference to stun bar

    //References to the abilities, so we don't have to look them up all the time.
    //These are set by hand in the inspector for the hero game object.
    public HeroAbility AbilityOne;
    public HeroAbility AbilityTwo;
    public HeroAbility AbilityThree;
    public HeroAbility AbilityFour;

    //Start is called before the first frame update
    void Start()
    {
        //The static version of Find() on the GameObject class will just find the named object anywhere.
        //Use GetComponent so we don't have to use it later to access the functionality we want.
        HealthBar = transform.Find("Hero_Health").GetComponent<BarScaler>(); 
        StaminaBar = transform.Find("Hero_Stamina").GetComponent<BarScaler>();
        StunBar = transform.Find("StunBar").GetComponent<BarScaler>();
        StunBar.InterpolateImmediate(StunLeft);

    }

    //Update is called once per frame
    void Update()
    {
        if (SimControl.RoundOver) //Don't update between rounds (or when the sim is over).
            return;
        if (Target == null) //If we don't have a target, the round must have just started.
            Initialize();
        //The fight is on, so move and use abilities.
        DoMovement();
        if (SimControl.AutoMode == true) //Let an "AI" determine which abilities to use.
        {
            if (SimControl.AI_List[SimControl.CurrentAI] == SimControl.PlayerAIMode.AbilityRandom)
                UseRandomAbility();
            else if (SimControl.AI_List[SimControl.CurrentAI] == SimControl.PlayerAIMode.AbilitySmart)
                UseSmartAbility();
            else if (SimControl.AI_List[SimControl.CurrentAI] == SimControl.PlayerAIMode.AbilityOne)
                UseAbility(1);
            else if (SimControl.AI_List[SimControl.CurrentAI] == SimControl.PlayerAIMode.AbilityTwo)
                UseAbility(2);
            else if (SimControl.AI_List[SimControl.CurrentAI] == SimControl.PlayerAIMode.AbilityThree)
                UseAbility(3);
            else if (SimControl.AI_List[SimControl.CurrentAI] == SimControl.PlayerAIMode.AbilityFour)
                UseAbility(4);
        }
        else //Let the player select which abilities to use.
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) == true)
                UseAbility(1);
            if (Input.GetKeyDown(KeyCode.Alpha2) == true)
                UseAbility(2);
            if (Input.GetKeyDown(KeyCode.Alpha3) == true)
                UseAbility(3);
            if (Input.GetKeyDown(KeyCode.Alpha4) == true)
                UseAbility(4);
        }

        if (StunLeft > 0.0f)
        {
            StunLeft = Mathf.Clamp(StunLeft - SimControl.DT, -0.1f, StunTime);
        }
        //if stun is done, reset stamina
        if (StunLeft <= 0.0f && Stamina <= 0.0f)
        {
            StunLeft = -0.001f;
            Stamina = MaxStamina;
            StaminaBar.InterpolateImmediate(Stamina/MaxStamina);
        }


    }

    //Try to stay close to optimal range. Note this is done even in Auto mode.
    public void DoMovement()
    {
        if (HitPoints <= 0.0f || Target == null) //If all enemies or the player is dead, no need to move.
            return;
        
        if(StunLeft > 0.0f ) return;

        float newX = transform.position.x;

        //if not in auto mode
        if (SimControl.AutoMode == true)
        {
            //Calculate distance to target along the X axis (1D not 2D).
            float distanceToTarget = transform.position.x - Target.transform.position.x;
            //If we are between 80% and 100% of optimal range, that's good enough.
            if (Mathf.Abs(distanceToTarget) <= OptimalRange && Mathf.Abs(distanceToTarget) >= OptimalRange * 0.8f)
                return;
            //If we are too close, flip the "distance" so we will move away instead of towards.
            if (Mathf.Abs(distanceToTarget) < OptimalRange * 0.8f)
                distanceToTarget = -distanceToTarget;
            //We need to move, so get our current X position.
            if (distanceToTarget > 0) //Move to the left.
                newX -= MoveSpeed * SimControl.DT; //Make sure to use the simulated DT.
            else //Move to the right.
                newX += MoveSpeed * SimControl.DT; //Make sure to use the simulated DT.
            //Don't go past the edge of the arena.
            //Update the transform.
        }
        else
        {
            newX += Input.GetAxis("Horizontal") * MoveSpeed * Time.deltaTime;
        }
        newX = Mathf.Clamp(newX, -SimControl.EdgeDistance, SimControl.EdgeDistance);
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);

    }

    //Find the best target for the hero.
    public Enemy FindTarget()
    {
        //Find all the enemies in the scene.
        var enemies = FindObjectsOfType<Enemy>();
        if (enemies.Length == 0) //No enemies means no target.
            return null;
        //There are enemies, now find the best one.
        Enemy target = null;
        if (Target != null && Target.HitPoints > 0.0f) //Start with our current target if it is still alive.
            target = Target;
        
        //Lowest HP
        
        //Find the enemy with the lowest HP.
        float lowestHP = float.MaxValue;
        if (target) //Start with the current target so any ties don't cause target switching.
            lowestHP = target.HitPoints;
        //Loop through all the enemies to find the weakest enemy.
        foreach (Enemy enemy in enemies)
        {
            if (enemy.HitPoints > 0 && enemy.HitPoints < lowestHP)
            {
                target = enemy;
                lowestHP = enemy.HitPoints;
            }
        }
        
        /*
        //Closest Target
        float distance = Single.MaxValue;
        
        foreach(Enemy enemy in enemies)
        {
            if(Vector2.Distance(enemy.transform.localPosition, this.gameObject.transform.localPosition) < distance)
            {
                target = enemy;
            }
        }
        */
        return target;
    }


    public Enemy FindTargetAOE(HeroAbility ability)
    {
        //Find all the enemies in the scene.
        var enemies = FindObjectsOfType<Enemy>();
        if (enemies.Length == 0) //No enemies means no target.
            return null;
        //There are enemies, now find the best one.
        Enemy target = null;
        if (Target != null && Target.HitPoints > 0.0f) //Start with our current target if it is still alive.
            target = Target;

        //FIND LOWEST HP
        //look for most enemies in AOE range
        //Find the enemy with the lowest HP.
        float lowestDensity = float.MinValue;

        //Loop through all the enemies to find the weakest enemy.
        foreach (Enemy enemy in enemies)
        {
            int density = 0;
            //check the rest of the enemies
            foreach (Enemy enemysub in enemies)
            {
                //check if enemy is in abilities range
                if (Vector2.Distance(enemy.transform.localPosition, enemysub.transform.localPosition) < ability.AOERange )
                {
                    //tally density
                    ++density;
                }
            }
            //check if density was greater 
            if (density > lowestDensity)
            {
                //set target and update min density
                target = enemy;
                lowestDensity = density;
            }

        }

        return target;
    }

    //This is NOT a Start() function because we need to be able to call Initialize() whenever a new
    //round starts, not just when the object is created.
    public void Initialize()
    {
        //Set our X position to the correct starting position on the left side of the arena, while keeping the Y and Z the same.
        transform.position = new Vector3(-SimControl.StartingX, transform.position.y, transform.position.z);
        //Reset hit points.
        HitPoints = MaxHitPoints;
        Stamina = MaxStamina;
        //Reset all the cooldowns.
        if (AbilityOne != null) AbilityOne.ResetCooldown();
        if (AbilityTwo != null) AbilityTwo.ResetCooldown();
        if (AbilityThree != null) AbilityThree.ResetCooldown();
        if (AbilityFour != null) AbilityFour.ResetCooldown();
        //Find a target.
        Target = FindTarget();
        //Make sure the health stamina, and stun bars get reset.
        HealthBar.InterpolateImmediate(HitPoints / MaxHitPoints);
        StaminaBar.InterpolateImmediate(Stamina / MaxStamina);

    }

    //Try to use a random ability.
    public bool UseRandomAbility()
    {
        //Get a random number between 1 and 4. Yes, the integer version of this function is not
        //inclusive. This is wrong and Unity should feel bad for doing this.
        return UseAbility(Random.Range(1, 5));
    }
    public bool UseSmartAbility()
    {

        //prioritize ability 4,
        if (AbilityFour.IsReady())
            return UseAbility(4);
        //if distance is far prioritize 3
        else if(AbilityThree.IsReady())
            return UseAbility(3);
        //all other cases random between 2 and 1
        else if(AbilityTwo.IsReady())
            return UseAbility(2);
        else if (AbilityOne.IsReady())
            return UseAbility(1);

        return false;
    }

    //Try to use a specific ability.
    public bool UseAbility(int abilityNumber)
    {
        if (abilityNumber == 1 && AbilityOne != null)
        {
            if (AbilityOne.Use())
            {
                SimControl.abilityCountsRound[abilityNumber - 1] += 1;
                return true;
            }

            return false;
        }

        if (abilityNumber == 2 && AbilityTwo != null)
        {
            if (AbilityTwo.Use())
            {
                SimControl.abilityCountsRound[abilityNumber - 1] += 1;
                return true;
            }

            return false;
        }

        if (abilityNumber == 3 && AbilityThree != null)
        {

            if (AbilityThree.Use())
            {
                SimControl.abilityCountsRound[abilityNumber - 1] += 1;
                return true;
            }

            return false;
        }

        if (abilityNumber == 4 && AbilityFour != null)
        {
            if (AbilityFour.Use())
            {
                SimControl.abilityCountsRound[abilityNumber - 1] += 1;
                return true;
            }
            return false;
            
        }
        return false;
    }




    //Use a given amount of power.
    public void UseStamina(float stamina)
    {
        if (Stamina - stamina <= 0.0f)
        {
            //StunSelf
            Stun(StaminaStun);
        }
        //Make sure power does not go negative (or above max, becaust the "power" could be negative).
        Stamina = Mathf.Clamp(Stamina - stamina, 0.0f, MaxStamina);
        //Interpolate the power UI bar over half a second.
        StaminaBar.InterpolateToScale(Stamina / MaxStamina, 0.2f);
    }

    //Take damage from any source.
    public bool TakeDamage(float damage)
    {
        if (damage != 0.0f) //Don't bother if the damage is 0
        {
            //Make sure hit points do not go negative (or above max, because the "damage" could be negative, i.e., healing).
            HitPoints = Mathf.Clamp(HitPoints - damage, 0.0f, MaxHitPoints);
            //Interpolate the hit point UI bar over half a second.
            HealthBar.InterpolateToScale(HitPoints / MaxHitPoints, 0.2f);
            //Create a temporary InfoText object to show the damage using the static Instantiate() function.
            if (!SimControl.FastMode)
            {
                Text damageText = Object.Instantiate(SimControl.InfoTextPrefab, transform.position, Quaternion.identity, SimControl.Canvas.transform).GetComponent<Text>();
                //Set the damage text to just the integer amount of the damage done.
                //Uses the "empty string plus number" trick to make it a string.
                damageText.text = "" + Mathf.Floor(damage);
            }

        }
        //Return true if dead.
        return (HitPoints <= 0.0f);
    }

    public void Stun(float stunTime)
    {
        StunLeft = stunTime - SimControl.DT;
        StunTime = stunTime;
        StunBar.InterpolateImmediate(1.0f);
        //Interpolate the hit 0 over stun time
        StunBar.InterpolateToScale(0.0f, stunTime);
        

    }

}

    