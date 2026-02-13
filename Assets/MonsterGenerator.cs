using JetBrains.Annotations;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using System.Linq;

public class MonsterGenerator : MonoBehaviour
{
    public int statTotal = 400;
    public string[] desiredType = {};
    public float dualTypeOdds = 0.5f;

    //TYPE HANDLING

    [SerializeField] public string[] typeList = { "Fire", "Water", "Grass" };

    [Header("The corresponding weight of each type. \nIf a type's weight is not listed here, \nit will be set to 1.")]
    [SerializeField] public float[] typeListWeight = { 1, 1, 1 };
    float totalWeight = 0;

    [SerializeField] public string[] statList = { "Health", "Attack", "Defense", "Special Attack", "Special Defense", "Speed" };

    //MOVE HANDLING

    [SerializeField] public int numberOfMoves = 4;

    // Common Move Types are types whose moves are common across all mons, regardless of base type. If chosen, a random type from this list will be pulled.
    [SerializeField] public string[] commonMoveTypes = { "None" };

    // The odds of getting a common type move instead of a matching type move. (If the type matches, this effectively does nothing.)
    [SerializeField] public float commonMoveOdds = 0.3f;

    // The odds of getting a coverage (ie non-common nor matching) type move. Additive with the previous value. (ie, 0.3 common + 0.1 coverage = 0.4 chance of a non-matching type move!)
    [SerializeField] public float coverageMoveOdds = 0.1f;

    [SerializeField] GameObject tester;

    public class monsterInfo
    {
        public string[] typing;
        public string[] statNames;
        public int[] stats;
        public MoveListGenerator.moveInfo[] moves; //We record all info about the moves instead of pointers. THIS IS INNEFICIENT, but allows them to be used without the moveList being saved.
    }

    //An unsused alternate class, theoretically lighter than the regular option, but relying heavily on info being saved to external files that must be accessed.
    //Debated going with this, but relies too heavily on constantly accessing other scripts and storing info seperately. Perhaps if I had more time? idk
    public class monsterInfoLight
    {
        public string[] typing;
        public int[] stats;
        //public MoveListGenerator.moveInfo[] moves; //We record all info about the moves instead of pointers. THIS IS INNEFICIENT, but allows them to be used without the moveList being saved.
        public int[] moveIDs;
        public string moveFileName;
    }


    // Start is called before the first frame update
    void Start()
    {
        // uncomment the below to test stuff!
        // monsterInfo myNewMonster = GenerateMonster();
        // saveMonsterToJson(myNewMonster);

        //Below ran in order to prepare for generating monsters!
        updateTypeWeightDetails();
    }

    //Helper to test random functions, and ensure it actually, works. Not needed anymore, saving anyway to be safe.
    private void testRandomness()
    {
        for (int i = 0; i < 50; i++)
        {
            float myRand = UnityEngine.Random.value;
            float value = randomHeavyCenter(myRand);
            Debug.Log(value);
            GameObject myTester = Instantiate(tester);
            myTester.transform.position = new Vector2(myRand,value) * 4;
        }
    }

    public monsterInfo GenerateMonster(bool exactTotal = true)
    {
        //STATS
        Dictionary<string, int> statLine = new Dictionary<string, int>();

        if(statTotal < 2 * statList.Length)
        {
            statTotal = 2 * statList.Length;
            Debug.Log("[MonsterGen] StatTotal must be 2 times the number of stats or higher! Lower values are not supported.");
        }

        if (!exactTotal) //Not exact total
        {
            int statAverage = statTotal / statList.Length;
            //Our goal is to have a 'curve' for each of the stats, from 0 to 2 times the average, centering on 1.
            int remainingStats = Mathf.FloorToInt(statAverage * 2 * randomHeavyCenter(UnityEngine.Random.value));
            foreach (string stat in statList)
            {
                //Get random value from 0 to 2x the average
                //if 0, set to 1
                statLine[stat] = Math.Max(1, Mathf.FloorToInt(statAverage * 2 * randomHeavyCenter(UnityEngine.Random.value)));
            }
        }
        else //Exact total
        {
            //all the stats are rolled independently, but the results are the ratios of the stats to one another. ie a roll of 0.1/0.5/0.9 with a total of 150 becomes 10/50/90

            Dictionary<string, float> statStrength = new Dictionary<string, float>();
            float totalValueAmount = 0;
            foreach (string stat in statList)
            {
                //Get random value from 0 to 1, which will be used in comparison against eachother
                statStrength[stat] = randomHeavyCenter(UnityEngine.Random.value);
                //Add value to the totalAmount of value
                totalValueAmount += statStrength[stat];
            }
            int currentTotalStats = 0;
            foreach (string stat in statList)
            {
                //if 0, set to 1
                statLine[stat] = Math.Max(1, Mathf.RoundToInt(statTotal * statStrength[stat] / totalValueAmount));
                currentTotalStats += statLine[stat];
            }
            //This should result in a little over or under the exact total...
            //If no 0, this value is usually -1 or 1, rarely 2.
            //print(statTotal - currentTotalStats);
            if(statTotal - currentTotalStats != 0)
            {
                //In this case, find the lowest/highest value stat and add the value to it
                int statDifference = statTotal - currentTotalStats;
                if (statDifference > 0)
                {
                    string lowestStat = "";
                    int lowestVal = int.MaxValue;
                    foreach (string stat in statList)
                    {
                        if (statLine[stat] < lowestVal)
                        {
                            lowestStat = stat;
                            lowestVal = statLine[stat];
                        }
                    }
                    statLine[lowestStat] += statDifference;
                }
                else
                {
                    string highestStat = "";
                    int highestVal = int.MinValue;
                    foreach (string stat in statList)
                    {
                        if (statLine[stat] > highestVal)
                        {
                            highestStat = stat;
                            highestVal = statLine[stat];
                        }
                    }
                    statLine[highestStat] += statDifference;
                }
                //Now, we should have an exact stat total!
                //Also, no need to really worry about splitting +2, as statistically the lowest number will be lower then everything else by more than 2.
            }
        }


        //TYPE
        updateTypeWeightDetails();
        string[] myTypes;
        if (desiredType.Length > 0)//User wishes to select a specific type
        {
            if (desiredType.Length > 1) //User wants a specific set of 2 types!
                myTypes = new string[2] { desiredType[0], desiredType[1]};
            else //User wants the first type to be a specific type!
            {
                if (UnityEngine.Random.value <= dualTypeOdds) //randomly hit dual type!
                {
                    int randomSecondTypeID = getRandomWeightedType(); //get a random type
                    if (desiredType[0] == typeList[randomSecondTypeID]) //if type already is desired, get a unique type!
                    {
                        randomSecondTypeID = getRandomWeightedType(randomSecondTypeID);
                    }
                    myTypes = new string[2] { desiredType[0], typeList[randomSecondTypeID] };
                }
                else
                {
                    myTypes = new string[1] { desiredType[0]};
                }
            }
        }
        else if(UnityEngine.Random.value <= dualTypeOdds) //monster becomes dual typed
        {
            int typeID1 = getRandomWeightedType();
            int typeID2 = getRandomWeightedType(typeID1);
            myTypes = new string[2] { typeList[typeID1], typeList[typeID2] };
        }
        else
            myTypes = new string[1] { typeList[getRandomWeightedType()] };


        //MOVES
        //make move dictionary to pull from
        MoveListGenerator.completeListOfMoves myMoveList = MoveListGenerator.loadMovesFromJson();
        Dictionary<string, MoveListGenerator.moveInfo[]> myMoveDictionary = new Dictionary<string, MoveListGenerator.moveInfo[]>();
        foreach (MoveListGenerator.typeListOfMoves moveTypeList in myMoveList.moveArrays)
            myMoveDictionary[moveTypeList.typeName] = moveTypeList.myMoves;
        //make array to know what types you can pull from for coverage
        string[] coverageTypes = new string[typeList.Length - myTypes.Length];
        if (coverageMoveOdds > 0) //only bother filling if user is using this mechanic
        {
            int counter = 0;
            foreach (string uniqueType in typeList)
            {
                if(!Array.Exists(myTypes, element => element == uniqueType)) //if type not in monster's typing, add it to list
                {
                    coverageTypes[counter] = uniqueType;
                    counter++;
                }
            }
        }
        //actually take the dictionary info and put it into the mon
        MoveListGenerator.moveInfo[] myMonsMoves = new MoveListGenerator.moveInfo[numberOfMoves];
        for (int i = 0; i < numberOfMoves; i++)
        {
            //just doing matching type moves for now, add more later
            //create a unique list of options with no duplicates
            //IEnumerable<MoveListGenerator.moveInfo> validMoves = myMoveDictionary[myTypes[0]].Except(myMonsMoves); //DOES NOT WORK
            float moveChoice = UnityEngine.Random.value;
            if(moveChoice < commonMoveOdds) //check if you pull a common type move
            {
                string myRandomType = commonMoveTypes[UnityEngine.Random.Range(0, commonMoveTypes.Length)];
                myMonsMoves[i] = myMoveDictionary[myRandomType][UnityEngine.Random.Range(0, myMoveDictionary[myRandomType].Length)];
            }
            else
            {
                moveChoice -= commonMoveOdds;
                if (moveChoice < coverageMoveOdds) //check if you pull a coverage move
                {
                    string myRandomType = coverageTypes[UnityEngine.Random.Range(0, coverageTypes.Length)];
                    myMonsMoves[i] = myMoveDictionary[myRandomType][UnityEngine.Random.Range(0, myMoveDictionary[myRandomType].Length)];
                }
                else
                {
                    //pulled a regular type-match move
                    string myRandomType = myTypes[UnityEngine.Random.Range(0, myTypes.Length)];
                    myMonsMoves[i] = myMoveDictionary[myRandomType][UnityEngine.Random.Range(0, myMoveDictionary[myRandomType].Length)];
                }
            }
            
        }


        ////WRITE RESULTS
        //resultDisplay.text = "";
        ////types
        //resultDisplay.text += myTypes[0];
        //if (myTypes.Length > 1) resultDisplay.text += " / " + myTypes[1];
        //resultDisplay.text += "\n";
        ////stats
        //foreach (string stat in statLine.Keys)
        //{
        //    resultDisplay.text += stat +": "+ statLine[stat].ToString() + "\n";
        //}




        //RETURN RESULTS

        //Convert stats dictionary into an array
        //(JSON can't use dictionaries... so we use an array instead!)
        int[] statsArray = new int[statList.Length];
        string[] statsArrayNames = new string[statList.Length];
        for(int i = 0; i < statList.Length; i++)
        {
            statsArrayNames[i] = statList[i];
            statsArray[i] = statLine[statList[i]];
        }
        //Form it into a class to return!
        monsterInfo myMonster = new monsterInfo
        {
            typing = myTypes,
            statNames = statsArrayNames,
            stats = statsArray,
            moves = myMonsMoves
        };
        return myMonster;
    }

    //Helper function that updates the weights for types.
    //Done at start and mid-running to prevent errors from differences between the weight and type lists, as well as account for updates to the weights.
    void updateTypeWeightDetails()
    {
        //editor adjustments, make the typeWeightList as long as the typeList
        if (typeListWeight.Length < typeList.Length)
        {
            float[] newTypeListWeight = new float[typeList.Length];
            for (int i = 0; i < typeList.Length; i++)
            {
                if (i < typeListWeight.Length)
                    newTypeListWeight[i] = typeListWeight[i];
                else
                    newTypeListWeight[i] = 1;
            }
            typeListWeight = newTypeListWeight;
        }
        //Determine total weight
        //(we do this second because if the typeListWeight is shorter than the typeList, this would throw an error.)
        //(and we check it via typeList because if it's shorter, we want to only consider the relevant types.)
        totalWeight = 0;
        for (int i = 0; i < typeList.Length; i++)
            totalWeight += typeListWeight[i];
    }

    //Helper function
    //Gets a random typeID based on their respective weights
    int getRandomWeightedType(int skippedTypeID = -1)
    {
        float currentResult;
        if (skippedTypeID == -1)
        {
            currentResult = UnityEngine.Random.Range(0, totalWeight);
        }
        else
        {
            print("skipping a type!");
            currentResult = UnityEngine.Random.Range(0, totalWeight - typeListWeight[skippedTypeID]);
        }
        print(currentResult);
        for(int i = 0; i < typeList.Length; i++)
        {
            if(skippedTypeID == i)
            {
                continue;
            }
            if (currentResult < typeListWeight[i])
            {
                return i;
            }
            currentResult -= typeListWeight[i];
        }
        print("uh oh");
        print(currentResult);
        return -1;
    }

    //Random float val (0 to 1) based on a bell curve
    float randomOnBellCurve(float rand)
    {
        float position = 2 * rand;
        return easeInOutSine(position);
    }

    //Random float val (0 to 1) with a heavy weight towards the center
    float randomHeavyCenter(float rand)
    {
        if (rand <= 0.5f)
        {
            return eastOutCirc(2 * rand) / 2;
        }
        else
        {
            return (2 - eastOutCirc(2 * rand)) / 2;
        }
    }

    //modified from easings.net
    //Ok turns out, the way this works is that it's already technically a bell curve.
    //They just don't expect you to go above 1, and 0-2 makes a full bell curve. neat!
    float easeInOutSine(float x)
    {
        return (float)-(Math.Cos(Math.PI* x) - 1) / 2;
    }

    //modified from easings.net
    float eastOutCirc(float x)
    {
        return Mathf.Sqrt(1 - Mathf.Pow(x - 1, 2));
    }

    public void updateStatTotal(int input)
    {
        statTotal = input;
    }

    //public void updateStatTotalFromText()
    //{
    //    Debug.Log("running updateStatTotal");
    //    string newStatTotal = statTotalInput.text;
    //    Debug.Log("-" + newStatTotal + "-");
    //    //newStatTotal = "99999";
    //    Debug.Log(newStatTotal);

    //    int result;
    //    if (Int32.TryParse(newStatTotal, out result))
    //    {
    //        Debug.Log("huh?");
    //        statTotal = result;
    //    }
    //    else
    //    {
    //        Debug.Log(result);
    //        statTotalInput.text = statTotal.ToString();
    //    }
    //}

    public void generateAndSave()
    {
        monsterInfo myNewMonster = GenerateMonster();
        saveMonsterToJson(myNewMonster);
    }

    //Json code based on www.youtube.com/watch?v=VR0mIs80Gys , ty to them!
    public void saveMonsterToJson(monsterInfo myMonster, string monsterName = "myMonster")
    {
        string myJsonInfo = JsonUtility.ToJson(myMonster);
        Debug.Log("Attempting save...");
        print(myJsonInfo);
        //Set file name and position
        string filePosition = Application.dataPath + "/GeneratedMonsters/" + monsterName + ".txt";
        //Check if there are duplicate names, modify the name until we get a new name
        int attempts = 1;
        while(File.Exists(filePosition))
        {
            filePosition = Application.dataPath + "/GeneratedMonsters/" + monsterName + " (" + attempts.ToString() + ").txt";
            attempts++;
        }
        File.WriteAllText(filePosition, myJsonInfo);
        Debug.Log("Saved! Probably");
    }

    public monsterInfo loadMonsterFromJson(string monsterName = "myMonster")
    {
        Debug.Log("Attempting load...");
        if(File.Exists(Application.dataPath + "/GeneratedMonsters/" + monsterName + ".txt"))
        {
            string myJsonInfo = File.ReadAllText(Application.dataPath + "/GeneratedMonsters/" + monsterName + ".txt");
            monsterInfo myNewMonster = JsonUtility.FromJson<monsterInfo>(myJsonInfo);
            Debug.Log("Loaded! Probably");
            return myNewMonster;
        }
        else
        {
            print("Load failed!");
            return null;
        }
    }
}
