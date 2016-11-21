using UnityEngine;
using System.Collections;
using NAudio.Midi;
using UnityEngine.UI;
using System.Collections.Generic;

public class Test : MonoBehaviour {



    public Text console;
    public RectTransform grid;
    public RectTransform buttonPrefab;
    public Scrollbar scrollBar;

    float ticksPerSecond = 1f;
    private float musicLength = 1f;
    MidiFile midiFile;
    List<Beat> beats = new List<Beat>();
    float tickToPixelRatio = 1f;
    ButtonPool buttonPool;
    Dictionary<int, BeatButton> shownButtons;
    int shownStart;
    int shownCount;

	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
        if (buttonPool != null)
            ShowButtons(scrollBar.value);
	}

    string AskForFileName()
    {
        return @"D:\downloads\LovetheWayYouLie.mid";
    }

    void LoadMidiFile(string fileName)
    {
        MidiFile mf = new MidiFile(fileName);
        long highestTick = -1;
        int bpm = 120;
        int tempo_count = 0;
        beats.Clear();
        
        for (int i = 0; i < mf.Events.Tracks; i ++)
        {
            var channel = mf.Events[i];
            foreach (var eve in channel)
            {
                highestTick = (eve.AbsoluteTime > highestTick) ? eve.AbsoluteTime : highestTick;
                if (eve.CommandCode == MidiCommandCode.MetaEvent)
                {
                    //Debug.Log(eve.Channel+":"+eve);
                    if (eve is TempoEvent)
                    {
                        tempo_count++;
                        if (tempo_count <= 1)
                            bpm = (60000000 / ((TempoEvent)eve).MicrosecondsPerQuarterNote);
                        else
                        {
                            Debug.LogError("BPM has changed twice!");
                        }
                    }
                }
                else if (eve.CommandCode == MidiCommandCode.NoteOn)
                {
                    NoteOnEvent noteOnEvent = eve as NoteOnEvent;
                    Beat beat = new Beat();
                    beat._event = noteOnEvent;
                    beat.trackIndex = i;
                    beats.Add(beat);
                }
            }
        }

        ticksPerSecond = bpm * mf.DeltaTicksPerQuarterNote / 60f;
        musicLength = highestTick / ticksPerSecond;

        Debug.Log(string.Format("music length = {0}, bpm = {1}", musicLength, bpm));
        beats.Sort((a, b) => a._event.AbsoluteTime.CompareTo(b._event.AbsoluteTime));
        Debug.Log(string.Format("beat count = {0}, last beat time = {1}, last beat = {2}", 
            beats.Count, beats[beats.Count-1]._event.AbsoluteTime / ticksPerSecond,
            beats[beats.Count - 1]._event));
    }

    void SetupUI()
    {
        shownButtons = new Dictionary<int, BeatButton>();
        buttonPool = new ButtonPool(buttonPrefab);
        tickToPixelRatio = 1f;
        var size = grid.sizeDelta;
        size.x = beats[beats.Count - 1].Time / tickToPixelRatio;
        grid.sizeDelta = size;
        scrollBar.value = 0;
        ShowButtons(scrollBar.value);

        grid.parent.GetComponent<RectTransform>();
    }

    void GetShowWindow(float scrollBarValue, out int window_start, out int window_length)
    {
        window_start = -1;
        window_length = 0;

        
        float shownLength = grid.transform.parent.GetComponent<RectTransform>().sizeDelta.x * tickToPixelRatio;
        float shownBegin = (grid.sizeDelta.x - shownLength) * scrollBarValue * tickToPixelRatio;
        //Debug.Log(string.Format("shownLength = {0}, grid.sizeDelta.x = {1}, showBegin = {2}", shownLength, grid.sizeDelta.x, shownBegin));
        
        for (int i = 0; i < beats.Count; i ++)
        {
            var beat = beats[i];
            if (beat.Time >= shownBegin && beat.Time <= shownBegin + shownLength)
            {
                if (window_start == -1)
                {
                    window_start = i;
                }
                window_length++;
            }
        }

        //Debug.Log(string.Format("window start = {0}, window length = {1}, shown_start = {2}, shown_count = {3}", 
        //    window_start, window_length, shownStart, shownCount));
    }

    int Min(int a, int b)
    {
        return (a > b) ? b : a;
    }
    int Max(int a, int b)
    {
        return (a > b) ? a : b;
    }

    void ShowButton(BeatButton button)
    {
        button.GetComponent<RectTransform>().SetParent(grid);
        //button.transform.parent = grid.transform.parent;
        var beat = beats[button.beatIndex];
        var trans = button.GetComponent<RectTransform>();
        var pos = buttonPrefab.anchoredPosition3D;
        var size = buttonPrefab.sizeDelta;
        pos.x = beat.Time / tickToPixelRatio;
        
        trans.anchoredPosition3D = pos;
        trans.sizeDelta = size;
        trans.gameObject.SetActive(true);
    }

    void ShowButtons(float scrollBarValue)
    {
        int window_start, window_length;
        GetShowWindow(scrollBarValue, out window_start, out window_length);
        for (int i = shownStart; i < shownStart+shownCount; i ++ )
        {
            if (i < window_start || i >= window_start + window_length)
            {
                if (shownButtons.ContainsKey(i))
                {
                    buttonPool.ReturnButton(shownButtons[i]);
                    shownButtons.Remove(i);
                }
            }
        }

        for (int i = window_start; i < window_start + window_length; i ++)
        {
            if (!shownButtons.ContainsKey(i))
            {
                BeatButton button = buttonPool.BorrowButton(i);
                ShowButton(button);
                shownButtons.Add(i, button);
            }
        }

        shownStart = window_start;
        shownCount = window_length;
    }

    public void Load()
    { 
        Debug.Log("test load");
        //MidiFile mf = new MidiFile(@"D:\downloads\LovetheWayYouLie.mid");
        //AudioFileInspector.MidiFileInspector mf = new AudioFileInspector.MidiFileInspector();
        string fileName = AskForFileName();
        LoadMidiFile(fileName);
        SetupUI();
    }
}
