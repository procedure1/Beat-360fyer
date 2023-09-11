using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;

namespace Stx.ThreeSixtyfyer
{
    public enum SpawnRotationEventType //v3
    {
        Early = 1,
        Late
    }

    public enum BeatmapEventData //v2 should be called BeatmapEventData since rotations and lighting events are all part of the same thing.
    {
        Early = 14,
        Late = 15
    }

    public enum NoteCutDirection //v2 & v3
    {
        Up,
        Down,
        Left,
        Right,
        UpLeft,
        UpRight,
        DownLeft,
        DownRight,
        Any,
        None
    }

    public enum NoteLineLayer //v2 & v3
    {
        Base,
        Upper,
        Top
    }

    public enum ColorType //v2 & v3
    {
        ColorA = 0,//Red left
        ColorB = 1,//Blue right
        None = -1
    }
    public enum NoteType//v2 & 3
    {
        NoteA = 0,//Red left
        NoteB = 1,//Blue right
        GhostNote = 2,
        Bomb = 3,
        None = -1
    }
    public enum GameplayType //v3
    {
        Normal,
        Bomb,
        BurstSliderHead,
        BurstSliderElement,
        BurstSliderElementFill
    }

    public enum BeatmapObjectType //v2
    {
        Note = 0,
        Obstacle = 2,
        Waypoint = 3,
        None = -1
    }

    public enum ObstacleType //v2
    {
        FullHeight,
        Top
    }

    [Serializable]
    public class BeatMapData//base for v2 & v3 maps
    {
        [JsonProperty("_version")]
        public string Version { get; set; }

        private int MajorVersion = 0;

        [JsonProperty("_events")]
        public List<BeatMapEvent> Events { get; set; }

        [JsonProperty("_notes")]
        public List<BeatMapNote> Notes { get; set; }

        [JsonProperty("_obstacles")]
        public List<BeatMapObstacle> Obstacles { get; set; }

        [JsonProperty("_customData", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public object CustomData { get; set; }

        public BeatMapData() { }

        public BeatMapData(BeatMapData other)
        {
            Version = other.Version;
            Events = new List<BeatMapEvent>(other.Events);
            Notes = new List<BeatMapNote>(other.Notes);
            Obstacles = new List<BeatMapObstacle>(other.Obstacles);
            CustomData = other.CustomData;

            // Split the mapVersion string by '.' to extract major version number.
            string[] versionParts = Version.Split('.');
            if (versionParts.Length > 0 && int.TryParse(versionParts[0], out int majorVersion))
            {
                MajorVersion = majorVersion;

                // Check if MajorVersion is not 2 or 3
                if (MajorVersion != 2 && MajorVersion != 3)
                {
                    throw new Exception("Unsupported major version: " + MajorVersion);
                }
            }
            else
            {
                // Handle the case where parsing the major version fails.
                throw new Exception("Invalid version format: " + Version);
            }
        }

        public void AddRotationEvent(float time, int rotation, int type)
        {
            if (rotation == 0)
                return;

            if (MajorVersion == 2)
                AddRotationEventV2(time, rotation, type);
            else
                AddRotationEventV3(time, rotation, type);
        }
        public void AddRotationEventV2(float time, int rotation, int type = 15)
        {
            BeatmapEventData rotationEvent;

            if (type == 14)
                rotationEvent = BeatmapEventData.Early;
            else
                rotationEvent = BeatmapEventData.Late;

            Events.Add(new BeatMapEventV2()
            {
                time = time,
                type = rotationEvent,
                value = rotation
            });
        }
        public void AddRotationEventV3(float time, int rotation, int type = 2)
        {
            SpawnRotationEventType rotationEvent;

            //If v2 rotation event is sent here to convert it to v3 
            if (type == 1)
                rotationEvent = SpawnRotationEventType.Early;
            else
                rotationEvent = SpawnRotationEventType.Late;

            Events.Add(new BeatMapEventV3()
            {
                time = time,
                spawnRotationEventType = rotationEvent, // Update this to match version 3's enum
                rotation = rotation
            });
        }
        public void AddWall(float time, int noteLineIndex, ObstacleType type, int noteLineLayer, float duration, int width, int height = 5)
        {
            if (MajorVersion == 2)
                AddWallV2(time, noteLineIndex, type, noteLineLayer, duration, width, height = 5);
            else
                AddWallV3(time, noteLineIndex, type, noteLineLayer, duration, width, height = 5);
        }
        public void AddWallV2(float time, int noteLineIndex, ObstacleType type, int noteLineLayer, float duration, int width, int height = 5)
        {
            Obstacles.Add(new BeatMapObstacleV2()
            {
                time = time,
                noteLineIndex = noteLineIndex,
                type = type,
                duration = duration,
                width = width
            });
        }

        public void AddWallV3(float time, int noteLineIndex, ObstacleType type, int noteLineLayer, float duration, int width, int height = 5)
        {
            //If v2 obstacle is sent here to convert it to v3
            if ((int)type == 0)//Fullheight
            {
                noteLineLayer = 0;
            }
            else if ((int)type == 1)//Top
            {
                noteLineLayer = 2;//BW check this!
            }

            Obstacles.Add(new BeatMapObstacleV3()
            {
                time = time,
                noteLineIndex = noteLineIndex,
                noteLineLayer = noteLineLayer,
                duration = duration,
                width = width,
                height = height
            });
        }
        public void Sort()
        {
            if (Events != null)
                Events = Events.OrderBy((e) => e.time).ToList();
            if (Notes != null)
                Notes = Notes.OrderBy((e) => e.time).ToList();
            //if (Sliders != null)
                //Sliders = Sliders.OrderBy((e) => e.headTime).ToList();
            if (Obstacles != null)
                Obstacles = Obstacles.OrderBy((e) => e.time).ToList();
            //if (Waypoints != null)
            //Waypoints = Waypoints.ToList();
        }
    }

    [Serializable]
    public class BeatMapEvent
    {
        [JsonProperty("_time")]
        public float time { get; set; }//used by Sort method

        [JsonProperty("_type")]
        public BeatmapEventData type { get; set; }

        [JsonProperty("_value")]
        public int value { get; set; }
    }

    [Serializable]
    public class BeatMapEventV2 : BeatMapEvent
    {

    }

    [Serializable]
    public class BeatMapEventV3 : BeatMapEvent
    {
        [JsonProperty("b")]
        public float time { get; set; }//beat

        [JsonProperty("e")]
        public SpawnRotationEventType spawnRotationEventType { get; set; }//type

        [JsonProperty("r")]
        public int rotation { get; set; }//rotation
    }

    [Serializable]
    public class BeatMapNote
    {
        [JsonProperty("_time")]
        public float time { get; set; }

        [JsonProperty("_lineIndex")]
        public int noteLineIndex { get; set; }

        [JsonProperty("_lineLayer")]
        public int noteLineLayer { get; set; }

        [JsonProperty("_type")]
        public NoteType type { get; set; }

        [JsonProperty("_cutDirection")]
        public NoteCutDirection noteCutDirection { get; set; }
    }

    [Serializable]
    public class BeatMapNoteV2 : BeatMapNote
    {

    }

    [Serializable]
    public class BeatMapNoteV3 : BeatMapNote
    {
        [JsonProperty("b")]
        public float time { get; set; }//beat

        [JsonProperty("x")]
        public int noteLineIndex { get; set; }

        [JsonProperty("y")]
        public int noteLineLayer { get; set; }

        [JsonProperty("c")]
        public NoteType color { get; set; }//0 Red 1 Blue

        [JsonProperty("d")]
        public NoteCutDirection cutDirection { get; set; }

        [JsonProperty("a")]
        public int angleOffset { get; set; }//An integer number which represents the additional counter-clockwise angle offset applied to the note's cut direction in degrees

    }
    [Serializable]
    public class BeatMapSlider
    {
        [JsonProperty("_colorType")]
        public int colorType { get; set; }

        [JsonProperty("_headTime")]
        public float headTime { get; set; }

        [JsonProperty("_headLineIndex")]
        public int headLineIndex { get; set; }

        [JsonProperty("_headLineLayer")]
        public int headLineLayer { get; set; }

        [JsonProperty("_headControlPointLengthMultiplier")]
        public float headControlPointLengthMultiplier { get; set; }

        [JsonProperty("_headCutDirection")]
        public int headCutDirection { get; set; }

        [JsonProperty("_tailTime")]
        public float tailTime { get; set; }

        [JsonProperty("_tailLineIndex")]
        public int tailLineIndex { get; set; }

        [JsonProperty("_tailLineLayer")]
        public int tailLineLayer { get; set; }

        [JsonProperty("_tailControlPointLengthMultiplier")]
        public float tailControlPointLengthMultiplier { get; set; }

        [JsonProperty("_tailCutDirection")]
        public int tailCutDirection { get; set; }

        [JsonProperty("_sliderMidAnchorMode")]
        public int sliderMidAnchorMode { get; set; }

    }

    [Serializable]
    public class BeatMapObstacle
    {
        [JsonProperty("_time")]
        public float time { get; set; }

        [JsonProperty("_lineIndex")]
        public int noteLineIndex { get; set; }

        [JsonProperty("_type")]
        public ObstacleType type { get; set; }

        [JsonProperty("_duration")]
        public float duration { get; set; }

        [JsonProperty("_width")]
        public int width { get; set; }
    }

    [Serializable]
    public class BeatMapObstacleV2 : BeatMapObstacle
    {

    }

    [Serializable]
    public class BeatMapObstacleV3 : BeatMapObstacle
    {
        [JsonProperty("b")]
        public float time { get; set; }//beat

        [JsonProperty("x")]
        public int noteLineIndex { get; set; }

        [JsonProperty("y")]
        public int noteLineLayer { get; set; }

        [JsonProperty("d")]
        public float duration { get; set; }

        [JsonProperty("w")]
        public int width { get; set; }

        [JsonProperty("h")]
        public int height { get; set; }
    }
}
