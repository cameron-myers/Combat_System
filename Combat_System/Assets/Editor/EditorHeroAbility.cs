/*******************************************************************************
Author:    Cameron Myers
DP Email:  cameron.myers@digipen.edu
Date:      2/24/2022
Course:    DES 212

Description:
Custom Editor for Hero Ability
	
*******************************************************************************/


using System.Collections;
using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEditor;

using UnityEngine;

[CustomEditor(typeof(HeroAbility))]

public class EditorHeroAbility : Editor
{
        public override void OnInspectorGUI()
        {
            // If we call base the default inspector will get drawn too.
            // Remove this line if you don't want that to happen.
            base.OnInspectorGUI();

            HeroAbility myBehaviour = target as HeroAbility;

        //target.myBool = EditorGUILayout.Toggle("myBool", target.myBool);

            if (myBehaviour.EffectsList.Count == 0) return;


            foreach (HeroAbility.Effect effect in myBehaviour.EffectsList)
            {
                if (effect == HeroAbility.Effect.cCount) break;

                    myBehaviour.EffectValues[(int)effect] = EditorGUILayout.FloatField(effect.ToString(), myBehaviour.EffectValues[(int)effect]);
                }
            
            /*
            //this is for damage to enemy
            if (myBehaviour.EffectsList.Contains(HeroAbility.Effect.DamageTarget))
            {
                myBehaviour.DamageEnemy = EditorGUILayout.FloatField("Damage to Enemy", myBehaviour.DamageEnemy);


            }
            //damage to self
            if (myBehaviour.EffectsList.Contains(HeroAbility.Effect.DamageSelf))
            {
                myBehaviour.DamageSelf = EditorGUILayout.FloatField("Damage to Self", myBehaviour.DamageSelf);

            }

            //stun to enemy
            if (myBehaviour.EffectsList.Contains(HeroAbility.Effect.StunOther))
            {
                myBehaviour.StunEnemy = EditorGUILayout.FloatField("Stun length to Enemy", myBehaviour.StunEnemy);

            }
            //stun to self
            if (myBehaviour.EffectsList.Contains(HeroAbility.Effect.StunSelf))
            {
                myBehaviour.StunSelf = EditorGUILayout.FloatField("Stun length to Self", myBehaviour.StunSelf);

            }
            */
    }
}
