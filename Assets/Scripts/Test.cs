using UnityEngine;
using System.Collections;
using NAudio.Midi;
using UnityEngine.UI;
using System.Collections.Generic;

public class Test : MonoBehaviour {

    class Beat
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

    public Text console;
    public RectTransform grid;
    public GameObject buttonPrefab;
    public Scrollbar scrollBar;

    float ticksPerSecond = 1f;
    private float musicLength = 1f;
    MidiFile midiFile;
    List<Beat> beats = new List<Beat>();
    

	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
	    
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
        var size = grid.sizeDelta;
        size.x = beats[beats.Count - 1].Time;
        grid.sizeDelta = size;
        scrollBar.value = 0;

        
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
