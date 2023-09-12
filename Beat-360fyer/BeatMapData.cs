using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Controls;
using static Stx.ThreeSixtyfyer.BeatMapGenerator;

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
        private int MajorVersion = 0;

        //v2 elements-----------------------------------------------------------

        [JsonProperty("_version", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Version { get; set; }

        [JsonProperty("_events", DefaultValueHandling = DefaultValueHandling.Ignore)]//adding this will remove _events if its empty otherwise if its missing from the doc will say "null" if no customdata
        public List<BeatMapEvent> Events { get; set; }

        [JsonProperty("_notes", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<BeatMapNote> Notes { get; set; }

        [JsonProperty("_sliders", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public object Sliders { get; set; }//List<BeatMapSlider> Sliders { get; set; }//instroduced v2.6

        [JsonProperty("_obstacles", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<BeatMapObstacle> Obstacles { get; set; }

        [JsonProperty("_waypoints", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public object Waypoints { get; set; }//instroduced v2.2

        [JsonProperty("_customData", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public object CustomData { get; set; }

        //v3 elements ---------------------------------------------------------

        [JsonProperty("version", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string versionV3 { get; set; }

        [JsonProperty("bpmEvents", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public object bpmEvents { get; set; }

        [JsonProperty("rotationEvents", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public object rotationEvents { get; set; }

        [JsonProperty("colorNotes", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public object colorNotes { get; set; }

        [JsonProperty("bombNotes", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public object bombNotes { get; set; }

        [JsonProperty("obstacles", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public object obstacles { get; set; }

        [JsonProperty("sliders", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public object sliders { get; set; }

        [JsonProperty("burstSliders", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public object burstSliders { get; set; }

        [JsonProperty("waypoints", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public object waypoints { get; set; }

        [JsonProperty("basicBeatmapEvents", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public object basicBeatmapEvents { get; set; }

        [JsonProperty("colorBoostBeatmapEvents", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public object colorBoostBeatmapEvents { get; set; }

        [JsonProperty("lightColorEventBoxGroups", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public object lightColorEventBoxGroups { get; set; }

        [JsonProperty("lightRotationEventBoxGroups", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public object lightRotationEventBoxGroups { get; set; }

        [JsonProperty("lightTranslationEventBoxGroups", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public object lightTranslationEventBoxGroups { get; set; }

        [JsonProperty("basicEventTypesWithKeywords", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public object basicEventTypesWithKeywords { get; set; }

        [JsonProperty("useNormalEventsAsCompatibleEvents", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool useNormalEventsAsCompatibleEvents { get; set; }

        [JsonProperty("customData", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public object customData { get; set; }

        //public bool containsCustomWalls;

        public BeatMapData() { }

        public BeatMapData(BeatMapData other)
        {
            //v2 properties ---------------------------------
            Version = other.Version;
            Events = new List<BeatMapEvent>(other.Events);
            Notes = new List<BeatMapNote>(other.Notes);
            Sliders = other.Sliders;//not altering these //new List<BeatMapSlider>(other.Sliders);
            Obstacles = new List<BeatMapObstacle>(other.Obstacles);
            Waypoints = other.Waypoints;//not altering these // new List<BeatMapWaypoint>(other.Waypoints);
            CustomData = other.CustomData;//not altering these

            //containsCustomWalls = ContainsCustomWalls();//crashes exe to use this. tried it in BeatMap360Generator.cs also. same result.
            /*
            //v3 properties ---------------------------------
            versionV3 = other.Version;
            bpmEvents = other.bpmEvents;
            rotationEvents = other.rotationEvents;
            colorNotes = other.colorNotes;
            bombNotes = other.bombNotes;
            obstacles = other.obstacles;
            sliders = other.sliders;
            burstSliders = other.burstSliders;
            waypoints = other.waypoints;
            basicBeatmapEvents = other.basicBeatmapEvents;
            colorBoostBeatmapEvents = other.colorBoostBeatmapEvents;
            lightColorEventBoxGroups = other.lightColorEventBoxGroups;
            lightRotationEventBoxGroups = other.lightRotationEventBoxGroups;
            lightTranslationEventBoxGroups = other.lightTranslationEventBoxGroups;
            useNormalEventsAsCompatibleEvents = other.useNormalEventsAsCompatibleEvents;
            customData = other.customData;
            */

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
            if (Obstacles != null)
                Obstacles = Obstacles.OrderBy((e) => e.time).ToList();
            //don't need the other items here (sliders, waypoints,etc)
        }
        /*
        //crashes exe if use this
        public bool ContainsCustomWalls()//checks for _position in Obstacles>CustomData
        {
            string propertyNameToSearch = "_position";

            // Count the occurrences of the property in the BeatMapData.
            int propertyCount = CountPropertyOccurrences(this, propertyNameToSearch);

            // Check if more than 12 occurrences were found.
            bool isMoreThan12 = propertyCount > 12;

            Console.WriteLine("Is More Than 12: " + isMoreThan12);

            int CountPropertyOccurrences(BeatMapData beatMap, string propertyName)
            {
                int count = 0;

                // Check if BeatMapData has events.
                if (beatMap?.Obstacles != null)
                {
                    foreach (var beatMapEvent in beatMap.Obstacles)
                    {
                        // Check if the event has _customData.
                        if (beatMapEvent?.CustomData != null)
                        {
                            // Convert the _customData object to a dictionary.
                            var customDataDictionary = GetPropertyDictionary(beatMapEvent.CustomData);

                            // Check if the _customData dictionary contains the specified property name.
                            if (customDataDictionary.ContainsKey(propertyName))
                            {
                                count++;
                            }
                        }
                    }
                }

                return count;
            }

            Dictionary<string, object> GetPropertyDictionary(object obj)
            {
                // Use reflection to get the properties and their values from the object.
                var propertyDictionary = new Dictionary<string, object>();

                if (obj != null)
                {
                    var type = obj.GetType();
                    var properties = type.GetProperties();

                    foreach (var property in properties)
                    {
                        var propertyName = property.Name;
                        var propertyValue = property.GetValue(obj, null);
                        propertyDictionary[propertyName] = propertyValue;
                    }
                }

                return propertyDictionary;
            }
            return isMoreThan12;
        }*/
    }

    [Serializable]
    public class BeatMapEvent//everything in here will be added to the output map
    {
        [JsonProperty("_customData", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public object CustomData { get; set; }

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
        [JsonProperty("_customData", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public object CustomData { get; set; }

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
    /*
    //Not needed since just passing sliders with changes
    [Serializable]
    public class BeatMapSlider
    {
        [JsonProperty("_customData", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public object CustomData { get; set; }

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
    */

    [Serializable]
    public class BeatMapObstacle
    {
        [JsonProperty("_customData", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public object CustomData { get; set; }

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
