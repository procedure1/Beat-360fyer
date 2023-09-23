using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

    public enum NoteCutDirection //v2 & v3 (see None in the code but the formating site only lists 0-8 and doesn't include none)
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
        GhostNote = 2,//or Unused
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
        public static int MajorVersion = 0;

        public bool ShouldSerializeMajorVersion()
        { return false; }//Hides from serization into the json file

        public static float AverageNPS = 0;

        public bool ShouldSerializeAverageNPS()
        { return false; }// Return true to include Events when MajorVersion is 2, false otherwise

        //v2 elements-----------------------------------------------------------

        [JsonProperty("_version", DefaultValueHandling = DefaultValueHandling.Ignore)]//adding this will remove _events if its empty otherwise if its missing from the doc will say "null" if no customdata
        public string Version { get; set; }

        [JsonProperty("_events", DefaultValueHandling = DefaultValueHandling.Ignore)]
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
        //v3 maps are converted to v2 in BeatMapData so they can be universally handled in the generator360. But the side effect is JSON will serialize _events, _notes and _obstacles into the .dat file since i'm using those 3 properties to convert v3. these hide them.

        public bool ShouldSerializeEvents()
        { return MajorVersion == 2; }// Return true to include Events when MajorVersion is 2, false otherwise

        public bool ShouldSerializeNotes()
        { return MajorVersion == 2; }// Return true to include Events when MajorVersion is 2, false otherwise

        public bool ShouldSerializeObstacles()
        { return MajorVersion == 2; }// Return true to include Events when MajorVersion is 2, false otherwise

        [JsonProperty("version", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string version { get; set; }

        [JsonProperty("bpmEvents", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public object bpmEvents { get; set; }

        [JsonProperty("rotationEvents", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<BeatMapRotationEvent> rotationEvents { get; set; }

        [JsonProperty("colorNotes", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<BeatMapColorNote> colorNotes { get; set; }

        [JsonProperty("bombNotes", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<BeatMapBombNote> bombNotes { get; set; }
        //public object bombNotes { get; set; }

        [JsonProperty("obstacles", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<BeatMapObstacleV3> obstacles { get; set; }

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
            // Split the mapVersion string by '.' to extract major version number.
            string[] versionParts;
            if (other.Version == null)
                versionParts = other.version.Split('.');
            else
                versionParts = other.Version.Split('.');

            if (versionParts != null && versionParts.Length > 0 && int.TryParse(versionParts[0], out int majorVersion))
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

            if (MajorVersion == 2)//v2 properties ---------------------------------
            {
                Version = other.Version;

                Events = new List<BeatMapEvent>(other.Events);
                Notes = new List<BeatMapNote>(other.Notes);
                Obstacles = new List<BeatMapObstacle>(other.Obstacles);

                //these could be left out of the if then since if they are null they will not appear. same for any like this in v3
                Sliders = other.Sliders;//not altering these
                Waypoints = other.Waypoints;//not altering these
                CustomData = other.CustomData;//not altering these
            }
            else//v3 properties ---------------------------------
            {
                version = other.version;
                bpmEvents = other.bpmEvents;

                //convert to v2 so can use universally in the Generator
                rotationEvents = other.rotationEvents;//there should be no rotation events unless the original map is 90degree
                Events = other.rotationEvents.Select(rotationEvent => new BeatMapEvent
                {
                    time = rotationEvent.beat,
                    type = (BeatmapEventData)((int)rotationEvent.spawnRotationEventType + 14),
                    value = rotationEvent.rotation
                }
                ).ToList();

                //convert to v2
                colorNotes = other.colorNotes;
                Notes = colorNotes.Select(colorNote => new BeatMapNote
                {
                    time = colorNote.beat,
                    noteLineIndex = colorNote.xPosition,
                    noteLineLayer = colorNote.yPosition,
                    type = colorNote.color,
                    noteCutDirection = colorNote.cutDirection
                }
                ).ToList();
                //Notes = new List<BeatMapNote>(other.colorNotes);  

                //convert to v2
                obstacles = other.obstacles;
                Obstacles = obstacles.Select(obstacles => new BeatMapObstacle
                {
                    time = obstacles.beat,
                    noteLineIndex = obstacles.xPosition,
                    type = (obstacles.yPosition == 2) ? ObstacleType.Top : ObstacleType.FullHeight,
                    duration = obstacles.d,
                    width = obstacles.w
                }
                ).ToList();
                //Obstacles = new List<BeatMapObstacle>(other.obstacles);

                //bombNotes = other.bombNotes;
                bombNotes = new List<BeatMapBombNote>(other.bombNotes);
                /*bombNotes = bombNotes.Select(bombNotes => new BeatMapBombNote
                {
                    time = obstacles.beat,
                    noteLineIndex = obstacles.xPosition,
                    type = (obstacles.yPosition == 2) ? ObstacleType.Top : ObstacleType.FullHeight,
                    duration = obstacles.d,
                    width = obstacles.w
                }
                ).ToList();*/

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

            }

            float SongDuration = Notes.OrderByDescending(n => n.time).Select(n => n.time).FirstOrDefault();
            AverageNPS = Notes.Count / SongDuration;

            //containsCustomWalls = ContainsCustomWalls();//crashes exe to use this. tried it in BeatMap360Generator.cs also. same result.
        }

        public void AddRotationEvent(float beat, int amount, int type)
        {
            if (amount == 0)
                return;

            if (MajorVersion == 2)
                AddRotationEventV2(beat, amount, type);
            else
                AddRotationEventV3(beat, amount, type);
        }
        public void AddRotationEventV2(float beat, int amount, int type = 15)
        {
            BeatmapEventData rotationEvent;

            if (type == 14)
                rotationEvent = BeatmapEventData.Early;
            else
                rotationEvent = BeatmapEventData.Late;

            //value 0 = -60, 1 = -45, 2 = -30, 3 = -15, 4 = 15, 5 = 30, 6 = 45, 7 = 60 degrees clockwise
            int theValue;

            switch (amount)
            {
                case -4:
                    theValue = 0;
                    break;
                case -3:
                    theValue = 1;
                    break;
                case -2:
                    theValue = 2;
                    break;
                case -1:
                    theValue = 3;
                    break;
                case 1:
                    theValue = 4;
                    break;
                case 2:
                    theValue = 5;
                    break;
                case 3:
                    theValue = 6;
                    break;
                case 4:
                    theValue = 7;
                    break;
                default:
                    theValue = -1; // Set a default value if necessary - no rotation
                    break;
            }

            Events.Add(new BeatMapEventV2()
            {
                time = beat,
                type = rotationEvent,
                value = theValue
            });
        }
        public void AddRotationEventV3(float theBeat, int amount, int type = 2)
        {
            SpawnRotationEventType rotationEvent;

            //If v2 rotation event is sent here to convert it to v3 
            if (type == 1)
                rotationEvent = SpawnRotationEventType.Early;
            else
                rotationEvent = SpawnRotationEventType.Late;

            rotationEvents.Add(new BeatMapRotationEvent()
            {
                beat = theBeat,
                spawnRotationEventType = rotationEvent, // Update this to match version 3's enum
                rotation = amount * 15
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

            obstacles.Add(new BeatMapObstacleV3()
            {
                beat = time,
                xPosition = noteLineIndex,
                yPosition = noteLineLayer,
                d = duration,
                w = width,
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

        [JsonProperty("_time")]//was having problems with time=0 not appearing if i use DefaultValueHandling.Ignore. but will appear in v3 maps if don't use "shoudlserialize"
        public float time { get; set; }//used by Sort method -- should be in beats

        public bool ShouldSerializetime()
        { return BeatMapData.MajorVersion == 2; }//time should appear in v2 maps not matter what value (even 0)

        [JsonProperty("_type")]//, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public BeatmapEventData type { get; set; }

        public bool ShouldSerializetype()
        { return BeatMapData.MajorVersion == 2; }//time should appear in v2 maps not matter what value (even 0)

        [JsonProperty("_value")]//, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int value { get; set; }

        public bool ShouldSerializevalue()
        { return BeatMapData.MajorVersion == 2; }//time should appear in v2 maps not matter what value (even 0)
    }

    [Serializable]
    public class BeatMapEventV2 : BeatMapEvent
    {

    }
    /*
    [Serializable]
    public class BeatMapEventV3 : BeatMapEvent
    {
        [JsonProperty("customData", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public object customData { get; set; }

        [JsonProperty("b")]
        public float beat { get; set; }//beat

        [JsonProperty("e")]
        public SpawnRotationEventType spawnRotationEventType { get; set; }//type

        [JsonProperty("r")]
        public int rotation { get; set; }//rotation
    }
    */
    [Serializable]
    public class BeatMapRotationEvent//v3
    {
        [JsonProperty("customData", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public object customData { get; set; }

        [JsonProperty("b")]
        public float beat { get; set; }//beat

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
    /*
    [Serializable]
    public class BeatMapNoteV3 : BeatMapNote
    {
        [JsonProperty("b")]
        public float beat { get; set; }//beat

        [JsonProperty("x")]
        public int xPosition { get; set; }

        [JsonProperty("y")]
        public int yPosition { get; set; }

        [JsonProperty("c")]
        public NoteType color { get; set; }//0 Red 1 Blue

        [JsonProperty("d")]
        public NoteCutDirection cutDirection { get; set; }

        [JsonProperty("a")]
        public int angleOffset { get; set; }//An integer number which represents the additional counter-clockwise angle offset applied to the note's cut direction in degrees

    }
    */
    [Serializable]
    public class BeatMapColorNote//v3
    {
        [JsonProperty("b")]
        public float beat { get; set; }//beat

        [JsonProperty("x")]
        public int xPosition { get; set; }

        [JsonProperty("y")]
        public int yPosition { get; set; }

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
    /*
    [Serializable]
    public class BeatMapObstacleV3 : BeatMapObstacle
    {
        [JsonProperty("customData", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public object customData { get; set; }

        [JsonProperty("b")]
        public float beat { get; set; }//beat

        [JsonProperty("x")]
        public int xPosition { get; set; }

        [JsonProperty("y")]
        public int yPosition { get; set; }

        [JsonProperty("d")]
        public float d { get; set; }

        [JsonProperty("w")]
        public int w { get; set; }

        [JsonProperty("h")]
        public int height { get; set; }
    }
    */
    [Serializable]
    public class BeatMapObstacleV3
    {
        [JsonProperty("customData", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public object customData { get; set; }

        [JsonProperty("b")]
        public float beat { get; set; }//beat

        [JsonProperty("x")]
        public int xPosition { get; set; }

        [JsonProperty("y")]
        public int yPosition { get; set; }

        [JsonProperty("d")]
        public float d { get; set; }

        [JsonProperty("w")]
        public int w { get; set; }

        [JsonProperty("h")]
        public int height { get; set; }
    }

    [Serializable]
    public class BeatMapBombNote
    {
        [JsonProperty("b")]
        public float beat { get; set; }

        [JsonProperty("x")]
        public int xPosition { get; set; }

        [JsonProperty("y")]
        public int yPosition { get; set; }
    }
}
