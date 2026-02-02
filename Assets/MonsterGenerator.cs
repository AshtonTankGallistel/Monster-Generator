using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.UI;
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
        GenerateMonster();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GenerateMonster()
    {
        //STATS
        Dictionary<string, int> statLine = new Dictionary<string, int>();

        int statAverage = statTotal / statList.Length;

        //Our goal is to have a 'curve' for each of the stats, from 0 to 2 times the average, centering on 1.
        int remainingStats = Mathf.FloorToInt(statAverage * 2 * randomHeavyCenter(UnityEngine.Random.value));

        foreach (string stat in statList)
        {
            //Get random value from 0 to 2xthe average
            //if 0, set to 1
            statLine[stat] = Math.Max(1,Mathf.FloorToInt(statAverage * 2 * randomHeavyCenter(UnityEngine.Random.value)));
            //Debug.Log(statLine[stat]);
        }
        //Debug.Log(statLine);


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
}
