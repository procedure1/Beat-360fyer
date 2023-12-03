using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Stx.ThreeSixtyfyer.Generators
{
    // [/ ] [ \] [\/] [  ] [/\] [\|] [\-] [|/] [-/]
    // [\ ] [ /] [  ] [/\] [\/] [-\] [|\] [/-] [/|]

    [BeatMapGenerator("360 Degree Generator", 10, "BW", "This generator generates 360 Degree gamemodes from the Standard ones.\n" +
        "It makes sure it does not rotate too much and walls will be cut off at\n" +
        "the right moment so they don't block your vision when rotating.\n" +
        "Wall generation is supported, check the generator settings.")]
    public class BeatMap360Generator : IBeatMapGenerator
    {
        public string GeneratedGameModeName => "360Degree";
        public object Settings { get; set; } = new BeatMap360GeneratorSettings();

        private static int Floor(float f)
        {
            int i = (int)f;
            return f - i >= 0.999f ? i + 1 : i;
        }

        public BeatMapData FromStandard(BeatMapData standardMap, float bpm, float timeOffset, string author)
        {
            //FROM MASTER
            BeatMap360GeneratorSettings settings = (BeatMap360GeneratorSettings)Settings;
            BeatMapData map = new BeatMapData(standardMap, bpm);

            if (map.Notes.Count == 0)
                return map;

            //FROM PLUGIN
            //Can't figure out how to search for custom walls without crashing the exe so not implementing it.
            /*
            //LinkedList<BeatmapDataItem> dataItems = data.allBeatmapDataItems;
            //bool containsCustomWalls = dataItems.Count((e) => e is CustomObstacleData d && (d.customData?.ContainsKey("_position") ?? false)) > 12;
            bool containsCustomWalls = map.ContainsCustomWalls();
            */
            bool containsCustomWalls = false;


            // Amount of rotation events emitted
            int eventCount = 0;
            // Current rotation
            int totalRotation = 0;
            // Moments where a wall should be cut
            List<(float, int)> wallCutMoments = new List<(float, int)>();
            // Previous spin direction, false is left, true is right
            bool previousDirection = true;
            //float previousSpinTime = float.MinValue;

            //BOOST Lighting Events
            int boostInteration = 0; // Counter for tracking iterations
            bool boostOn = true; // Initial boolean value

            //Add Extra Rotations
            int r = 1;
            int totalRotationsGroup = 0;
            bool prevRotationPositive = true;
            int newRotation = 0;
            bool addMoreRotations = false;
            int RotationGroupLimit = settings.RotationGroupLimit;
            int RotationGroupSize = settings.RotationGroupSize;
            bool alternateParams = false;
            int offSetR = 0;

            #region Rotate
            //Each rotation amount is a 15 degree increment step so 24 positive rotations is 360. Negative numbers rotate to the left, positive to the right
            void Rotate(float time, int amount, int type, bool enableLimit = true)
            {
                //Allows 4*15=60 degree turn max and -60 degree min -- however amounts are never passed in higher than 3 or lower than -3. I in testing I only see 2 to -2
                if (amount == 0)
                    return;
                if (amount < -4)
                    amount = -4;
                if (amount > 4)
                    amount = 4;

                if (enableLimit)//always true unless you enableSpin in settings
                {
                    if (totalRotation + amount > settings.LimitRotations)
                        amount = Math.Min(amount, Math.Max(0, settings.LimitRotations - totalRotation));
                    else if (totalRotation + amount < -settings.LimitRotations)
                        amount = Math.Max(amount, Math.Min(0, -(settings.LimitRotations + totalRotation)));
                    if (amount == 0)
                        return;

                    totalRotation += amount;
                }

                if (settings.ArcFix && map.sliders != null)
                {
                    foreach (BeatMapSlider slider in map.Sliders)
                    {

                        if (time < slider.time - .001f)//if rotation time is at least .001s less than slider.time (so defiantely less)
                        {
                            break;//if the rotation is before a slider, it will be before all the remaining sliders too so can break from the foreach // lastRotationBeforeSlider = amount;//get the last rotation before or at the head of the slider
                        }
                        else if (time <= slider.tailTime + .001f)//can be a tiny bit .001s past the tailTime
                        {
                            //Plugin.Log.Info($"----- ARC FIX: Cancelled Rotation time: {time} amount: {amount * 15f}. During Slider time: {slider.time} tailTime: {slider.tailTime}.");

                            return;//exit rotate() and thus cancel the rotation

                            //Plugin.Log.Info($"--- lastRotationBeforeSlider: {lastRotationBeforeSlider}. During Slider found rotation at: {ro.time} amount: {ro.rotation} -- # of rotations inside so far: {sliderRotations.Count}");
                        }

                    }
                }

                previousDirection = amount > 0;
                eventCount++;
                wallCutMoments.Add((time, amount));//in seconds

                //BW Discord help said to change InsertBeatmapEventData to InsertBeatmapEventDataInOrder which allowed content to be stored to data.
                //data.InsertBeatmapEventDataInOrder(new SpawnRotationBeatmapEventData(time, moment, amount * 15.0f));// * RotationAngleMultiplier));//discord suggestion         
                map.AddRotationEvent(time, amount, type);
                Debug.WriteLine($"Time: {time}, Rotation: {amount}, Type: {type}");

                //Creates a boost lighting event. if ON, will set color left to boost color left new color etc. Will only boost a color scheme that has boost colors set so works primarily with COLORS > OVERRIDE DEFAULT COLORS. Or an authors color scheme must have boost colors set (that will probably never happen since they will have boost colors set if they use boost events).
                if (settings.BoostLighting && !BeatMapData.AlreadyUsingEnvColorBoost)
                {
                    boostInteration++;
                    if (boostInteration == 24 || boostInteration == 29)//33)//5 & 13 is good but frequent
                    {
                        map.AddBoost(time, boostOn);
                        //Plugin.Log.Info($"Boost Light! --- Time: {time} On: {boostOn}");
                        boostOn = !boostOn; // Toggle the boolean
                    }

                    // Reset the iteration counter if it reaches 13
                    if (boostInteration == 33) { boostInteration = 0; }
                }
            }
            #endregion

            float beatDuration = 60f / bpm;

            // Align PreferredBarDuration to beatDuration
            float barLength = beatDuration;

            while (barLength >= settings.PreferredBarDuration * 1.25f / settings.RotationSpeedMultiplier)
            {
                barLength /= 2f;
            }
            while (barLength < settings.PreferredBarDuration * 0.75f / settings.RotationSpeedMultiplier)
            {
                barLength *= 2f;
            }

            Debug.WriteLine($"PreferredBarDuration: {settings.PreferredBarDuration}");

            //All in seconds
            List<BeatMapNote> notes = new List<BeatMapNote>(map.Notes);//time is in seconds. can see beats are used in JSON file. PLUGIN List<NoteData> notes = dataItems.OfType<NoteData>().ToList();
            List<BeatMapNote> notesInBar = new List<BeatMapNote>();
            List<BeatMapNote> notesInBarBeat = new List<BeatMapNote>();

            // Align bars to first note, the first note (almost always) identifies the start of the first bar
            float firstBeatmapNoteTime = notes[0].time;

#if DEBUG
            Debug.WriteLine($"Setup bpm={bpm} beatDuration={barLength} barLength={barLength} firstNoteTime={firstBeatmapNoteTime}");
#endif
            #region Main Loop
            for (int i = 0; i < notes.Count;)
            {
                float currentBarStart = Floor((notes[i].time - firstBeatmapNoteTime) / barLength) * barLength;
                float currentBarEnd = currentBarStart + barLength - 0.001f;

                notesInBar.Clear();
                for (; i < notes.Count && notes[i].time - firstBeatmapNoteTime < currentBarEnd; i++)
                {
                    //if (notes[i].type != NoteType.Bomb)//BW CHANGED THIS TO TEST to see if have more rotations
                    notesInBar.Add(notes[i]);
                }

                if (notesInBar.Count == 0)
                    continue;

                /*
                if (settings.EnableSpin && notesInBar.Count >= 2 && currentBarStart - previousSpinTime > settings.SpinCooldown && notesInBar.All((e) => Math.Abs(e.time - notesInBar[0].time) < 0.001f))
                {
#if DEBUG
                    Debug.WriteLine($"[Generator] Spin effect at {firstBeatmapNoteTime + currentBarStart}");
#endif
                    int leftCount  = notesInBar.Count((e) => e.noteCutDirection == NoteCutDirection.Left  || e.noteCutDirection == NoteCutDirection.UpLeft  || e.noteCutDirection == NoteCutDirection.DownLeft);
                    int rightCount = notesInBar.Count((e) => e.noteCutDirection == NoteCutDirection.Right || e.noteCutDirection == NoteCutDirection.UpRight || e.noteCutDirection == NoteCutDirection.DownRight);

                    int spinDirection;
                    if (leftCount == rightCount)
                        spinDirection = previousDirection ? -1 : 1;
                    else if (leftCount > rightCount)
                        spinDirection = -1;
                    else
                        spinDirection = 1;

                    float spinStep = settings.TotalSpinTime / 24;
                    for (int s = 0; s < 24; s++)//amount (spinDirectin) is either -1 or 1
                    {
                        float theTime = firstBeatmapNoteTime + currentBarStart + spinStep * s;

                        //EnableSpin is FALSE in the settings. So this never runs.
                        Debug.WriteLine($"Spin Rotate--- Time: {theTime} Rotation: {spinDirection * 15.0f} Type: {(int)BeatmapEventType.Early}");
                        Rotate(firstBeatmapNoteTime + currentBarStart + spinStep * s, spinDirection, (int)BeatmapEventType.Early, false);
                    }

                    // Do not emit more rotation events after this
                    previousSpinTime = currentBarStart;
                    continue;
                }
                */
                // Divide the current bar in x pieces (or notes), for each piece, a rotation event CAN be emitted
                // Is calculated from the amount of notes in the current bar
                // barDivider | rotations
                // 0          | . . . . (no rotations)
                // 1          | r . . . (only on first beat)
                // 2          | r . r . (on first and third beat)
                // 4          | r r r r 
                // 8          |brrrrrrrr
                // ...        | ...
                // TODO: Create formula out of these if statements
                int barDivider;
                if (notesInBar.Count >= 58)
                    barDivider = 0; // Too mush notes, do not rotate
                else if (notesInBar.Count >= 38)
                    barDivider = 1;
                else if (notesInBar.Count >= 26)
                    barDivider = 2;
                else if (notesInBar.Count >= 8)
                    barDivider = 4;
                else
                    barDivider = 8;

                if (barDivider <= 0)
                    continue;
#if DEBUG
                StringBuilder builder = new StringBuilder();
#endif
                // Iterate all the notes in the current bar in barDiviver pieces (bar is split in barDiviver pieces)
                float dividedBarLength = barLength / barDivider;
                for (int j = 0, k = 0; j < barDivider && k < notesInBar.Count; j++)
                {
                    notesInBarBeat.Clear();
                    for (; k < notesInBar.Count && Floor((notesInBar[k].time - firstBeatmapNoteTime - currentBarStart) / dividedBarLength) == j; k++)
                    {
                        notesInBarBeat.Add(notesInBar[k]);
                        Debug.WriteLine(i);
                    }

#if DEBUG
                    // Debug purpose
                    if (j != 0)
                        builder.Append(',');
                    builder.Append(notesInBarBeat.Count);
#endif

                    if (notesInBarBeat.Count == 0)
                        continue;

                    float currentBarBeatStart = firstBeatmapNoteTime + currentBarStart + j * dividedBarLength;

                    // Determine the rotation direction based on the last notes in the bar
                    BeatMapNote lastNote = notesInBarBeat[notesInBarBeat.Count - 1];// NoteData lastNote = notesInBarBeat[notesInBarBeat.Count - 1];
                    IEnumerable<BeatMapNote> lastNotes = notesInBarBeat.Where((e) => Math.Abs(e.time - lastNote.time) < 0.005f);//IEnumerable<NoteData> lastNotes = notesInBarBeat.Where((e) => Math.Abs(e.time - lastNote.time) < 0.005f);

                    // Amount of notes pointing to the left/right
                    int leftCount = lastNotes.Count((e) => e.noteLineIndex <= 1 || e.noteCutDirection == NoteCutDirection.Left || e.noteCutDirection == NoteCutDirection.UpLeft || e.noteCutDirection == NoteCutDirection.DownLeft);
                    int rightCount = lastNotes.Count((e) => e.noteLineIndex >= 2 || e.noteCutDirection == NoteCutDirection.Right || e.noteCutDirection == NoteCutDirection.UpRight || e.noteCutDirection == NoteCutDirection.DownRight);

                    BeatMapNote afterLastNote = (k < notesInBar.Count ? notesInBar[k] : i < notes.Count ? notes[i] : null);

                    // Determine amount to rotate at once
                    // TODO: Create formula out of these if statements
                    int rotationCount = 1;
                    if (afterLastNote != null)
                    {
                        double barLength8thRound = Math.Round(barLength / 8, 4);
                        double timeDiff = Math.Round(afterLastNote.time - lastNote.time, 4);//BW without any rounding or rounding to 5 or more digits still produces a different rotation between exe and plugin.

                        if (notesInBarBeat.Count >= 1)
                        {
                            if (timeDiff >= barLength)//BW this never happens are far as I can tell.
                                rotationCount = 3;
                            else if (timeDiff >= barLength8thRound)//barLength / 8)//BW ---- This is the place where exe vs plugin maps will differ due to rounding between the 2 applications. i added rounding to 4 digits in order to match the output between the 2
                                rotationCount = 2;
                        }
                    }

                    int rotation = 0;
                    if (leftCount > rightCount)
                    {
                        // Most of the notes are pointing to the left, rotate to the left
                        rotation = -rotationCount;
                    }
                    else if (rightCount > leftCount)
                    {
                        // Most of the notes are pointing to the right, rotate to the right
                        rotation = rotationCount;
                    }
                    else
                    {
                        // Rotate to left or right
                        if (totalRotation >= settings.BottleneckRotations)
                        {
                            // Prefer rotating to the left if moved a lot to the right
                            rotation = -rotationCount;
                        }
                        else if (totalRotation <= -settings.BottleneckRotations)
                        {
                            // Prefer rotating to the right if moved a lot to the left
                            rotation = rotationCount;
                        }
                        else
                        {
                            // Rotate based on previous direction
                            rotation = previousDirection ? rotationCount : -rotationCount;
                        }
                    }

                    if (totalRotation >= settings.BottleneckRotations && rotationCount > 1)
                    {
                        rotationCount = 1;
                    }
                    else if (totalRotation <= -settings.BottleneckRotations && rotationCount < -1)
                    {
                        rotationCount = -1;
                    }

                    if (totalRotation >= settings.LimitRotations - 1 && rotationCount > 0)
                    {
                        rotationCount = -rotationCount;
                    }
                    else if (totalRotation <= -settings.LimitRotations + 1 && rotationCount < 0)
                    {
                        rotationCount = -rotationCount;
                    }

                    Debug.WriteLine($"k: {k} rotationCount: {rotationCount} totalRotation: {totalRotation}");

                    #region AddXtraRotation
                    //############################################################################
                    //BW had to add more rotations directly in the main loop. tried it outside this main loop. the problem with being outside the loop is you cannot decide if a map is really low on rotations until after the map is finished.
                    //add more rotation to maps without much rotation. If there are few rotations, look for directionless notes up/down/dot/bomb and make their rotation direction the same as the previous direction so that there will be increased totalRotation.
                    //Once rotation steps pass the RotationGroupLimit, make this inactive. Stay inactive for RotationGroupSize number of rotations and if there are few rotations while off, activate this again.

                    if (settings.AddXtraRotation)
                    {
                        if (addMoreRotations)//this stays on until passes the rotation limit
                        {
                            if (Math.Abs(totalRotationsGroup) < Math.Abs(RotationGroupLimit))
                            {
                                if (lastNote.noteCutDirection == NoteCutDirection.Up || lastNote.noteCutDirection == NoteCutDirection.Down || lastNote.noteCutDirection == NoteCutDirection.Any || lastNote.noteCutDirection == NoteCutDirection.None)//only change rotation if using a non-directional note. if remove this will allow a lot more rotations
                                {
                                    if (prevRotationPositive)//keep direction the same as the previous note
                                        newRotation = Math.Abs(rotation);
                                    else
                                        newRotation = -Math.Abs(rotation);

                                    //if (newRotation != rotation)
                                    //    Plugin.Log.Info($"r: {r} Old Rotation: {rotation} New Rotation: {newRotation}");// totalRotationsGroup: {totalRotationsGroup}");

                                    rotation = newRotation;

                                    totalRotationsGroup += rotation;
                                }

                            }
                            else//has now passed the rotation limit now
                            {
                                addMoreRotations = false;

                                totalRotationsGroup = 0;

                                //Plugin.Log.Info($"Change to NOT ACTIVE since passed the limit!!! RotationGroupLimit: {RotationGroupLimit}\t totalRotationsGroup: {totalRotationsGroup}");

                                offSetR = r;//need this since when passes the limit, r may be close or equal to being a multiple of RotationGroupSize. that means it could be active soon again. so need to offset r so it will stay off for RotationGroupSize rotations.(r - offSetR) will be 0 on first rotation...
                            }

                        }
                        else//inactive
                        {
                            totalRotationsGroup += rotation;

                            if ((r - offSetR) % RotationGroupSize == 0)// after RotationGroupSize - offset number of iterations, this will check if rotations are over the limit
                            {
                                if (Math.Abs(totalRotationsGroup) >= Math.Abs(RotationGroupLimit))//if the total rotations was over the limit, stay inactive
                                {
                                    addMoreRotations = false;

                                    //Plugin.Log.Info($"Continue to be NOT ACTIVE: Inactive rotations are over the limit so stay inactive for {RotationGroupSize} rotations. RotationGroupLimit: {RotationGroupLimit}\t RotationGroupSize set to: 0 ++++++++++++++++++++++++++++++++++++++++++++++++");
                                }
                                else//if the total rotations was under the limit, activate more rotations
                                {
                                    addMoreRotations = true;

                                    if (alternateParams)
                                    {
                                        RotationGroupLimit += 4;//change the limit size for variety //could not alter RotationGroupSize since causing looping problem
                                    }
                                    else
                                    {
                                        RotationGroupLimit -= 4;//change the limit size for variety //could not alter RotationGroupSize since causing looping problem
                                    }

                                    alternateParams = !alternateParams; // Toggles every other time addMoreRotations is true

                                    //Plugin.Log.Info($"ACTIVE:     RotationGroupLimit: {RotationGroupLimit}\t RotationGroupSize: {RotationGroupSize}------------------------------------------------");
                                }

                                totalRotationsGroup = 0;

                            }
                        }

                        if (rotation > 0)
                            prevRotationPositive = true;
                        else
                            prevRotationPositive = false;

                    }

                    //############################################################################
                    #endregion

                    //***********************************
                    //Finally rotate - possible values here are -3,-2,-1,0,1,2,3 but in testing I only see -2 to 2
                    //The condition for setting rotationCount to 3 is that timeDiff (the time difference between afterLastNote and lastNote) is greater than or equal to barLength. If your test data rarely or never satisfies this condition, you won't see rotation values of -3 or 3.
                    //Similarly, the condition for setting rotationCount to 2 is that timeDiff is greater than or equal to barLength / 8. If this condition is rarely met in your test cases, it would explain why you mostly see rotation values of - 2, -1, 0, 1, or 2.

                    Rotate(lastNote.time, rotation, (int)BeatmapEventType.Late);

                    Debug.WriteLine($"Finally Rotate--- Time: {lastNote.time} Rotation: {rotation * 15} Type: {(int)BeatmapEventType.Late}");
                    Debug.WriteLine($"Total Rotations: {totalRotation * 15} Time: {lastNote.time} Rotation: {rotation * 15}");

                    r++;

                    //BW I gave up on this since had errors that are difficult to trace
                    /*
                    #region OneSaber
                    if (settings.OnlyOneSaber)
                    {
                        foreach (BeatMapNote nd in notesInBarBeat)
                        {
                            if (settings.LeftHandedOneSaber)
                            {
                                if (nd.type == (rotation > 0 ? NoteType.NoteA : NoteType.NoteB))
                                {
                                    // Remove note
                                    notes.Remove(nd);//or notes.RemoveAt(i);
                                }
                                else
                                {
                                    // Switch all notes to ColorA
                                    if (nd.type == NoteType.NoteB)
                                    {
                                        //Debug.WriteLine($"Before Mirror Note type:{nd.type} lineIndex: {nd.noteLineIndex} cutDirection: {nd.noteCutDirection}");
                                        BeatMapData.Mirror(notes[i]);//PROBABLY NOT WORKING FIXXXX!!!!!
                                        //Debug.WriteLine($"After  Mirror Note type:{nd.type} lineIndex: {nd.noteLineIndex} cutDirection: {nd.noteCutDirection}");
                                        Debug.WriteLine(i); Debug.WriteLine(j); Debug.WriteLine(k); 
                                    }
                                }
                            }
                            else
                            {
                                if (nd.type == (rotation < 0 ? NoteType.NoteB : NoteType.NoteA))
                                {
                                    try
                                    {
                                        // Remove note
                                        notes.Remove(nd);//or notes.RemoveAt(i);
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"Exception while mirroring note: {ex}");
                                    }
                                }
                                else
                                {
                                    // Switch all notes to ColorA
                                    if (nd.type == NoteType.NoteA)
                                    {
                                        //Debug.WriteLine($"Before Mirror Note type:{nd.type} lineIndex: {nd.noteLineIndex} cutDirection: {nd.noteCutDirection}");
                                        try
                                        {
                                            Debug.WriteLine($"Before Mirror Note type:{notes[i].type} lineIndex: {notes[i].noteLineIndex} cutDirection: {notes[i].noteCutDirection}");
                                            BeatMapData.Mirror(notes[i]);
                                            Debug.WriteLine($"After Mirror Note type:{notes[i].type} lineIndex: {notes[i].noteLineIndex} cutDirection: {notes[i].noteCutDirection}");
                                        }
                                        catch (Exception ex)
                                        {
                                            Debug.WriteLine($"Exception while mirroring note: {ex}");
                                        }

                                        //Debug.WriteLine($"After  Mirror Note type:{nd.type} lineIndex: {nd.noteLineIndex} cutDirection: {nd.noteCutDirection}");
                                        Debug.WriteLine(i); Debug.WriteLine(j); Debug.WriteLine(k);
                                    }
                                }
                            }
                        }
                    }
                    #endregion
                    */
                    #region Wall Generator
                    // Generate wall.
                    // BW fyi v2 wall _type 1 or v3 wall y: noteLineLayer.Top(2) is a crouch wall -- but must be must be wide enough to and over correct x: lineIndex to be over a player
                    if (settings.WallGenerator && !containsCustomWalls)
                    {
                        float wallTime = currentBarBeatStart;
                        float wallDuration = dividedBarLength;

                        /*
                        foreach (BeatMapNote note1 in notesInBarBeat)
                        {
                            Debug.WriteLine($"----notesInBarBeat: Time: {Math.Round(note1.time, 2)}\t LineIndex: {note1.noteLineIndex}");
                        }
                        */

                        // Check if there is already a wall
                        bool generateWall = true;

                        foreach (BeatMapObstacle obs in map.Obstacles)
                        {
                            if (obs.time + obs.duration >= wallTime && obs.time < wallTime + wallDuration)
                            {
                                generateWall = false;
                                break;
                            }
                        }

                        if (generateWall && afterLastNote != null)
                        {
                            int width = 1;

                            if (!notesInBarBeat.Any((e) => e.noteLineIndex == 3))//line index 3 is far right
                            {
                                ObstacleType type = notesInBarBeat.Any((e) => e.noteLineIndex == 2) ? ObstacleType.Top : ObstacleType.FullHeight;//BW added this from plugin v1.1.1 for v1.19--I think this just sets some walls shorter or taller for visual interest

                                int wallHeight = notesInBarBeat.Any((e) => e.noteLineIndex == 2) ? 1 : 3;//BW I think this just sets some walls shorter or taller for visual interest

                                if (afterLastNote.noteLineIndex == 3 && !(wallHeight == 1 && afterLastNote.noteLineLayer == (int)NoteLineLayer.Base))
                                    wallDuration = afterLastNote.time - settings.WallBackCut - wallTime;

                                if (wallDuration > settings.MinWallDuration)
                                {   //BW Discord help said to change AddBeatmapObjectData to AddBeatmapObjectDataInOrder which allowed content to be stored to data.
                                    //data.AddBeatmapObjectDataInOrder(new ObstacleData(wallTime, 3, wallHeight == 1 ? NoteLineLayer.Top : NoteLineLayer.Base, wallDuration, 1, 5));//note width is always 1 here. BW changed to make all walls 5 high since this version of plugin shortens height of walls which i don't like - default:  wallHeight)); wallHeight));
                                    //string temp; if (wallHeight == 1) { temp = "Top"; } else { temp = "Base"; };

                                    if (settings.BigWalls)
                                    {
                                        if (i % 3 == 0 || i % 7 == 0)// Check for every 5th and 8th iteration
                                        {
                                            //width1Change++;//counts the 5th and 8th iterations.
                                            width = 12;// (width1Change % 3 == 0) ? 12 : 6;//every 3rd time we enter this block it sets width1 to -19; otherwise, it sets it to -11
                                        }
                                        else
                                        {
                                            width = 1; // Default value for all other iterations
                                        }
                                    }

                                    int lineIndex = 3;


                                    map.AddWall(wallTime, lineIndex, type, wallHeight == 1 ? (int)NoteLineLayer.Top : (int)NoteLineLayer.Base, wallDuration, width, 5);
                                }
                            }
                            if (!notesInBarBeat.Any((e) => e.noteLineIndex == 0))//line index 0 is far left
                            {
                                ObstacleType type = notesInBarBeat.Any((e) => e.noteLineIndex == 1) ? ObstacleType.Top : ObstacleType.FullHeight;//BW added this from plugin v1.1.1 for v1.19--I think this just sets some walls shorter or taller for visual interest

                                int wallHeight = notesInBarBeat.Any((e) => e.noteLineIndex == 1) ? 1 : 3;

                                if (afterLastNote.noteLineIndex == 0 && !(wallHeight == 1 && afterLastNote.noteLineLayer == (int)NoteLineLayer.Base))
                                    wallDuration = afterLastNote.time - settings.WallBackCut - wallTime;

                                if (wallDuration > settings.MinWallDuration)
                                {   //Discord help said to change AddBeatmapObjectData to AddBeatmapObjectDataInOrder which allowed content to be stored to data.
                                    //data.AddBeatmapObjectDataInOrder(new ObstacleData(wallTime, 0, wallHeight == 1 ? NoteLineLayer.Top : NoteLineLayer.Base, wallDuration, 1, 5));//BW wallHeight));
                                    //string temp; if (wallHeight == 1) { temp = "Top"; } else { temp = "Base"; };

                                    if (settings.BigWalls)
                                    {
                                        if (i % 4 == 0 || i % 6 == 0)// Check for every 5th and 8th iteration
                                        {
                                            //width2Change++;//counts the 5th and 8th iterations.
                                            width = -11;// (width2Change % 3 == 0) ? -11 : -5;//every 3rd time we enter this block it sets width1 to -19; otherwise, it sets it to -11
                                        }
                                        else
                                        {
                                            width = 1; // Default value for all other iterations
                                        }
                                    }

                                    int lineIndex = 0;

                                    map.AddWall(wallTime, lineIndex, type, wallHeight == 1 ? (int)NoteLineLayer.Top : (int)NoteLineLayer.Base, wallDuration, width, 5);
                                }
                            }
                        }
                    }
                    #endregion

#if DEBUG
                    Debug.WriteLine($"[{currentBarBeatStart}] Rotate {rotation} (c={notesInBarBeat.Count},lc={leftCount},rc={rightCount},lastNotes={lastNotes.Count()},rotationTime={lastNote.time + 0.01f},afterLastNote={afterLastNote?.time},rotationCount={rotationCount})");
#endif
                }


#if DEBUG
                Debug.WriteLine($"[{currentBarStart + firstBeatmapNoteTime}({(currentBarStart + firstBeatmapNoteTime)}) -> {currentBarEnd + firstBeatmapNoteTime}({(currentBarEnd + firstBeatmapNoteTime) / (60/bpm)})] count={notesInBar.Count} segments={builder} barDiviver={barDivider}");
#endif
            }//End for loop over all notes
            #endregion
            #region Wall Removal           
            //BW noodle extensions causes BS crash in the section somewhere below. Could drill down and figure out why. Haven't figured out how to test for noodle extensions but noodle extension have custom walls that crash Beat Saber so BW added test for custom walls.
            //BW FIX! 
            if (!containsCustomWalls)
            {
                //Cut walls, walls will be cut when a rotation event is emitted
                Queue<BeatMapObstacle> obstacles = new Queue<BeatMapObstacle>(map.Obstacles);

                while (obstacles.Count > 0)
                {

                    BeatMapObstacle ob = obstacles.Dequeue();

                    int totalRotations = 0;

                    foreach ((float cutTime, int cutAmount) in wallCutMoments)
                    {
                        if (ob.duration <= 0f)
                            break;

                        // Do not cut a margin around the wall if the wall is at a custom position
                        bool isCustomWall = false;
                        //if (ob.customData != null)
                        //{
                        //    isCustomWall = ob.customData.ContainsKey("_position");
                        //}
                        float frontCut = isCustomWall ? 0f : settings.WallFrontCut;
                        float backCut = isCustomWall ? 0f : settings.WallBackCut;


                        if (!isCustomWall && ((ob.noteLineIndex == 1 || ob.noteLineIndex == 2) && ob.width == 1))//BW lean walls that are only width 1 and hard to see coming in 360)
                        {
                            map.Obstacles.Remove(ob);
                        }
                        else if (!isCustomWall && !settings.AllowLeanWalls && ((ob.noteLineIndex == 0 && ob.width == 2) || (ob.noteLineIndex == 2 && ob.width > 1)))//BW lean walls
                        {
                            map.Obstacles.Remove(ob);
                        }
                        else if (!isCustomWall && !settings.AllowCrouchWalls && (ob.noteLineIndex == 0 && ob.width > 2))//BW crouch walls
                        {
                            map.Obstacles.Remove(ob);
                        }
                        // If moved in direction of wall
                        else if (isCustomWall || (ob.noteLineIndex <= 1 && cutAmount < 0) || (ob.noteLineIndex >= 2 && cutAmount > 0))
                        {
                            int cutMultiplier = Math.Abs(cutAmount);
                            if (cutTime > ob.time - frontCut && cutTime < ob.time + ob.duration + backCut * cutMultiplier)
                            {
                                float originalTime = ob.time;
                                float originalDuration = ob.duration;

                                float firstPartTime = ob.time;// 225.431: 225.631(0.203476) -> 225.631() <|> 225.631(0.203476)
                                float firstPartDuration = (cutTime - backCut * cutMultiplier) - firstPartTime; // -0.6499969
                                float secondPartTime = cutTime + frontCut; // 225.631
                                float secondPartDuration = (ob.time + ob.duration) - secondPartTime; //0.203476

                                if (firstPartDuration >= settings.MinWallDuration && secondPartDuration >= settings.MinWallDuration)
                                {
                                    // Update duration of existing obstacle
                                    ob.duration = firstPartDuration;

                                    // And create a new obstacle after it
                                    BeatMapObstacle secondPart = new BeatMapObstacle
                                    {
                                        time = secondPartTime,
                                        noteLineIndex = ob.noteLineIndex,
                                        type = ob.type,
                                        duration = secondPartDuration,
                                        width = ob.width,
                                    };
                                    map.Obstacles.Add(secondPart);//BW Discord help said to change AddBeatmapObjectData to AddBeatmapObjectDataInOrder which allowed content to be stored to data.
                                    obstacles.Enqueue(secondPart);

                                }
                                else if (firstPartDuration >= settings.MinWallDuration)
                                {
                                    // Just update the existing obstacle, the second piece of the cut wall is too small
                                    ob.duration = firstPartDuration;
                                }
                                else if (secondPartDuration >= settings.MinWallDuration)
                                {
                                    // Reuse the obstacle and use it as second part
                                    if (secondPartTime != ob.time && secondPartDuration != ob.duration)
                                    {
                                        //Debug.WriteLine("Queue 7");
                                        ob.time = secondPartTime;
                                        ob.duration = secondPartDuration;
                                        obstacles.Enqueue(ob);
                                    }
                                }
                                else
                                {
                                    // When this wall is cut, both pieces are too small, remove it
                                    map.Obstacles.Remove(ob);
                                }
#if DEBUG
                                Debug.WriteLine($"Split wall at {cutTime}: {originalTime}({originalDuration}) -> {firstPartTime}({firstPartDuration}) <|> {secondPartTime}({secondPartDuration}) cutMultiplier={cutMultiplier}");
#endif
                            }
                        }

                        //-------------BW added --- remove any walls whose duration is long enough to get enough rotations that the wall becomes visible again as it exits through the user space (is visible traveling backwards)-------------
                        // Check if the total rotations is more than 75 degrees (5*15) which means it is no longer visible and therefore probably not needed and possibly will be seen leaving the user space
                        if (cutTime >= ob.time && cutTime < ob.time + ob.duration)//checks if rotations occur during a wall
                        {
                            totalRotations += cutAmount;// Total number of rotations during the current obstacle - resets to 0 with each ob

                            if ((totalRotations > 5 || totalRotations < -5))
                            {
                                //Plugin.Log.Info($"Wall found with more than 5 rotations during its duration -- starting: {ob.time} duration: {ob.duration} are: {totalRotations} rotations");
                                float newDuration = (cutTime - ob.time) / 2.3f;
                                if (newDuration >= settings.MinWallDuration)
                                {
                                    ob.duration = newDuration;
                                    //Plugin.Log.Info($"------New Duration: {ob.duration} which is (cutTime - ob.time)/2 since half the wall occurs past the user play area");
                                    break;
                                }
                                else
                                {
                                    map.Obstacles.Remove(ob);
                                    //Plugin.Log.Info($"------Wall removed since shorter than MinWallDuration");
                                    break;
                                }
                            }
                        }
                    }

                }

            }
            #endregion

            #region Bomb Removal

            // Remove bombs (just problamatic ones)
            // ToList() is used so the Remove operation does not update the list that is being iterated
            if (BeatMapData.MajorVersion == 2)
            {
                foreach (BeatMapNote bomb in map.Notes.Where((e) => e.type == NoteType.Bomb).ToList())
                {
                    foreach ((float cutTime, int cutAmount) in wallCutMoments)
                    {
                        if (bomb.time >= cutTime - settings.WallFrontCut && bomb.time < cutTime + settings.WallBackCut)
                        {
                            if ((bomb.noteLineIndex <= 2 && cutAmount < 0) || (bomb.noteLineIndex >= 1 && cutAmount > 0))
                            {
                                map.Notes.Remove(bomb);//FIX Test if removing most of the bombs!
                            }
                            else
                            {
                                //Debug.WriteLine($"Bomb not removed");
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (BeatMapBombNote bomb in map.bombNotes.ToList())//create a new list so can iterate and remove items
                {
                    foreach ((float cutTime, int cutAmount) in wallCutMoments)
                    {
                        if (bomb.beat >= cutTime - settings.WallFrontCut && bomb.beat < cutTime + settings.WallBackCut)
                        {
                            if ((bomb.xPosition <= 2 && cutAmount < 0) || (bomb.xPosition >= 1 && cutAmount > 0))
                            {
                                map.bombNotes.Remove(bomb);// Will be removed later
                            }
                        }
                    }
                }
            }

            #endregion

            //Debug.WriteLine($"Emitted {eventCount} rotation events");
            //Debug.WriteLine($"LimitRotations: {LimitRotations}");
            //Debug.WriteLine($"BottleneckRotations: {BottleneckRotations}");

            //int rotationEventsCount = data.allBeatmapDataItems.OfType<SpawnRotationBeatmapEventData>().Count();
            //int obstaclesCount = data.allBeatmapDataItems.OfType<ObstacleData>().Count();

            //Debug.WriteLine($"rotationEventsCount: {rotationEventsCount}");
            //Debug.WriteLine($"obstaclesCount: {obstaclesCount}");

            //return data;

            if (settings.CleanUpBeatSage && author.Contains("Beat Sage"))
                CleanUpBeatSage(notes, new List<BeatMapObstacle>(map.Obstacles));


            //FROM MASTER
            map.SortAndConvertToBeats(bpm);
            return map;
        }

        #region CleanUpBeatSage
        //REMOVE NOTES (and bombs) located at the same space and also remove any notes side by side with cutdirections facing out 
        private void CleanUpBeatSage(List<BeatMapNote> notes, List<BeatMapObstacle> obs)
        {
            Debug.WriteLine($"Beat Sage Map being cleaned! Looking at {notes.Count} notes");
            List<int> indicesToRemove = new List<int>();

            // Iterate through the notes list
            for (int i = 0; i < notes.Count; i++)
            {
                BeatMapNote currentNote = notes[i];

                // Iterate through the next three notes
                for (int j = i + 1; j < Math.Min(i + 4, notes.Count); j++)
                {
                    //Plugin.Log.Info($"Beat Sage j: {j}"); 

                    BeatMapNote nextNote = notes[j];

                    //if (Math.Round(currentNote.time, 2) == 17.58 && Math.Round(nextNote.time, 2) == 17.58)
                    //    Plugin.Log.Info($"BW 1 ********Found the offending notes!!!!!*******************");

                    // Check if the 2 notes are the same time or within .0001 sec of each other so they appear to almost overlap
                    if (nextNote.time - currentNote.time <= 0.05f)//0.03 seems good. 0.08 will start to catch notes from different beats.
                    {

                        //if (Math.Round(currentNote.time, 2) == 17.58 && Math.Round(nextNote.time, 2) == 17.58)
                        //    Plugin.Log.Info($"BW 2 ********Found the offending notes!!!!!*******************");

                        //Plugin.Log.Info($"Beat Sage found 2 notes at the exact same time (or close) of {currentNote.time} current note: {currentNote.gameplayType} index: {currentNote.lineIndex} layer: {currentNote.noteLineLayer} --- Nextnote: {nextNote.gameplayType} index: {nextNote.lineIndex} layer: {nextNote.noteLineLayer}");

                        // Check for SIDE-BY-SIDE Notes. -- Check if the two notes (not any bombs) have the same layer, and different index (they may be side-by-side)
                        if (currentNote.noteLineLayer == nextNote.noteLineLayer && // Check for same layer
                            Math.Abs(currentNote.noteLineIndex - nextNote.noteLineIndex) == 1 && //side-by-side based on index values
                            (currentNote.type == NoteType.NoteA || currentNote.type == NoteType.NoteB) && // Check if both are "Normal" notes
                            (nextNote.type == NoteType.NoteA || nextNote.type == NoteType.NoteB))
                        {
                            //if (Math.Round(currentNote.time, 2) == 17.58 && Math.Round(nextNote.time, 2) == 17.58)
                            //    Plugin.Log.Info($"BW 3 ********Found the offending notes!!!!!*******************");

                            // Check if the leftmost note has cutDirection Left and the rightmost note has cutDirection Right - and other impossible configurations
                            if (currentNote.noteLineIndex < nextNote.noteLineIndex)
                            {
                                if ((currentNote.noteCutDirection == NoteCutDirection.Left || currentNote.noteCutDirection == NoteCutDirection.DownLeft || currentNote.noteCutDirection == NoteCutDirection.UpLeft))
                                {
                                    indicesToRemove.Add(i);
                                    Debug.WriteLine($"Beat Sage 1 - remove note side-by-side with another note in impossible cutDirection at {currentNote.time}");
                                }
                            }
                            else
                            {
                                if ((currentNote.noteCutDirection == NoteCutDirection.Right || currentNote.noteCutDirection == NoteCutDirection.DownRight || currentNote.noteCutDirection == NoteCutDirection.UpRight))
                                {
                                    indicesToRemove.Add(i);
                                    Debug.WriteLine($"Beat Sage 2 - remove note side-by-side with another note in impossible cutDirection at {currentNote.time}");
                                }
                            }
                        }
                        // Check for ONE-ABOVE-THE-OTHER Notes. -- Check if the two notes (not any bombs) have the same index, and different layer (they may be one-above-the-other)
                        else if (currentNote.noteLineIndex == nextNote.noteLineIndex && // Check for same index
                                Math.Abs(currentNote.noteLineLayer - nextNote.noteLineLayer) == 1 && //one-above-the-other based on layer values
                                (currentNote.type == NoteType.NoteA || currentNote.type == NoteType.NoteB) && // Check if both are "Normal" notes
                                (nextNote.type == NoteType.NoteA || nextNote.type == NoteType.NoteB))
                        {
                            // Check if the bottommost note has cutDirection Down and the uppermost note has cutDirection Up - and other impossible configurations
                            if (currentNote.noteLineLayer < nextNote.noteLineLayer)
                            {
                                if ((currentNote.noteCutDirection == NoteCutDirection.Down || currentNote.noteCutDirection == NoteCutDirection.DownLeft || currentNote.noteCutDirection == NoteCutDirection.DownRight))
                                {
                                    indicesToRemove.Add(i);
                                    Debug.WriteLine($"Beat Sage 1 - remove note one-above-the-other with another note in impossible cutDirection at {currentNote.time}");
                                }
                            }
                            else
                            {
                                if ((currentNote.noteCutDirection == NoteCutDirection.Up || currentNote.noteCutDirection == NoteCutDirection.UpLeft || currentNote.noteCutDirection == NoteCutDirection.UpRight))
                                {
                                    indicesToRemove.Add(i);
                                    Debug.WriteLine($"Beat Sage 2 - remove note one-above-the-other with another note in impossible cutDirection at {currentNote.time}");
                                }
                            }
                        }
                        // Check for OVERLAPPING NOTES. -- Check if the two notes have the same lineIndex, and noteLineLayer
                        else if (currentNote.noteLineIndex == nextNote.noteLineIndex &&
                                 currentNote.noteLineLayer == nextNote.noteLineLayer)
                        {
                            Debug.WriteLine($"Found overlapping notes at: {currentNote.time} of type: {currentNote.type} & {nextNote.type}. Should delete one of them in next log.");
                            // Check if either of the notes is a bomb
                            if (currentNote.type == NoteType.Bomb)
                            {
                                // Remove the bomb note (1st note)
                                indicesToRemove.Add(i);
                                Debug.WriteLine($"Beat Sage 1 - remove bomb overlapping a 2nd bomb/note at {currentNote.time}");
                            }
                            else
                            {
                                // remove the 2nd note whether a bomb or not
                                indicesToRemove.Add(j);
                                j++; //since j is removed, skip it in the next iteration
                                Debug.WriteLine($"Beat Sage 2 - remove type: {nextNote.type} overlapping a note at {currentNote.time}");
                            }
                        }
                    }
                    else
                        break;//exits loop if a notes has time beyond .03sec from the currentNote
                }
            }

            // Remove notes inside obstacles (walls)
            foreach (BeatMapObstacle obstacle in obs)
            {
                float obstacleEndTime = obstacle.time + obstacle.duration;

                for (int i = 0; i < notes.Count; i++)
                {
                    BeatMapNote note = notes[i];

                    // Check if the note is within the time and lineIndex and lineLayer boundaries of the obstacle
                    if (note.time >= obstacle.time && note.time <= obstacleEndTime && note.noteLineIndex == obstacle.noteLineIndex)
                    {
                        indicesToRemove.Add(i);
                        //Plugin.Log.Info($"Beat Sage Map had a note/bomb inside a wall at: {note.time} and will be removed later");
                    }
                    else if (note.time > obstacleEndTime)
                    {
                        break;
                    }
                }
            }

            // Remove the duplicate notes from the original list in reverse order to avoid index issues
            for (int i = indicesToRemove.Count - 1; i >= 0; i--)
            {
                int indexToRemove = indicesToRemove[i];
                //Plugin.Log.Info($"Beat Sage Map had a note/bomb cut at: {notes[indexToRemove].time}");
                notes.RemoveAt(indexToRemove);
            }
            if (indicesToRemove.Count == 0)
                Debug.WriteLine($"Nothing to clean!!!!!!");

            // At this point, 'notes' will contain the unique notes with no duplicates, with both specified rules applied.

        }
        #endregion

    }
    //---------------------------------------------------------------------------------------------------------------------------------
    [Serializable]
    public class BeatMap360GeneratorSettings
    {
        //don't need this. a person should just set LimitRotations to the angle they prefer.
        //public bool Wireless360 { get; set; } = true;//LimitRotations = 99999;BottleneckRotations = 99999;

        //FROM PLUGIN
        /// <summary>
        /// The preferred bar duration in seconds. The generator will loop the song in bars. 
        /// This is called 'preferred' because this value will change depending on a song's bpm (will be aligned around this value).
        /// Affects the speed at which the rotation occurs. It will not affect the total number of rotations or the range of rotation.
        /// BW CREATED CONFIG ROTATION  SPEED to allow user to set this.
        /// </summary>

        [JsonIgnore]//won't show up in the user config file
        public float PreferredBarDuration { get; set; } = 2.75f;//BW I like 1.5f instead of 1.84f but very similar to changing LimitRotations, 1.0f is too much and 0.2f freezes beat saber  // Calculated from 130 bpm, which is a pretty standard bpm (60 / 130 bpm * 4 whole notes per bar ~= 1.84)
        public float RotationSpeedMultiplier { get; set; } = 1.0f;//BW This is a mulitplyer for PreferredBarDuration
        /// <summary>
        /// The amount of 15 degree rotations before stopping rotation events (rip cable otherwise) (24 is one full 360 rotation)
        /// </summary>
        public int LimitRotations { get; set; } = 10000;//BW a large number here in degress is the same as wirelesss 360. 24 is equivalent to 360 (24*15) so this is 420 degrees.
        /// <summary>
        /// The amount of rotations before preferring the other direction (24 is one full rotation) - USER should set this to LimitRotations/2 if plant to limit rotations.
        /// </summary>
        [JsonIgnore]
        public int BottleneckRotations { get; set; } = 10000;//BW 14 default. This is set by LevelUpdatePatcher which sets this to LimitRotations/2
        /// <summary>
        /// Enable the spin effect when no notes are coming.
        /// </summary>
        [JsonIgnore]
        public bool EnableSpin { get; set; } = false;
        /// <summary>
        /// The total time 1 spin takes in seconds.
        /// </summary>
        [JsonIgnore]
        public float TotalSpinTime { get; set; } = 0.6f;
        /// <summary>
        /// Minimum amount of seconds between each spin effect.
        /// </summary>
        [JsonIgnore]
        public float SpinCooldown { get; set; } = 10f;
        /// <summary>
        /// Amount of time in seconds to cut of the front of a wall when rotating towards it.
        /// </summary>
        [JsonIgnore]
        public float WallFrontCut { get; set; } = 0.2f;
        /// <summary>
        /// Amount of time in seconds to cut of the back of a wall when rotating towards it.
        /// </summary>
        [JsonIgnore]
        public float WallBackCut { get; set; } = 0.45f;
        /// <summary>
        /// True if you want to generate walls, walls are cool in 360 mode
        /// </summary>
        public bool WallGenerator { get; set; } = true;

        public virtual bool BigWalls { get; set; } = true;

        /// <summary>
        /// The minimum duration of a wall before it gets discarded
        /// </summary>
        [JsonIgnore]
        public float MinWallDuration { get; set; } = 0.001f;//BW try shorter duration walls because i like the cool short walls that some authors use default: 0.1f;
        /// <summary>
        /// Use to increase or decrease general rotation amount. This doesn't alter the number of rotations - .5 will reduce rotations size by 50% and 2 will double the rotation size.
        /// Set to default for rotations in increments of 15 degrees. 2 would make increments of 30 degrees etc.
        /// </summary>
        //public float RotationAngleMultiplier { get; set; } = 1f;//BW added this to lessen/increase rotation angle amount. 1 means 15 decre
        /// <summary>
        /// Allow crouch obstacles
        /// </summary>
        public bool AllowCrouchWalls { get; set; } = false;//BW added
        /// <summary>
        /// Allow lean obstacles (step to the left, step to the right)
        /// </summary>
        public bool AllowLeanWalls { get; set; } = false;//BW added
        /// <summary>
        /// True if you only want to keep notes of one color.
        /// </summary>
        /// 
        //BW I gave up on this since had errors that were hard to trace.
        //public bool OnlyOneSaber { get; set; } = false;
        /// <summary>
        /// Left handed mode when OnlyOneSaber is activated
        /// </summary>
        //public bool LeftHandedOneSaber { get; set; } = false;//BW added   


        public bool BoostLighting { get; set; } = false;
        public bool AddXtraRotation { get; set; } = true;//for periods of low rotation, will make sure rotations for direction-less notes move in same direction as last rotation so totalRotation will increase.
        [JsonIgnore]
        public int RotationGroupLimit { get; set; } = 10;//If totalRotations are under this limit, will add more rotations
        [JsonIgnore]
        public int RotationGroupSize { get; set; } = 12;//The number of rotations to remain inactive for adding rotations

        //public virtual bool ArcFix { get; set; } = true;//remove rotation during sliders unless the head and tail rotation ends up the same. results is partial mismatch of tail
        public bool ArcFix { get; set; } = true;//removes all rotations during sliders
        public bool CleanUpBeatSage { get; set; } = false;

        //END FROM PLUGIN

        /*
        [JsonConverter(typeof(StringEnumConverter))]
        public enum WallGeneratorMode
        {
            Disabled = 0,   // disable the builtin wall generator
            Enabled = 1     // enable the builtin wall generator
        }
        
        [JsonConverter(typeof(StringEnumConverter))]
        public enum RemoveOriginalWallsMode
        {
            RemoveNotFun,   // remove the walls that are not fun in 360 mode, like walls thicker than 1 lane (default)
            RemoveAll,      // remove all the walls from the original map, the wall generator is the only thing that should cause a wall in the 360 level
            Keep            // do not remove any walls from the original map
        }
        */
        //public float frameLength = 1f / 16f;               // in beats (default 1f/16f), the length of each generator loop cycle in beats, per this of a beat, a single spin rotation is possible
        [JsonIgnore]
        public float beatLength = 1f;                      // in beats (default 1f), how the generator should interpret each beats length
        [JsonIgnore]
        public float obstableBackCutoffSeconds = 0.38f;    // x seconds will be cut off a wall's back if it is in activeWallMaySpinPercentage
        [JsonIgnore]
        public float obstacleFrontCutoffSeconds = 0.18f;   // x seconds will be cut off a wall's front if it is in activeWallMaySpinPercentage
        [JsonIgnore]
        public float activeWallMaySpinPercentage = 0.6f;   // the percentage (0f - 1f) of an obstacles duration from which rotation is enabled again (0.4f), and wall cutoff will be used
        [JsonIgnore]
        public bool enableSpin = false;                    // enable spin effect
        //public RemoveOriginalWallsMode originalWallsMode = RemoveOriginalWallsMode.RemoveNotFun;
        //public WallGeneratorMode wallGenerator = WallGeneratorMode.Enabled;

        public override bool Equals(object obj)//this is called when config file is missing and is created
        {
            if (obj is BeatMap360GeneratorSettings s)
            {
                return //s.frameLength == frameLength
                       //beatLength == s.beatLength
                       //&& obstableBackCutoffSeconds == s.obstableBackCutoffSeconds
                       //&& obstacleFrontCutoffSeconds == s.obstacleFrontCutoffSeconds
                       //&& activeWallMaySpinPercentage == s.activeWallMaySpinPercentage
                       //&& enableSpin == s.enableSpin
                       //&& originalWallsMode == s.originalWallsMode
                    LimitRotations == s.LimitRotations
                    && RotationSpeedMultiplier == s.RotationSpeedMultiplier
                    && AddXtraRotation == s.AddXtraRotation
                    && ArcFix == s.ArcFix
                    && WallGenerator == s.WallGenerator
                    && BigWalls == s.BigWalls
                    && AllowCrouchWalls == s.AllowCrouchWalls
                    && AllowLeanWalls == s.AllowLeanWalls
                    && BoostLighting == s.BoostLighting
                    //&& OnlyOneSaber == s.OnlyOneSaber
                    //&& LeftHandedOneSaber == s.LeftHandedOneSaber
                    && CleanUpBeatSage == s.CleanUpBeatSage;
            }
            else
            {
                return false;
            }
        }

        // Override GetHashCode() is recommended to check if generator settings are equal
        public override int GetHashCode()//not sure when this gets called
        {
            int hash = 13;
            unchecked
            {
                //hash = (hash * 7) + frameLength.GetHashCode();
                //hash = (hash * 7) + beatLength.GetHashCode();
                //hash = (hash * 7) + obstableBackCutoffSeconds.GetHashCode();
                //hash = (hash * 7) + obstacleFrontCutoffSeconds.GetHashCode();
                //hash = (hash * 7) + activeWallMaySpinPercentage.GetHashCode();
                //hash = (hash * 7) + enableSpin.GetHashCode();
                //hash = (hash * 7) + originalWallsMode.GetHashCode();
                hash = (hash * 7) + RotationSpeedMultiplier.GetHashCode();
                hash = (hash * 7) + AddXtraRotation.GetHashCode();
                hash = (hash * 7) + ArcFix.GetHashCode();
                hash = (hash * 7) + WallGenerator.GetHashCode();
                hash = (hash * 7) + BigWalls.GetHashCode();
                hash = (hash * 7) + AllowCrouchWalls.GetHashCode();
                hash = (hash * 7) + AllowLeanWalls.GetHashCode();
                hash = (hash * 7) + BoostLighting.GetHashCode();
                //hash = (hash * 7) + OnlyOneSaber.GetHashCode();
                //hash = (hash * 7) + LeftHandedOneSaber.GetHashCode();
                hash = (hash * 7) + CleanUpBeatSage.GetHashCode();
            }
            return hash;
        }
    }
}