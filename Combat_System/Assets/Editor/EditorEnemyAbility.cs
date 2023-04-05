/*******************************************************************************
Author:    Cameron Myers
DP Email:  cameron.myers@digipen.edu
Date:      2/24/2022
Course:    DES 212

Description:
Custom Editor for Enemy Ability
	
*******************************************************************************/


using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnemyAbility))]

public class EditorEnemyAbility : Editor
{
        public override void OnInspectorGUI()
        {
            // If we call base the default inspector will get drawn too.
            // Remove this line if you don't want that to happen.
            base.OnInspectorGUI();

            EnemyAbility myBehaviour = target as EnemyAbility;

        //target.myBool = EditorGUILayout.Toggle("myBool", target.myBool);
        if(myBehaviour.EffectsList.Count == 0 ) return;

        EnemyAbility.Effect effect = EnemyAbility.Effect.StunSelf;

        for (int i = 0; i < myBehaviour.EffectValues.Count; ++i)
        {
            if(i == (int)EnemyAbility.Effect.cCount) break;
            if (myBehaviour.EffectsList.Contains(effect))
            {
                myBehaviour.EffectValues[i] = EditorGUILayout.FloatField(effect.ToString(), myBehaviour.EffectValues[i]);
            }
            ++effect;
        }

        /*
        //this is for damage to enemy
        if (myBehaviour.EffectsList.Contains(EnemyAbility.Effect.DamageTarget))
            {
                myBehaviour.DamageEnemy = EditorGUILayout.FloatField("Damage to Enemy", myBehaviour.DamageEnemy);

            }
            //damage to self
            if (myBehaviour.EffectsList.Contains(EnemyAbility.Effect.DamageSelf))
            {
                myBehaviour.DamageSelf = EditorGUILayout.FloatField("Damage to Self", myBehaviour.DamageSelf);

            }

            //stun to enemy
            if (myBehaviour.EffectsList.Contains(EnemyAbility.Effect.StunOther))
            {
                myBehaviour.StunEnemy = EditorGUILayout.FloatField("Stun length to Enemy", myBehaviour.StunEnemy);

            }
            //stun to self
            if (myBehaviour.EffectsList.Contains(EnemyAbility.Effect.StunSelf))
            {
                myBehaviour.StunSelf = EditorGUILayout.FloatField("Stun length to Self", myBehaviour.StunSelf);

            }
        */
    }
}
