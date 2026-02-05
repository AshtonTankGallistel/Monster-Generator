using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using TMPro;

public class GeneratorExampleSceneManager : MonoBehaviour
{
    [SerializeField] MonsterGenerator myGenerator;
    public MonsterGenerator.monsterInfo currentMonster;
    public string monsterFileName = "MyMonster";

    [SerializeField] TextMeshProUGUI resultDisplay;

    [SerializeField] TMP_InputField statTotalInput;

    [SerializeField] TMP_InputField fileNameInput;

    // Start is called before the first frame update
    void Start()
    {
        //Want to test something on run? Add it here!
    }

    void writeMyMonster(MonsterGenerator.monsterInfo myMonster)
    {
        //WRITE RESULTS
        resultDisplay.text = "";
        //types
        resultDisplay.text += myMonster.typing[0];
        if (myMonster.typing.Length > 1) resultDisplay.text += " / " + myMonster.typing[1];
        resultDisplay.text += "\n";
        //stats
        for(int i = 0; i < myMonster.stats.Length; i++)
        {
            if (i < myGenerator.statList.Length)
            {
                resultDisplay.text += myGenerator.statList[i] + ": " + myMonster.stats[i].ToString() + "\n";
            }
            else
            {
                resultDisplay.text += "???: " + myMonster.stats[i].ToString() + "\n";
            }
        }
    }

    public void makeNewCurrentMonster()
    {
        currentMonster = myGenerator.GenerateMonster();
        writeMyMonster(currentMonster);
    }

    public void saveCurrentMonster()
    {
        myGenerator.saveMonsterToJson(currentMonster, monsterFileName);
    }
    public void LoadCurrentMonster()
    {

        MonsterGenerator.monsterInfo myNewMonster = myGenerator.loadMonsterFromJson(monsterFileName);
        if (myNewMonster != null)
        {
            currentMonster = myNewMonster;
            writeMyMonster(currentMonster);
        }
        else
        {
            resultDisplay.text = monsterFileName + " not found!\nMake sure a file with that name exists in your GeneratedMonsters folder.";
        }
    }

    public void updateStatTotalFromTextInput()
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
            myGenerator.updateStatTotal(result);
        }
        else
        {
            Debug.Log(result);
            statTotalInput.text = myGenerator.statTotal.ToString();
        }
    }

    public void updateFileNameFromTextInput()
    {
        Debug.Log("running updateFileNameFromTextInput");
        monsterFileName = fileNameInput.text;
        Debug.Log(monsterFileName);
    }
}
