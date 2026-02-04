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

public class MonsterGenerator : MonoBehaviour
{
    public int statTotal = 400;
    public string desiredType = "None";
    public float dualTypeOdds = 0.5f;

    [SerializeField] public string[] typeList = { "Fire", "Water", "Grass" };

    [SerializeField] public string[] statList = { "Health", "Attack", "Defense", "Special Attack", "Special Defense", "Speed" };

    [SerializeField] GameObject tester;

    [SerializeField] TextMeshProUGUI resultDisplay;

    [SerializeField] TMP_InputField statTotalInput;

    public class monsterInfo
    {
        public string[] typing;
        public int[] stats;
        //public Dictionary<string, int> stats;
    }

    // Start is called before the first frame update
    void Start()
    {
        //Tester code for checking if random code worked
        //for (int i = 0; i < 50; i++)
        //{
        //    float myRand = UnityEngine.Random.value;
        //    float value = randomHeavyCenter(myRand);
        //    Debug.Log(value);
        //    GameObject myTester = Instantiate(tester);
        //    myTester.transform.position = new Vector2(myRand,value) * 4;
        //}
        monsterInfo myNewMonster = GenerateMonster();
        saveMonsterToJson(myNewMonster);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public monsterInfo GenerateMonster(bool exactTotal = true)
    {
        //STATS
        Dictionary<string, int> statLine = new Dictionary<string, int>();


        if (!exactTotal) //Not exact total
        {
            int statAverage = statTotal / statList.Length;
            //Our goal is to have a 'curve' for each of the stats, from 0 to 2 times the average, centering on 1.
            int remainingStats = Mathf.FloorToInt(statAverage * 2 * randomHeavyCenter(UnityEngine.Random.value));
            foreach (string stat in statList)
            {
                //Get random value from 0 to 2xthe average
                //if 0, set to 1
                statLine[stat] = Math.Max(1, Mathf.FloorToInt(statAverage * 2 * randomHeavyCenter(UnityEngine.Random.value)));
                //Debug.Log(statLine[stat]);
            }
            //Debug.Log(statLine);
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
            print(statTotal - currentTotalStats);
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
        string[] myTypes;
        if(UnityEngine.Random.value <= dualTypeOdds) //monster becomes dual typed
        {
            int typeID1 = UnityEngine.Random.Range(0, typeList.Length);
            int typeID2 = UnityEngine.Random.Range(0, typeList.Length);
            //check for and prevent dual-same-type
            if (typeID1 == typeID2)
            {
                //ok this is kinda wacky but also technically the most efficient way to do so without spending a bunch of effort/memory popping from a list
                //This effectively cycles ahead in the list (and looping if it goes beyond) from the previous point, up to right before it
                //ie 3 out of 12 goes from 4 to 2 (4 to 12, plus 0 to 2)
                Debug.Log(typeID2);
                typeID2 = (typeID2 + UnityEngine.Random.Range(1, typeList.Length - 1)) % typeList.Length;
                Debug.Log(typeID2);
            }
            myTypes = new string[2] { typeList[typeID1], typeList[typeID2] };
        }
        else
            myTypes = new string[1] { typeList[UnityEngine.Random.Range(0, typeList.Length)] };
        if (desiredType != "None")
        {
            myTypes[0] = desiredType;
        }

        //WRITE RESULTS
        resultDisplay.text = "";
        //types
        resultDisplay.text += myTypes[0];
        if (myTypes.Length > 1) resultDisplay.text += " / " + myTypes[1];
        resultDisplay.text += "\n";
        //stats
        foreach (string stat in statLine.Keys)
        {
            resultDisplay.text += stat +": "+ statLine[stat].ToString() + "\n";
        }




        //RETURN RESULTS

        //Convert stats dictionary into an array
        //(JSON can't use dictionaries... so we use an array instead!)
        int[] statsArray = new int[6];
        for(int i = 0; i < statList.Length; i++)
        {
            statsArray[i] = statLine[statList[i]];
        }
        //Form it into a class to return!
        monsterInfo myMonster = new monsterInfo
        {
            typing = myTypes,
            stats = statsArray
        };
        return myMonster;
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

    public void updateStatTotalFromText()
    {
        Debug.Log("running updateStatTotal");
        string newStatTotal = statTotalInput.text;
        Debug.Log("-" + newStatTotal + "-");
        //newStatTotal = "99999";
        Debug.Log(newStatTotal);

        int result;
        if (Int32.TryParse(newStatTotal, out result))
        {
            Debug.Log("huh?");
            statTotal = result;
        }
        else
        {
            Debug.Log(result);
            statTotalInput.text = statTotal.ToString();
        }
    }

    public void generateAndSave()
    {
        monsterInfo myNewMonster = GenerateMonster();
        saveMonsterToJson(myNewMonster);
    }

    //Json code based on www.youtube.com/watch?v=VR0mIs80Gys , ty to them!
    void saveMonsterToJson(monsterInfo myMonster, string monsterName = "myMonster")
    {
        //string myJsonInfo = JsonUtility.ToJson(myMonster);
        string mystring = "blauhahsdu";
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

    //monsterInfo loadMonsterToJson(string monsterName = "myMonster")
    //{
    //    string myJsonInfo = JsonUtility.ToJson("tester");//myMonster);
    //    Debug.Log("Attempting save...");
    //    File.WriteAllText(Application.dataPath + "/" + monsterName + ".txt", myJsonInfo);
    //    Debug.Log("Saved! Probably");

    //    return;
    //}
}
