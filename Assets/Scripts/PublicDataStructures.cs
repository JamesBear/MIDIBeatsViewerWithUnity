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
    const int MAX_BUTTONS = 1000;
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
        rectTrans.position = template.position;
        rectTrans.sizeDelta = template.sizeDelta;
        go.SetActive(false);

        return go.GetComponent<BeatButton>();
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

    public void ReturnButton(BeatButton button)
    {
        button.beatIndex = -1;
        button.gameObject.SetActive(false);
    }
}