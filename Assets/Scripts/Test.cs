using UnityEngine;
using System.Collections;
using NAudio.Midi;
using UnityEngine.UI;

public class Test : MonoBehaviour {

    public Text console;
    public RectTransform grid;

    float ticksPerMinute = 1f;
    private float musicLength = 1f;

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
        var ev = mf.Events[0][0];
        long highestTick = -1;
        int bpm = 120;
        int tempo_count = 0;
        
        foreach (var channel in mf.Events)
        {
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

                }
            }
        }

        ticksPerMinute = bpm * mf.DeltaTicksPerQuarterNote;
        musicLength = ((float)highestTick / ticksPerMinute) * 60;

        Debug.Log(string.Format("music length = {0}, bpm = {1}", musicLength, bpm));
    }

    public void Load()
    {
        Debug.Log("test load");
        //MidiFile mf = new MidiFile(@"D:\downloads\LovetheWayYouLie.mid");
        //AudioFileInspector.MidiFileInspector mf = new AudioFileInspector.MidiFileInspector();
        string fileName = AskForFileName();
        LoadMidiFile(fileName);
    }
}
