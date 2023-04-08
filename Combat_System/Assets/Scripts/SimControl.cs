/*******************************************************************************
Author:    Benjamin Ellinger
DP Email:  bellinge@digipen.edu
Date:      2/1/2022
Course:    DES 212

Description:
	This file is part of the framework for the 1D Combat Simulator assignment in
	DES 212 (System Design Methods). It can be freely used and modified by students
	for that assignment.
	
    This component controls the entire combat simulation. This component is added to
    a game object whose only purpose to contain this functionality, but using a
    ScriptedObject would potentially be a more advanced	way of doing this.
	
*******************************************************************************/

//Standard Unity component libraries

using System;
using System.Collections; //Not needed in this file, but here just in case.
using System.Collections.Generic; //Not needed in this file, but here just in case.
using System.IO;
using Unity.VisualScripting;
//using UnityEditor.PackageManager; //Needed for writing telemetry data to a file.
using UnityEngine; //The library that lets your access all of the Unity functionality.
using UnityEngine.UI;
using Random = UnityEngine.Random; //This is here so we don't have to type out longer names for UI components.

//Inherits from MonoBehavior like all normal Unity components do...
//Remember that the class name MUST be identical to the file name!
public class SimControl : MonoBehaviour
{
    //Does the simulation start in Auto mode?
    public static bool AutoMode = false;
    //Does the simulation start in Fast mode?
    public static bool FastMode = false;

    public static bool MixedMode = false;


    [SerializeField]
    public int MixCount = 3;
    
    [SerializeField]
    public int GroupCount = 3;
    
    public static bool GroupMode = false;
    //This is the delta time the simulation uses,
    //which is artificially increased when in fast mode.
    public static float DT;

    //How many different AI types (and therefore how many "fights") do we want?
    public int Fights = 5;
    private int FightCount = 0;
    public static bool SimOver = false; //Have all the fights been completed?
    public enum PlayerAIMode
    {
        AbilityOne,
        AbilityTwo,
        AbilityThree,
        AbilityFour,
        AbilityRandom,
        AbilitySmart,
        cCount
    }

    [SerializeField]
    public static List<PlayerAIMode> AI_List;

    public static int CurrentAI = 0; //What's the current type of AI for this fight?

    //How many rounds is each "fight"?
    public int Rounds = 5;
    private static int RoundCount = 0;
    public static bool RoundOver = false; //Did the current round just end?
    public static bool RoundStart = false; //Is a new round just starting (make sure the player has time to find a target)?
    //How long a delay between rounds?
    public float RoundDelay = 3.0f;
    private float RoundTimer = 3.0f;

    //How far from the center of the screen is the "edge" of the arena?
    public static float EdgeDistance = 8.0f;
    //How far from the center of the screen do combatants start?
    public static float StartingX = 6.0f;
 
    //Telemetry data for an individual fight.
    public static int Victories = 0;
    public static int Defeats = 0;
    public static float DamageDone = 0;
    public static float TotalFightTime = 0;
    public static StreamWriter DataStream; //Stream used to write the data to a file.

    //Need a reference to the player, so we don't have to look it
    //up each time.
    public static Hero Player;

    //We will use the UI canvas a lot, so store a reference to it.
    public static GameObject Canvas;

	//References for text prefabs and enemy prefabs, so we don't
	//have to load them each time.
    public static GameObject InfoTextPrefab;
    public static GameObject StaticInfoTextPrefab;

    [SerializeField]
    public static List<GameObject> EnemyTypes;

    public static GameObject RangeSignifierPrefab;
    public static GameObject AOESignifierPrefab;

    [SerializeField]
    public List<PlayerAIMode> temp_AI_List;

    public static int enemyit = 0;

    public static List<List<int>> abilityCounts;
    public static List<int> abilityCountsRound;


    //Start is called before the first frame update
    void Start()
    {
        //Create a comma-separated value file to output telemetry data.
        //This can just then be directly opened in Excel.
        DataStream = new StreamWriter("FightData.csv", true);
        DataStream.WriteLine("*1v1*,AI TYPE,AB1(%),AB2(%),AB3(%),AB4(%),VICTORIES,DEFEATS,WIN(%),DPS,ROUND LENGTH,*1v3*,AI TYPE,AB1(%),AB2(%),AB3(%),AB4(%),VICTORIES,DEFEATS,WIN(%),DPS,ROUND LENGTH,\n"); //Write some headers for our columns.

        //Get a reference to the canvas (used for UI objects).
        Canvas = GameObject.Find("Canvas");

        //Get a reference to the player's game object.
        //Note that we use GetComponent so we don't have to do that
        //every time we want to access the Hero class functionality.
        Player = GameObject.Find("Hero").GetComponent<Hero>();

        //Load all the prefabs we are going to use.
        InfoTextPrefab = Resources.Load("Prefabs/InfoText") as GameObject;
        StaticInfoTextPrefab = Resources.Load("Prefabs/StaticInfoText") as GameObject;
        RangeSignifierPrefab = Resources.Load("Prefabs/RangeSig") as GameObject;
        AOESignifierPrefab = Resources.Load("Prefabs/AOESig") as GameObject;
        EnemyTypes = new List<GameObject>(Resources.LoadAll<GameObject>("Prefabs/Enemies"));
        EnemyTypes.Sort((p1, p2) => p1.GetComponent<Enemy>().strength.CompareTo(p2.GetComponent<Enemy>().strength));
        AI_List = temp_AI_List;
        abilityCounts = new List<List<int>>(Rounds);
        abilityCountsRound = new List<int>(new int[4]);

    }

//Update is called once per frame
void Update()
    {
        //If the ESC key is pressed, exit the program.
        if (Input.GetKeyDown(KeyCode.Escape) == true)
        {
            DataStream.Close();
            Application.Quit();

        }

        //The simulation is over, so stop updating.
        if (FightCount >= Fights)
        {
            if (SimOver == false) //Did the simulation just end?
            {
                SimOver = true;
                FastMode = false;

                DataStream.Close(); //Don't forget to close the stream.
                SpawnInfoText("SIMULATION OVER", true);
            }
            return;
        }

		//If the A key is pressed, toggle Auto mode on or off.
        if (Input.GetKeyDown(KeyCode.A) == true)
            AutoMode = !AutoMode;

        //If the F key is pressed, toggle Fast Auto mode on or off.
        if (Input.GetKeyDown(KeyCode.F) == true)
            FastMode = !FastMode;

        //If the R key is pressed, restart the simulation.
        if (Input.GetKeyDown(KeyCode.R) == true)
        {
            FightCount = 0;
            RoundCount = 0;
            RoundTimer = RoundDelay;
            RoundStart = false;
            RoundOver = false;
            SimOver = false;
            RoundCount = 0;
            Victories = 0;
            Defeats = 0;
            DamageDone = 0;
            TotalFightTime = 0;
        }

        //on press "G" toggle group mode
        if (Input.GetKeyDown(KeyCode.G))
        {
            GroupMode = !GroupMode;
        }

        //on "M" press toggle mixed mode
        if (Input.GetKeyDown(KeyCode.M))
        {
            MixedMode = !MixedMode;
        }

        //Get the actual delta time, but cap it at one-tenth of
        //a second. Except in fast mode, where we just make it
        //one-tenth of a second all the time. Note that if we make
        //this more than one-tenth of a second, we might get different
        //results in fast mode vs. normal mode by "jumping" over time
        //thresholds (cooldowns for example) that are in tenths of a second.
        if (FastMode)
            DT = 0.05f; //We could go even faster by not having visual feedback in this mode...
        else if (Time.deltaTime < 0.1f)
            DT = Time.deltaTime;
        else
            DT = 0.1f;

        //It's the start of a fight, so start a new round.
        if (RoundCount == 0)
            NewRound();

        RoundOver = IsRoundOver();
        if (RoundOver == false) //The round isn't over, so run the simulation (all the logic is in the updates of other classes).
            TotalFightTime += DT; //Accumulate the SIMULATED time for telemetry data.
        else if (RoundTimer > 0.0f) //The round is over, but this is the delay before a new round.
            RoundTimer -= DT; //Update the round delay timer.
        else //Time for a new round.
            NewRound();
    }

    //The round is over if either the player is dead or all enemies are.
    bool IsRoundOver()
    {
        //Player is dead.
        if (Player.HitPoints <= 0.0f)
        {
            if (RoundOver == false ) //Player just died.
            {
                SpawnInfoText("DEFEAT...");
                Defeats++;
            }
            return true;
        }
        //Enemies are dead.
        if (Player.Target == null)
        {
            if (RoundStart == true) //Make sure player has a chance to find a target at the start of a round.
                return false;
            if (RoundOver == false) //Last enemy just died.
            {

                SpawnInfoText("VICTORY!!!");
                Victories++;
            }
            return true;
        }
        //Round is not over.
        RoundStart = false;
        return false;
    }

	//Reset everything for the new round.
    void NewRound()
    {

        RoundCount++;
        if (RoundCount != 1)
        {
            //track counts
            abilityCounts.Add(new List<int>(abilityCountsRound));
            abilityCountsRound.Clear();
            abilityCountsRound = new List<int>(new int[4]);
        }

        //Clear out any remaining enemies.
        ClearEnemies();
        
        //The whole fight is over, so start a new one.
        if (RoundCount > Rounds)
        {
            NewFight();
            return;
        }





        //Note that this just cycles through enemy types, but you'll need more structure than this.
        //Each fight should be one AI type against one enemy type multiple times. And then each AI type
        //against a group of the same type multiple times. And then each AI type against a mixed group
        //multiple times. And possibly more.

        //Call the Initialize() functions for the player.
        Player.Initialize();

        //Feedback is good...
        SpawnInfoText("ROUND " + RoundCount); //Look! A string concatenation operator!

        //Spawn enemies by calling the Unity engine function Instantiate().
        //Pass in the appropriate prefab, its position, its rotation (90 degrees),
        //and its parent (none).

        //3 different combos
        if (MixedMode)
        {
            switch (Random.Range(0, MixCount))
            {
                case 0:
                    Instantiate(EnemyTypes[0], new Vector3(StartingX, Random.Range(-1.5f, 1.0f), 0), Quaternion.Euler(0, 0, 90), null); //Just make multiple calls to spawn a group of enemies.
                    Instantiate(EnemyTypes[1], new Vector3(StartingX, Random.Range(-1.5f, 1.0f), 0), Quaternion.Euler(0, 0, 90), null); //Just make multiple calls to spawn a group of enemies.
                    Instantiate(EnemyTypes[2], new Vector3(StartingX, Random.Range(-1.5f, 1.0f), 0), Quaternion.Euler(0, 0, 90), null); //Just make multiple calls to spawn a group of enemies.
                    break;
                case 1:
                    Instantiate(EnemyTypes[1], new Vector3(StartingX, Random.Range(-1.5f, 1.0f), 0), Quaternion.Euler(0, 0, 90), null); //Just make multiple calls to spawn a group of enemies.
                    Instantiate(EnemyTypes[3], new Vector3(StartingX, Random.Range(-1.5f, 1.0f), 0), Quaternion.Euler(0, 0, 90), null); //Just make multiple calls to spawn a group of enemies.
                    Instantiate(EnemyTypes[5], new Vector3(StartingX, Random.Range(-1.5f, 1.0f), 0), Quaternion.Euler(0, 0, 90), null); //Just make multiple calls to spawn a group of enemies.
                    break;

                case 2:
                    Instantiate(EnemyTypes[2], new Vector3(StartingX, Random.Range(-1.5f, 1.0f), 0), Quaternion.Euler(0, 0, 90), null); //Just make multiple calls to spawn a group of enemies.
                    Instantiate(EnemyTypes[4], new Vector3(StartingX, Random.Range(-1.5f, 1.0f), 0), Quaternion.Euler(0, 0, 90), null); //Just make multiple calls to spawn a group of enemies.
                    Instantiate(EnemyTypes[1], new Vector3(StartingX, Random.Range(-1.5f, 1.0f), 0), Quaternion.Euler(0, 0, 90), null); //Just make multiple calls to spawn a group of enemies.
                    break;

                default: break;
            }
        }
        //just group mode, all same enemies
        else if (GroupMode)
        {
            for (int i = 0; i < GroupCount; ++i)
            {
                Instantiate(EnemyTypes[enemyit], new Vector3(StartingX, Random.Range(-1.5f, 1.0f), 0), Quaternion.Euler(0, 0, 90), null); //Just make multiple calls to spawn a group of enemies.
            }
        }
        //Just a single enemy
        else
        {
            Instantiate(EnemyTypes[enemyit], new Vector3(StartingX, 0, 0), Quaternion.Euler(0, 0, 90), null); //Just make multiple calls to spawn a group of enemies.
        }

        //Reset the round delay timer (and round start flag) for after this new round ends.
        RoundTimer = RoundDelay;
        RoundStart = true;
    }

    //Reset everything for the new fight.
    void NewFight()
    {
        FightCount++;
        RoundCount = 0;
        //Show a bit of telemetry data on screen.
        SpawnInfoText(Victories + "-" + Defeats + "\n" + DamageDone / TotalFightTime + " DPS");
        //Write all the telemetry data to the file.

        //should always do single mode first
        if (GroupMode)
        {
            DataStream.Write(", ," + AI_List[CurrentAI].ToString() + ",");



            /************AVERAGE STUFF**************/
            //i is round
            //j is ability count
            //for each ability write the average percentage used per round
            List<List<float>> averages = new List<List<float>>();
            for (int i = 0; i < abilityCounts.Count; ++i)
            {
                //list of ability percentages
                List<float> percent_used = new List<float>();
                //total use counter
                float total_used = 0.0f;

                //get percentage each ability was used
                //for each ability in this round
                foreach (var ability in abilityCounts[i])
                {
                    total_used += ability;
                }
                //for rach ability in round (i)
                for (int j = 0; j < abilityCounts[i].Count; ++j)
                {
                    //add the percentage to the list of percentages
                    percent_used.Add((abilityCounts[i][j] / total_used));
                }

                averages.Add(new List<float>(percent_used));

            }
            //store the final outputs
            List<float> final_average = new List<float>(new float[4]);
            //averages is of size, number of abilities(x) by number of rounds(y)
            //add up all percentages per round, divide by number of rounds
            //this is round iterator
            for (int k = 0; k < averages.Count; ++k)
            {
                //add up each percent
                //this is ability iterator
                for (int l = 0; l < averages[k].Count; ++l)
                {
                    final_average[l] += averages[k][l];
                }
                //divide to find average
                //final_average[k] /= averages[k].Count;
            }

            for (int m = 0; m < final_average.Count; ++m)
            {
                final_average[m] /= (float)Rounds;
            }
            /************AVERAGE STUFF**************/


            //print each average
            foreach (var _out in final_average)
            {
                DataStream.Write(_out*100 + ",");

            }
            DataStream.Write( Victories + "," + Defeats + ","+ ((Victories/Rounds)*100) +"," + DamageDone / TotalFightTime + "," + TotalFightTime / Rounds + ", \n");
            GroupMode = !GroupMode;
            ++enemyit;
            if (enemyit > EnemyTypes.Count) enemyit = 0;
            CurrentAI++;
            if (CurrentAI > AI_List.Count) { CurrentAI = 0; }

            abilityCounts = new List<List<int>>();
        }
        else
        {
            DataStream.Write("," + AI_List[CurrentAI].ToString() + ",");
            //i is round
            //j is ability count
            //for each ability write the average percentage used per round
            List<List<float>> averages = new List<List<float>>();
            for (int i = 0; i < abilityCounts.Count; ++i)
            {
                //list of ability percentages
                List<float> percent_used = new List<float>();
                //total use counter
                float total_used = 0.0f;

                //get percentage each ability was used
                //for each ability in this round
                foreach (var ability in abilityCounts[i])
                {
                    total_used += ability;
                }
                //for rach ability in round (i)
                for (int j = 0; j < abilityCounts[i].Count; ++j)
                {
                    //add the percentage to the list of percentages
                    percent_used.Add((abilityCounts[i][j] / total_used));
                }

                averages.Add(new List<float>(percent_used));

            }
            //store the final outputs
            List<float> final_average = new List<float>(new float[4]);
            //averages is of size, number of abilities(x) by number of rounds(y)
            //add up all percentages per round, divide by number of rounds
            //this is round iterator
            for (int k = 0; k < averages.Count; ++k)
            {
                //add up each percent
                //this is ability iterator
                for (int l = 0; l < averages[k].Count; ++l)
                {
                    final_average[l] += averages[k][l];
                }
                //divide to find average
                //final_average[k] /= averages[k].Count;
            }

            for(int m = 0; m < final_average.Count; ++m)
            {
                final_average[m] /= (float)Rounds;
            }
            //print each average
            foreach (var _out in final_average)
            {
                DataStream.Write(_out * 100 + ",");

            }


            DataStream.Write(Victories + "," + Defeats + "," + ((Victories / Rounds)*100) + "," + DamageDone / TotalFightTime + "," + TotalFightTime / Rounds);
            GroupMode = !GroupMode;
            abilityCounts = new List<List<int>>();

        }
        //Reset the telemetry counters
        Victories = 0;
        Defeats = 0;
        DamageDone = 0;
        TotalFightTime = 0;
        //After the first fight (which is random), just spam a single key for each fight.

    }

    //Destroy all the enemy game objects.
    void ClearEnemies()
    {
        //Find all the game objects that have an Enemy component.
        var enemies = FindObjectsOfType<Enemy>();
        foreach (Enemy enemy in enemies) //A foreach loop! Fancy...
            DestroyImmediate(enemy.gameObject);
    }

    //Spawn text at the center of the screen.
    //If set to static, that just means it doesn't move.
    void SpawnInfoText(string text, bool isStatic = false)
    {
        //dont show feedback if in fast mode
        if(!FastMode)
            SpawnInfoText(new Vector3(0, 0, 0), text, isStatic);
    }

    //Spawn text wherever you want.
    //If set to static, that just means it doesn't move.
    void SpawnInfoText(Vector3 location, string text, bool isStatic = false)
    {
        //Throw up some text by calling the Unity engine function Instantiate().
        //Pass in the appropriate InfoText prefab, its position, its rotation (none in this case),
        //and its parent (the canvas because this is text). Then we get the
        //Text component from the new game object in order to set the text itself.
        Text infotext;
        if (isStatic)
            infotext = Instantiate(StaticInfoTextPrefab, location, Quaternion.identity, Canvas.transform).GetComponent<Text>();
        else
            infotext = Instantiate(InfoTextPrefab, location, Quaternion.identity, Canvas.transform).GetComponent<Text>();
        //Set the text.
        infotext.text = text;
    }

    /// <summary>
    /// Gets the round counter
    /// </summary>
    /// <returns></returns>
    public static int GetRoundCount()
    {
        return RoundCount;
    }
}