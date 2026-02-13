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
        public int power;

        public moveInfo(string myName, string myTyping, int myPower)
        {
            moveName = myName;
            typing = myTyping;
            power = myPower;
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

    // Start is called before the first frame update
    void Start()
    {
        //generateList();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void generateList()
    {
        //later on, addiitonal info will be ran here to determine what moves get made
        //For now, lets cheat!

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
                int moveNamePos = UnityEngine.Random.Range(0, finalNameList.Count); //get name position
                moveList[x] = new moveInfo(finalNameList[moveNamePos], typeList[i], (x+1) * 100 / moveList.Length);
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
