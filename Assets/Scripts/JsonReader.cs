using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class JsonReader : MonoBehaviour
{
    //i did the non-default working out of facilitating my testing. it is NOT intended to work out of the editor environment. for build purposes, this bool should be left as false
    public bool defaultWorking = false;

    public Text titleLabel;
    public GameObject loadButton;
    public RectTransform contentContainer;

    public GameObject columnPrefab;

    private string title = "";
    private CharacteristicList[] headers;

    private int points = 0;
    private bool pastHeaders = false;
    private bool entryChange = false;
    private int entryCount = 0;

    private int columnCount = 0;
    private int rowCount = 0;

    public struct CharacteristicList
    {
        public string charaName;
        public List<string> values;
        public int lastIndex;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (defaultWorking)
            LoadAndBuild();
        else
        {
            loadButton.SetActive(true);
            titleLabel.gameObject.SetActive(false);
        }
    }

    //don't quite get why we need a json when the variable number and names are subject to change, so i'll make the method so it's a standard file reading based on a json structure
    private void LoadJson()
    {
        string filePath = Application.streamingAssetsPath + "/JsonChallenge.json";

#if UNITY_EDITOR
        if (!defaultWorking)
            filePath = EditorUtility.OpenFilePanel("Load table json file", Application.streamingAssetsPath, "json");
#endif
        pastHeaders = false;

        if (!File.Exists(filePath))
        {
            if (defaultWorking)
                Debug.Log("file not found on StreamingAssets folder.");
            else
                Debug.Log("file not found on given path.");
            
            titleLabel.gameObject.SetActive(false);

            return;
        }
        
        StreamReader sr = new StreamReader(filePath);

        List<string> headerList = new List<string>();

        while(true)
        {
            string line = sr.ReadLine();
            if (line == null)
                break;

            line = ClearSpaces(line);
            if (line.Length < 3)
            {
                if (points <= 2)
                    continue;
                else if (pastHeaders)
                {
                    entryChange = true;
                    continue;
                }
                else
                {
                    headers = new CharacteristicList[headerList.Count];

                    for(int i = 0; i < headerList.Count; i++)
                    {
                        headers[i].charaName = headerList[i];
                        headers[i].values = new List<string>();
                        headers[i].lastIndex = 0;
                    }

                    entryCount = -1;
                    entryChange = true;
                    pastHeaders = true;
                    continue;
                }
            }

            if (!pastHeaders)
            {
                //extract title
                if (points == 0)
                {
                    line = line.Substring(10);
                    title = line.Substring(0, line.IndexOf("\""));
                }
                else if (points >= 2 && !pastHeaders)
                {
                    line = line.Substring(line.IndexOf("\"") + 1);
                    line = line.Substring(0, line.IndexOf("\""));
                    headerList.Add(line);
                }
                points++;
            }
            else
            {
                if (entryCount >= 0)
                {
                    if (entryChange)
                    {
                        entryCount++;
                        entryChange = false;
                    }

                    AddLineToColumn(line, entryCount);
                }
                else
                    entryCount++;
            }
        }

        //we fill up possible missing data at the column ends
        int maxEntryCount = int.MinValue;
        for(int i = 0; i < headers.Length; i++)
        {
            int score = headers[i].values.Count;
            if (score > maxEntryCount)
                maxEntryCount = score;
        }

        for (int i = 0; i < headers.Length; i++)
        {
            for (int m = 0; m < maxEntryCount; m++)
            {
                if (m >= headers[i].values.Count)
                {
                    headers[i].values.Add("no data");
                    headers[i].lastIndex++;
                }
            }
        }

        columnCount = headers.Length;
        rowCount = maxEntryCount;

        /*
        //debug test
        int debugIndex = 2;
        for (int m = 0; m < headers[debugIndex].lastIndex; m++)
        {
            Debug.Log(headers[debugIndex].values[m]);
        }
        */
    }

    private void AddLineToColumn(string data, int entryCall)
    {
        int entry = entryCall - 1;

        string entryID = data.Substring(1);
        int qtidx = entryID.IndexOf("\"");
        entryID = entryID.Substring(0, entryID.IndexOf("\""));

        string value = data.Substring(qtidx + 2);
        value = value.Substring(value.IndexOf("\"") + 1);
        value = value.Substring(0, value.IndexOf("\""));

        for(int i = 0; i < headers.Length; i++)
        {
            if (headers[i].charaName.Equals(entryID))
            {
                if(entry > headers[i].lastIndex)
                {
                    int diff = entry - headers[i].lastIndex;
                    for(int k = 0; k < diff; k++)
                    {
                        headers[i].values.Add("no data");
                        headers[i].lastIndex++;
                    }
                }

                headers[i].values.Add(value);
                headers[i].lastIndex++;
                break;
            }
        }
    }

    private void BuildTable()
    {
        if (!defaultWorking)
            titleLabel.gameObject.SetActive(true);
        
        titleLabel.text = title;

        if (columnCount == 0)
            return;

        float cellWidth = 0f;
        float cellHeight = 0f;

        for (int c = 0; c < columnCount; c++)
        {
            GameObject columnObj = Instantiate(columnPrefab);

            if (c == 0) {
                RectTransform cTrans = columnObj.GetComponent<RectTransform>();
                cellWidth = cTrans.rect.width;
                cellHeight = cTrans.rect.height;

                contentContainer.sizeDelta = new Vector2(cellWidth * columnCount, cellHeight * (rowCount + 1));
            }

            columnObj.transform.SetParent(contentContainer.transform);
            columnObj.GetComponent<Text>().text = headers[c].charaName;

            RectTransform columnRect = columnObj.GetComponent<RectTransform>();
            columnRect.localScale = Vector3.one;
            columnRect.anchoredPosition = new Vector2(c * cellWidth, 0f);

            if (rowCount == 0)
            {
                Destroy(columnObj.transform.GetChild(0).gameObject);
                continue;
            }

            GameObject cellChild = columnObj.transform.GetChild(0).gameObject;
            cellChild.GetComponent<Text>().text = headers[c].values[0];
            RectTransform rf = cellChild.GetComponent<RectTransform>();
            rf.anchoredPosition = new Vector2(0f, -1f * cellHeight);

            for(int i = 1; i < rowCount; i++)
            {
                GameObject nuChild = Instantiate(cellChild);
                nuChild.transform.SetParent(columnObj.transform);

                nuChild.GetComponent<Text>().text = headers[c].values[i];
                rf = nuChild.GetComponent<RectTransform>();
                rf.localScale = Vector3.one;
                rf.anchoredPosition = new Vector2(0f, -1f * (i + 1) * cellHeight);
            }
        }
    }

    public void LoadAndBuild()
    {
        if (!defaultWorking)
            ResetTable();

        //we read and adapt the json table file
        LoadJson();

        //we build the UI table
        BuildTable();
    }

    private string ClearSpaces(string line)
    {
        return line.TrimStart(' ', '\t');
    }

    private void ResetTable()
    {
        for (int i = contentContainer.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(contentContainer.transform.GetChild(i).gameObject);
        }

        title = "";
        headers = null;

        points = 0;
        pastHeaders = false;
        entryChange = false;
        entryCount = 0;

        columnCount = 0;
        rowCount = 0;
    }
}
