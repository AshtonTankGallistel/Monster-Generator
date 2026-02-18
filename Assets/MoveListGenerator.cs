using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveListGenerator : MonoBehaviour
{
    //ASHTON GALLISTEL, THESE ARE YOUR NEXT STEPS: Implement attack name generator from key words
    //DONE(each name is 2 words, create 2 lists of each keyword list to combo (1 list is first word, 2 is 2nd), preventing dupes by popping a random word from one of them for each name)
    //Then, implement a system to randomly pull moves for the mons
    //Then, implement a GUI!

    [SerializeField] public string[] typeList = { "None", "Fire", "Water", "Grass" };

    //user-inputted words that function for each type
    [SerializeField] public string[] typeKeyWords = { "Bite,Scratch,Chomp,Punch,Kick,Slap", "Fiery,Flame,Blast,Burst,Heat", "Bubble,Splash,Wet,Wash,Rain", "Grove,Leaf,Root,Tangle,Branch" };

    //How many moves each type has
    public int numOfMovesPerType = 5;

    [Serializable]
    public class moveInfo
    {
        public string moveName;
        public string typing;
        public float power;
        public string[] tags; // Extra info about the move. What stat does it use, what buffs/debuffs, etc.

        public moveInfo(string myName, string myTyping, float myPower, string[] myTags)
        {
            moveName = myName;
            typing = myTyping;
            power = myPower;
            tags = myTags;
        }
    }

    [Serializable]
    public class typeListOfMoves
    {
        public string typeName;
        public moveInfo[] myMoves;
    }

    [Serializable]
    public class completeListOfMoves
    {
        public typeListOfMoves[] moveArrays;
        //public Dictionary<string, typeListOfMoves> movesMasterList;
    }

    Dictionary<string, typeListOfMoves> movesMasterList = new Dictionary<string, typeListOfMoves>();

    [Header("Move Details")]
    [SerializeField] public float movePowerMin = 20;
    [SerializeField] public float movePowerMax = 100;
    [SerializeField] public float movePowerStep = 10; //Difference between each amount of power (steps of 5 => 20, 25, 30)

    [SerializeField] public int numOfTags = 2;
    //At least one of these tags will be on every move.
    [SerializeField] public string guaranteedTags = "Physical,Special";
    //Tags that will be randomly applied
    [SerializeField] public string BonusTags = "BuffAtk,BuffDef,BuffSpe";


    // Start is called before the first frame update
    void Start()
    {
        generateList();
    }

    public void generateList()
    {
        //Calculating all possible move powers ahead of time, to save calculation time later
        //setup array length
        float[] possibleMovePowers = null;
        if (movePowerStep != 0) // no dividing by 0!
        {
            int pmpLength = 1 + Mathf.CeilToInt((movePowerMax - movePowerMin) / movePowerStep);
            if (pmpLength < 2) pmpLength = 2;
            possibleMovePowers = new float[pmpLength];
        }
        else
        {
            if(movePowerMax != movePowerMin) possibleMovePowers = new float[2];
            else possibleMovePowers = new float[1];
        }
        //fill array
        possibleMovePowers[0] = movePowerMin;
        for(int i = 1; i < possibleMovePowers.Length - 1; i++)
        {
            possibleMovePowers[i] = movePowerMin + (movePowerStep * i);
        }
        possibleMovePowers[possibleMovePowers.Length - 1] = movePowerMax;

        //Setup true tag arrays
        string[] guaranteedTagsArray = guaranteedTags.Split(",");
        if (guaranteedTagsArray[0] == "" && guaranteedTagsArray.Length == 1) guaranteedTagsArray = new string[0];
        string[] BonusTagsArray = BonusTags.Split(",");
        if (BonusTagsArray[0] == "" && BonusTagsArray.Length == 1) BonusTagsArray = new string[0];
        foreach (string tag in guaranteedTagsArray) print(tag);
        foreach (string tag in BonusTagsArray) print(tag);

        //We can't put dictionaries into the final json, so we make an array to use instead
        //All the same info is kept, just easier to setup this way. I think.
        completeListOfMoves finalMoveArray = new completeListOfMoves();
        finalMoveArray.moveArrays = new typeListOfMoves[typeList.Length];
        //foreach (string typeName in typeList)
        for (int i = 0; i < typeList.Length; i++)
        {
            //setup new typeListOfMoves
            movesMasterList[typeList[i]] = new typeListOfMoves();
            movesMasterList[typeList[i]].typeName = typeList[i];
            //Come up with names to use for the moves
            string[] wordArray1 = typeKeyWords[i].Split(",");
            List<string> finalNameList = new List<string>();
            foreach(string word1 in wordArray1)
            {
                foreach (string word2 in wordArray1)
                {
                    finalNameList.Add(word1 + " " + word2);
                }
            }
            //If the user wants more move names then it's possible to create, add 'bonus' names
            while(finalNameList.Count < numOfMovesPerType)
            {
                //adds 'Bonus TypeName #_' to the list.
                //Technically goes from highest to 1 rather than 1 to highest... but same result either way!
                finalNameList.Add("Bonus " + typeList[i] + " #" + (numOfMovesPerType - finalNameList.Count).ToString());
            }
            //Fill it out!
            moveInfo[] moveList;
            moveList = new moveInfo[numOfMovesPerType];
            for (int x = 0; x < moveList.Length; x++)
            {
                //MOVE NAME HANDLING
                int moveNamePos = UnityEngine.Random.Range(0, finalNameList.Count); //get name position
                //MOVE POWER HANDLING
                int movePowerPos = UnityEngine.Random.Range(0, possibleMovePowers.Length); //get name position
                //MOVE TAG HANDLING
                string[] moveTags;
                //determine num of tags before setting them, to be safe
                if(guaranteedTagsArray.Length > 0 && BonusTagsArray.Length > 0)
                    moveTags = new string[numOfTags];
                else
                    moveTags = new string[numOfTags];
                //TODO: EXPAND THIS TO HANDLE EDGE CASES OF ARRAY LENGTHS
                //First spot becomes a guaranteed tag if any exist. All other spots become a random tag
                if (numOfTags >= 1)
                {
                    if (guaranteedTagsArray.Length > 0)
                        moveTags[0] = guaranteedTagsArray[UnityEngine.Random.Range(0, guaranteedTagsArray.Length)];
                    else if (BonusTagsArray.Length > 0)
                        moveTags[0] = BonusTagsArray[UnityEngine.Random.Range(0, BonusTagsArray.Length)];
                    //Only add tags if there are tags to add
                    if (BonusTagsArray.Length > 0)
                    {
                        //can get duplicate tags. I'm ok with this.
                        for (int tagNum = 1; tagNum < numOfTags; tagNum++)
                        {
                            moveTags[tagNum] = BonusTagsArray[UnityEngine.Random.Range(0, BonusTagsArray.Length)];
                        }
                    }
                }
                //BUILD THE MOVE
                moveList[x] = new moveInfo(finalNameList[moveNamePos], typeList[i], possibleMovePowers[movePowerPos], moveTags); //TODO: IMPLEMENT MORE TAG STUFF
                finalNameList.RemoveAt(moveNamePos); //remove name as option from list to prevent dupe names
            }
            movesMasterList[typeList[i]].myMoves = moveList;
            //add it to the final json array!
            finalMoveArray.moveArrays[i] = movesMasterList[typeList[i]];
            //finalMoveArray[i] = movesMasterList[typeList[i]];
        }

        //JSON SAVING//////////////////////////////////////////////////////
        string myJsonInfo = JsonUtility.ToJson(finalMoveArray);
        Debug.Log("Attempting save...");
        print(myJsonInfo);
        //Set file name and position
        string filePosition = Application.dataPath + "/Move Lists/TestList.txt";
        //Check if there are duplicate names, modify the name until we get a new name
        int attempts = 1;
        while (File.Exists(filePosition))
        {
            filePosition = Application.dataPath + "/Move Lists/TestList (" + attempts.ToString() + ").txt";
            attempts++;
        }
        File.WriteAllText(filePosition, myJsonInfo);
        Debug.Log("Saved! Probably");
    }


    static public completeListOfMoves loadMovesFromJson(string moveFileName = "TestList")
    {
        Debug.Log("Attempting load...");
        if (File.Exists(Application.dataPath + "/Move Lists/" + moveFileName + ".txt"))
        {
            string myJsonInfo = File.ReadAllText(Application.dataPath + "/Move Lists/" + moveFileName + ".txt");
            completeListOfMoves myNewList = JsonUtility.FromJson<completeListOfMoves>(myJsonInfo);
            Debug.Log("Loaded! Probably");
            return myNewList;
        }
        else
        {
            print("Load failed!");
            return null;
        }
    }
}
