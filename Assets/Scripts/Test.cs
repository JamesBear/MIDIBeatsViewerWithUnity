using UnityEngine;
using System.Collections;
using NAudio.Midi;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;
using System.IO;

public class Test : MonoBehaviour {



    public Text console;
    public RectTransform grid;
    public RectTransform buttonPrefab;
    public RectTransform cursor;
    public Scrollbar scrollBar;
    public List<bool> trackEnabled;
    public Slider musicSlider;
    public bool PauseOrPlay;
    public RectTransform togglePrefab;
    public RectTransform tracksEnabledRoot;
    public Text timeLabelMidi;
    public Text timeLabelOgg;
    

    float ticksPerSecond = 1f;
    private float musicLength = 1f;
    MidiFile midiFile;
    List<Beat> beats = new List<Beat>();
    float tickToPixelRatio = 1f;
    int bpm;
    ButtonPool buttonPool;
    Dictionary<int, BeatButton> shownButtons;
    int shownStart;
    int shownCount;
    int selectedButton = -1;
    Color defaultColor = new Color(161f / 255, 163f / 255, 0f);
    Color beatColor = Color.green;
    AudioSource audioSource;
    string fileName;
    bool autoNextPage = true;
    List<BeatTypeInput> beatInputs = new List<BeatTypeInput> { new BeatTypeInput(KeyCode.A, KeyCode.LeftShift, 5, "_A"),
        new BeatTypeInput(KeyCode.W, KeyCode.LeftShift, 6, "_W"), new BeatTypeInput(KeyCode.D, KeyCode.LeftShift, 7, "_D"),
        new BeatTypeInput(KeyCode.S, KeyCode.LeftShift, 8, "_S"), new BeatTypeInput(KeyCode.A, KeyCode.None, 1, "a"),
        new BeatTypeInput(KeyCode.W, KeyCode.None, 2, "w"), new BeatTypeInput(KeyCode.D, KeyCode.None, 3, "d"),
        new BeatTypeInput(KeyCode.S, KeyCode.None, 4, "s"), new BeatTypeInput(KeyCode.Backspace, KeyCode.None, 0, "")};


    // Use this for initialization
    void Start () {

	}
	
	// Update is called once per frame
	void Update () {
        UpdateInput();
        UpdateUI();
    }

    string AskForFileName()
    {
        if (File.Exists("test.txt"))
        {
            string str = File.ReadAllText("test.txt").Trim();
            Debug.Log("Loaded path from test.txt: " + str);
            return str;
        }

        Debug.LogError("Can't find test.txt, using default path: " + @"D:\downloads\LovetheWayYouLie.mid");
        return @"D:\downloads\LovetheWayYouLie.mid";
    }

    void UpdateInput()
    {
        if (Input.GetKeyUp(KeyCode.LeftArrow))
        {
            if (selectedButton > 0)
                selectedButton--;
        }
        if (Input.GetKeyUp(KeyCode.RightArrow))
        {
            if (selectedButton < beats.Count - 1)
                selectedButton++;
        }

        foreach (var beatInput in beatInputs)
        {
            int beatType;
            string _name;
            if (beatInput.TryGetInputBeatType(out beatType, out _name))
            {
                ProcessBeatInput(beatType, _name);
                break;
            }
        }
    }

    void ProcessBeatInput(int beatType, string beatName)
    {
        if (shownButtons.ContainsKey(selectedButton))
        {
            BeatButton beatButton = shownButtons[selectedButton];
            Beat beat = beats[beatButton.beatIndex];
            beat.beatType = beatType;
            beat.beatName = beatName;
        }
    }


    void UpdateUI()
    {
        if (buttonPool != null)
        {
            ShowButtons(scrollBar.value);
            ShowCursor();
            ShowTimeLabel();
        }
        if (PauseOrPlay)
        {
            PauseOrPlay = false;
            PauseUnpause();
        }
    }

    void ShowTrackToggles()
    {
        for (int i = 0; i < trackEnabled.Count; i ++)
        {
            var child = tracksEnabledRoot.FindChild("track" + i);
            if (child != null)
            {
                child.GetComponent<Toggle>().isOn = trackEnabled[i];
            }
        }
    }

    string FormatSeconds(float seconds)
    {
        int secs = (int)seconds;
        int mins = secs / 60;
        secs = secs % 60;

        return mins + ":" + secs;
    }

    void ShowTimeLabel()
    {
        if (audioSource != null)
        {
            float playTime = audioSource.time;
            float audioLength = audioSource.clip.length;
            string ogg_time = FormatSeconds(playTime) + "/" + FormatSeconds(audioLength);
            timeLabelOgg.text = ogg_time;

            float midi_play_time = playTime;
            float midiLength = musicLength;
            string midi_time = FormatSeconds(midi_play_time) + "/" + FormatSeconds(midiLength);
            timeLabelMidi.text = midi_time;
        }
    }

    void ShowCursor()
    {
        if (audioSource != null)
        {
            if (audioSource.time < musicLength)
                musicSlider.value = audioSource.time;

            float ticks = audioSource.time* ticksPerSecond;
            float pixel = ticks / tickToPixelRatio;
            var pos = cursor.anchoredPosition3D;
            pos.x = pixel;
            cursor.anchoredPosition3D = pos;

            if (!audioSource.isPlaying || !autoNextPage)
            {
                return;
            }

            float shownLength = grid.transform.parent.GetComponent<RectTransform>().sizeDelta.x * tickToPixelRatio;
            float shownBegin = (grid.sizeDelta.x - shownLength) * scrollBar.value * tickToPixelRatio;

            if (pos.x < shownBegin || pos.x > shownLength + shownBegin)
            {
                scrollBar.value = pos.x / (grid.sizeDelta.x - shownLength) / tickToPixelRatio;
            }
        }
    }

    void LoadMidiFile(string fileName)
    {
        MidiFile mf = new MidiFile(fileName);
        midiFile = mf;
        long highestTick = -1;
        bpm = 120;
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
                        {
                            bpm = (60000000 / ((TempoEvent)eve).MicrosecondsPerQuarterNote);

                            // calculate ticks per second!
                            ticksPerSecond = bpm * mf.DeltaTicksPerQuarterNote / 60f;
                        }
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
                    beat.Time = (int)noteOnEvent.AbsoluteTime;
                    beat.trackIndex = i;
                    beat.estimatedTime = beat.Time / ticksPerSecond;
                    beats.Add(beat);
                }
            }
        }
        musicLength = highestTick / ticksPerSecond;



        Debug.Log(string.Format("music length = {0}, bpm = {1}", musicLength, bpm));
        beats.Sort((a, b) => a._event.AbsoluteTime.CompareTo(b._event.AbsoluteTime));
        SetBeatIndexes();
        Debug.Log(string.Format("beat count = {0}, last beat time = {1}, last beat = {2}", 
            beats.Count, beats[beats.Count-1]._event.AbsoluteTime / ticksPerSecond,
            beats[beats.Count - 1]._event));
    }

    void SetBeatIndexes()
    {
        for (int i = 0;i < beats.Count; i ++)
        {
            beats[i].beatIndex = i;
        }
    }

    void PlayClip(AudioClip clip)
    {
        if (audioSource == null)
        {
            GameObject go = new GameObject("AudioPlayer");
            audioSource = go.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    void InitializeTracksEnabled()
    {
        for (int i = 0; i < trackEnabled.Count; i ++)
        {
            var go = Instantiate(togglePrefab.gameObject, tracksEnabledRoot) as GameObject;
            go.name = "track" + i;
            var toggle = go.GetComponent<Toggle>();
            toggle.isOn = trackEnabled[i];
            toggle.GetComponentInChildren<Text>().text = i.ToString();
        }
    }

    void SetupUI()
    {
        trackEnabled = new List<bool>();
        for (int i = 0; i < midiFile.Tracks; i++)
        {
            trackEnabled.Add(true);
        }
        InitializeTracksEnabled();
        shownButtons = new Dictionary<int, BeatButton>();
        buttonPool = new ButtonPool(buttonPrefab);
        tickToPixelRatio = 1f;
        var size = grid.sizeDelta;
        size.x = beats[beats.Count - 1].Time / tickToPixelRatio;
        grid.sizeDelta = size;
        ShowButtons(scrollBar.value);
        scrollBar.value = 0;

        grid.parent.GetComponent<RectTransform>();
        musicSlider.maxValue = musicLength;
        musicSlider.minValue = 0;
        musicSlider.value = 0;
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

        button.GetComponentInChildren<Text>().text = beat.beatName;

        if (button.beatIndex == selectedButton)
            button.GetComponent<Image>().color = Color.white;
        else if (beat.beatType == 0)
            button.GetComponent<Image>().color = defaultColor;
        else
            button.GetComponent<Image>().color = beatColor;

        button.gameObject.SetActive(trackEnabled[beats[button.beatIndex].trackIndex]);
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
            BeatButton button;
            if (!shownButtons.ContainsKey(i))
            {
                button = buttonPool.BorrowButton(i);
                shownButtons.Add(i, button);
            }
            else
            {
                button = shownButtons[i];
            }

            ShowButton(button);
        }

        shownStart = window_start;
        shownCount = window_length;
    }

    public void Load()
    { 
        Debug.Log("test load");
        //MidiFile mf = new MidiFile(@"D:\downloads\LovetheWayYouLie.mid");
        //AudioFileInspector.MidiFileInspector mf = new AudioFileInspector.MidiFileInspector();
        fileName = AskForFileName();
        LoadMidiFile(fileName);
        SetupUI();
        LoadConfigFile();
        StartCoroutine(StartAudio(GetRelatedPath(fileName, ".ogg")));
    }

    public void OnClickButton(BeatButton button)
    {
        //if (selectedButton != -1 && shownButtons.ContainsKey(selectedButton))
        //{
        //    shownButtons[selectedButton].GetComponent<Image>().color = defaultColor;
        //}

        selectedButton = button.beatIndex;
        button.GetComponent<Image>().color = Color.white;
        var beat = beats[button.beatIndex];
        Debug.Log(string.Format("ticks = {0}, time = {1}", beat.Time, beat.Time / ticksPerSecond));
    }

    public void OnMusicProgressBarChanged()
    {
        if (Mathf.Abs(audioSource.time - musicSlider.value) > 0.5)
            audioSource.time = musicSlider.value;
    }

    public void PauseUnpause()
    {
        if (audioSource)
        {
            if (audioSource.isPlaying)
                audioSource.Pause();
            else
            {
                audioSource.Play();
            }
        }
    }

    public void Stop()
    {
        if (audioSource)
        {
            audioSource.Pause();
            audioSource.time = 0;
        }
    }

    string GetRelatedPath(string midi_path, string newExtension)
    {
        if (midi_path.Length > 4)
            midi_path = midi_path.Substring(0, midi_path.Length - 4);
        
        return midi_path + newExtension;
    }

    IEnumerator StartAudio(string audioFilePath)
    {
        WWW audioLoader = new WWW("file://" + audioFilePath);
        while (!audioLoader.isDone)
            yield return null;

        var clip = audioLoader.GetAudioClip(false);
        Debug.Log("Finish loading: " + audioFilePath);
        PlayClip(clip);
    }

    public void CheckBoxClicked(string checkBoxName, bool newValue)
    {
        if (checkBoxName.StartsWith("track"))
        {
            int trackID = int.Parse(checkBoxName.Substring(5));
            trackEnabled[trackID] = newValue;
        }
    }

    public void OnCheckAutoNextPage(Toggle toggle)
    {
        autoNextPage = toggle.isOn;
    }

    void LoadConfigFile()
    {
        string configPath = GetRelatedPath(fileName, ".txt");
        if (File.Exists(configPath))
        {
            string content = File.ReadAllText(configPath);
            ConfigObject configObj = new ConfigObject();
            configObj.LoadFromString(content);
            LoadFromConfigObject(configObj);

            Debug.Log("Successfully loaded config: " + configPath);
        }
        else
            Debug.Log("Config file doesn't exist:" + configPath);
    }

    public void SaveConfigFile()
    {
        string configPath = GetRelatedPath(fileName, ".txt");

        ConfigObject configObj = ToConfigObject();
        string content = configObj.ToString();
        File.WriteAllText(configPath, content);
        Debug.Log("Successfully written config to " + configPath);
    }

    public ConfigObject ToConfigObject()
    {
        ConfigObject configObj = new ConfigObject();
        configObj.beats = beats;
        configObj.bpm = bpm;
        configObj.deltaTicksPerQuarterNote = midiFile.DeltaTicksPerQuarterNote;
        configObj.ticksPerSecond = ticksPerSecond;
        configObj.trackEnabled = trackEnabled;

        return configObj;
    }

    public void LoadFromConfigObject(ConfigObject configObj)
    {
        trackEnabled = configObj.trackEnabled;
        foreach (var beat in configObj.beats)
        {
            var myBeat = beats[beat.beatIndex];
            if (beat.Time != myBeat.Time)
            {
                Debug.LogError(string.Format("Load beat error: index = {0}, myTime = {1}, theirTime = {2}", 
                    beat.beatIndex, myBeat.Time, beat.Time));
                continue;
            }
            myBeat.beatType = beat.beatType;
            myBeat.beatName = beat.beatName;
        }

        ShowTrackToggles();
    }
}
