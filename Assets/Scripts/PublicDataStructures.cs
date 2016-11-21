using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NAudio.Midi;

public class Beat
{
    public NoteOnEvent _event;
    public int trackIndex;
    public int Time
    {
        get
        {
            return (int)_event.AbsoluteTime;
        }
    }
}

public class ButtonPool
{
    const int MAX_BUTTONS = 2000;
    int steps;
    List<BeatButton> buttons;
    RectTransform template;

    public ButtonPool(RectTransform _template)
    {
        buttons = new List<BeatButton>();
        steps = 100;
        template = _template;
    }

    BeatButton NewButton()
    {
        var go = GameObject.Instantiate(template.gameObject) as GameObject;
        var rectTrans = go.GetComponent<RectTransform>();
        rectTrans.transform.SetParent(GameObject.Find("Game").GetComponent<Test>().grid);
        rectTrans.anchoredPosition3D = template.anchoredPosition3D;
        rectTrans.sizeDelta = template.sizeDelta;
        go.SetActive(false);

        var beatButton = go.GetComponent<BeatButton>();
        beatButton.beatIndex = -1;

        return beatButton;
    }

    BeatButton AddButtons(int count)
    {
        if (buttons.Count + count > MAX_BUTTONS)
        {
            Debug.LogError("MAX BUTTONS REACHED");
            return null;
        }

        BeatButton firstOne = null;
        int startID = buttons.Count;
        for (int i = startID; i < startID + count; i ++)
        {
            var newButton = NewButton();
            buttons.Add(newButton);
            if (firstOne == null)
                firstOne = newButton;
        }

        return firstOne;
    }

    public BeatButton BorrowButton(int beatIndex)
    {
        for (int i = 0; i < buttons.Count; i ++)
        {
            var button = buttons[i];
            if (button.beatIndex == -1)
            {
                button.beatIndex = beatIndex;
                return button;
            }
        }
        var newButton = AddButtons(steps);
        newButton.beatIndex = beatIndex;
        return newButton;
    }

    int FreeButtons()
    {
        int count = 0;
        for (int i = 0; i < buttons.Count; i ++)
        {
            var button = buttons[i];
            if (button.beatIndex == -1)
            {
                count++;
            }
        }

        return count;
    }

    public void ReturnButton(BeatButton button)
    {
        button.beatIndex = -1;
        button.gameObject.SetActive(false);
        //Debug.Log(string.Format("free buttons = {0}, buttons = {1}", FreeButtons(), buttons.Count));
    }
}