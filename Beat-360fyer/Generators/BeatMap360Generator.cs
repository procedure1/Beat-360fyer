using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Stx.ThreeSixtyfyer.Generators
{
    // [/ ] [ \] [\/] [  ] [/\] [\|] [\-] [|/] [-/]
    // [\ ] [ /] [  ] [/\] [\/] [-\] [|\] [/-] [/|]

    [BeatMapGenerator("360 Degree Generator BW", 10, "BW", "This generator generates 360 Degree gamemodes from the Standard ones.\n" +
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

        public BeatMapData FromStandard(BeatMapData standardMap, float bpm, float timeOffset)
        {
            //FROM MASTER
            BeatMap360GeneratorSettings settings = (BeatMap360GeneratorSettings)Settings;
            BeatMapData map = new BeatMapData(standardMap);
            if (map.Notes.Count == 0)
                return map;

            //FROM PLUGIN
            //Can't figure out how to search for custom walls without crashing the exe so not implementing it.
            /*
            //LinkedList<BeatmapDataItem> dataItems = data.allBeatmapDataItems;
            //bool containsCustomWalls = dataItems.Count((e) => e is CustomObstacleData d && (d.customData?.ContainsKey("_position") ?? false)) > 12;
            bool containsCustomWalls = map.ContainsCustomWalls();
            */



            // Amount of rotation events emitted
            int eventCount = 0;
            // Current rotation
            int totalRotation = 0;
            // Moments where a wall should be cut
            List<(float, int)> wallCutMoments = new List<(float, int)>();
            // Previous spin direction, false is left, true is right
            bool previousDirection = true;
            float previousSpinTime = float.MinValue;

            //BW TEST 2 *************************************************************
            //Try to avoid huge rotations on fast maps by altering the preferred bar duration depending on the speed of the average notes per sec
            #region Avoid Huge Rotations
            /*
            if (BeatMapData.AverageNPS <= 2.0f)
            {
                settings.PreferredBarDuration = 1.7f;
            }
            else if (BeatMapData.AverageNPS <= 3.0f)
            {
                settings.PreferredBarDuration = 1.84f;
            }
            else if (BeatMapData.AverageNPS <= 3.5f)
            {
                settings.PreferredBarDuration = 2.0f;
            }
            else if (BeatMapData.AverageNPS <= 4.0f)
            {
                settings.PreferredBarDuration = 2.25f;
            }
            else if (BeatMapData.AverageNPS <= 4.5f)
            {
                settings.PreferredBarDuration = 2.5f;
            }
            else
            {
                settings.PreferredBarDuration = 2.5f;
            }
            Debug.WriteLine($"averageNPS: {BeatMapData.AverageNPS}\tPreferredBarDuration: {settings.PreferredBarDuration}");
            */
            #endregion

            #region Rotate
            //Each rotation is 15 degree increments so 24 positive rotations is 360. Negative numbers rotate to the left, positive to the right
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

                previousDirection = amount > 0;
                eventCount++;
                wallCutMoments.Add((time, amount));

                float beat = time * bpm / 60f;

                //BW Discord help said to change InsertBeatmapEventData to InsertBeatmapEventDataInOrder which allowed content to be stored to data.
                //data.InsertBeatmapEventDataInOrder(new SpawnRotationBeatmapEventData(time, moment, amount * 15.0f));// * RotationAngleMultiplier));//discord suggestion         
                map.AddRotationEvent(beat, amount, type);
                Debug.WriteLine($"Time: {time}, Rotation: {amount}, Type: {type}");
            }
            #endregion

            float beatDuration = 60f / bpm;//sec

            // Align PreferredBarDuration to beatDuration
            float barLength = beatDuration;
            while (barLength >= settings.PreferredBarDuration * 1.25f / settings.RotationSpeedMultiplier)
                barLength /= 2f;
            while (barLength < settings.PreferredBarDuration * 0.75f / settings.RotationSpeedMultiplier)
                barLength *= 2f;

            Debug.WriteLine($"PreferredBarDuration: {settings.PreferredBarDuration}");


            List<BeatMapNote> notes = new List<BeatMapNote>();//time is in beats. can see beats are used in JSON file. PLUGIN List<NoteData> notes = dataItems.OfType<NoteData>().ToList();
            List<BeatMapNote> notesInBar = new List<BeatMapNote>();
            List<BeatMapNote> notesInBarBeat = new List<BeatMapNote>();

            //PLUGIN CODE -- The plugin inside Beat Saber works in seconds not in beats so need to convert to seconds to work with this code. use the following code to avoid altering map.notes since it will reference it otherwise
            for (int i = 0; i < map.Notes.Count; i++)
            {
                BeatMapNote originalNote = map.Notes[i];
                BeatMapNote newNote = new BeatMapNote
                {
                    time = originalNote.time * beatDuration, // Convert time from beats to seconds
                    noteLineIndex = originalNote.noteLineIndex,
                    noteLineLayer = originalNote.noteLineLayer,
                    type = originalNote.type,
                    noteCutDirection = originalNote.noteCutDirection,
                    CustomData = originalNote.CustomData // You can also create a deep copy of custom data if required
                };
                notes.Add(newNote);
            }
            // Align bars to first note, the first note (almost always) identifies the start of the first bar
            //Notes are read in beats not sec. time = beat * 60 / bpm;
            float firstBeatmapNoteTime = notes[0].time;

#if DEBUG
            Debug.WriteLine($"Setup bpm={bpm} beatDuration={beatDuration} barLength={barLength} firstNoteTime={firstBeatmapNoteTime}");
#endif

            for (int i = 0; i < notes.Count;)
            {
                float currentBarStart = Floor((notes[i].time - firstBeatmapNoteTime) / barLength) * barLength;
                float currentBarEnd = currentBarStart + barLength - 0.001f;

                notesInBar.Clear();
                for (; i < notes.Count && notes[i].time - firstBeatmapNoteTime < currentBarEnd; i++)
                {
                    // If isn't bomb
                    if (notes[i].noteCutDirection != NoteCutDirection.None)
                        notesInBar.Add(notes[i]);
                }

                if (notesInBar.Count == 0)
                    continue;

                if (settings.EnableSpin && notesInBar.Count >= 2 && currentBarStart - previousSpinTime > settings.SpinCooldown && notesInBar.All((e) => Math.Abs(e.time - notesInBar[0].time) < 0.001f))
                {
#if DEBUG
                    Debug.WriteLine($"[Generator] Spin effect at {firstBeatmapNoteTime + currentBarStart}");
#endif
                    int leftCount = notesInBar.Count((e) => e.noteCutDirection == NoteCutDirection.Left || e.noteCutDirection == NoteCutDirection.UpLeft || e.noteCutDirection == NoteCutDirection.DownLeft);
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
                        Debug.WriteLine($"Spin Rotate--- Time: {theTime} Rotation: {spinDirection * 15.0f} Type: {(int)BeatmapEventData.Early}");
                        Rotate(firstBeatmapNoteTime + currentBarStart + spinStep * s, spinDirection, (int)BeatmapEventData.Early, false);
                    }

                    // Do not emit more rotation events after this
                    previousSpinTime = currentBarStart;
                    continue;
                }

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
                        double timeDiff = Math.Round(Math.Round(afterLastNote.time, 4) - Math.Round(lastNote.time, 4), 4);//BW without any rounding or rounding to 5 or more digits still produces a different rotation between exe and plugin.

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

                    //***********************************
                    //Finally rotate - possible values here are -3,-2,-1,0,1,2,3 but in testing I only see -2 to 2
                    //The condition for setting rotationCount to 3 is that timeDiff (the time difference between afterLastNote and lastNote) is greater than or equal to barLength. If your test data rarely or never satisfies this condition, you won't see rotation values of -3 or 3.
                    //Similarly, the condition for setting rotationCount to 2 is that timeDiff is greater than or equal to barLength / 8. If this condition is rarely met in your test cases, it would explain why you mostly see rotation values of - 2, -1, 0, 1, or 2.
                    Debug.WriteLine($"Finally Rotate--- Time: {lastNote.time} Rotation: {rotation * 15} Type: {(int)BeatmapEventData.Late}");
                    Rotate(lastNote.time, rotation, (int)BeatmapEventData.Late);

                    Debug.WriteLine($"Total Rotations: {totalRotation * 15} Time: {lastNote.time} Rotation: {rotation * 15}");

                    /*
                    //BW FIX!
                    if (settings.OnlyOneSaber)
                    {
                        foreach (NoteData nd in notesInBarBeat)
                        {
                            if (settings.LeftHandedOneSaber)
                            {
                                if (nd.colorType == (rotation > 0 ? ColorType.ColorA : ColorType.ColorB))
                                {
                                    // Remove note
                                    dataItems.Remove(nd);
                                }
                                else
                                {
                                    // Switch all notes to ColorA
                                    if (nd.colorType == ColorType.ColorB)
                                    {
                                        nd.Mirror(data.numberOfLines);
                                    }
                                }
                            }
                            else
                            {
                                if (nd.colorType == (rotation < 0 ? ColorType.ColorB : ColorType.ColorA))
                                {
                                    // Remove note
                                    dataItems.Remove(nd);
                                }
                                else
                                {
                                    // Switch all notes to ColorA
                                    if (nd.colorType == ColorType.ColorA)
                                    {
                                        nd.Mirror(data.numberOfLines);
                                    }
                                }
                            }
                        }
                    }
                    */

                    // Generate wall.
                    // BW fyi v2 wall _type 1 or v3 wall y: noteLineLayer.Top(2) is a crouch wall -- but must be must be wide enough to and over correct x: lineIndex to be over a player
                    if (settings.WallGenerator)// && !containsCustomWalls)
                    {
                        int width = 1;//generate walls with width 1

                        float wallTime = currentBarBeatStart;
                        float wallDuration = dividedBarLength;

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
                            if (!notesInBarBeat.Any((e) => e.noteLineIndex == 3))//line index 3 is far right
                            {
                                ObstacleType type = notesInBarBeat.Any((e) => e.noteLineIndex == 2) ? ObstacleType.Top : ObstacleType.FullHeight;//BW added this from plugin v1.1.1 for v1.19--I think this just sets some walls shorter or taller for visual interest

                                int wallHeight = notesInBarBeat.Any((e) => e.noteLineIndex == 2) ? 1 : 3;//BW I think this just sets some walls shorter or taller for visual interest

                                if (afterLastNote.noteLineIndex == 3 && !(wallHeight == 1 && afterLastNote.noteLineLayer == (int)NoteLineLayer.Base))
                                    wallDuration = afterLastNote.time - settings.WallBackCut - wallTime;

                                if (wallDuration > settings.MinWallDuration)
                                {   //BW Discord help said to change AddBeatmapObjectData to AddBeatmapObjectDataInOrder which allowed content to be stored to data.
                                    //data.AddBeatmapObjectDataInOrder(new ObstacleData(wallTime, 3, wallHeight == 1 ? NoteLineLayer.Top : NoteLineLayer.Base, wallDuration, 1, 5));//note width is always 1 here. BW changed to make all walls 5 high since this version of plugin shortens height of walls which i don't like - default:  wallHeight)); wallHeight));
                                    map.AddWall(wallTime, 3, type, wallHeight == 1 ? (int)NoteLineLayer.Top : (int)NoteLineLayer.Base, wallDuration, width);
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
                                    map.AddWall(wallTime, 0, type, wallHeight == 1 ? (int)NoteLineLayer.Top : (int)NoteLineLayer.Base, wallDuration, width);
                                }
                            }
                        }
                    }

#if DEBUG
                    Debug.WriteLine($"[{currentBarBeatStart}] Rotate {rotation} (c={notesInBarBeat.Count},lc={leftCount},rc={rightCount},lastNotes={lastNotes.Count()},rotationTime={lastNote.time + 0.01f},afterLastNote={afterLastNote?.time},rotationCount={rotationCount})");
#endif
                }


#if DEBUG
                Debug.WriteLine($"[{currentBarStart + firstBeatmapNoteTime}({(currentBarStart + firstBeatmapNoteTime) / beatDuration}) -> {currentBarEnd + firstBeatmapNoteTime}({(currentBarEnd + firstBeatmapNoteTime) / beatDuration})] count={notesInBar.Count} segments={builder} barDiviver={barDivider}");
#endif
            }//End for loop over all notes

            
            //BW noodle extensions causes BS crash in the section somewhere below. Could drill down and figure out why. Haven't figured out how to test for noodle extensions but noodle extension have custom walls that crash Beat Saber so BW added test for custom walls.
            //BW FIX!
            bool containsCustomWalls = false;
            if (!containsCustomWalls)
            {
                //Cut walls, walls will be cut when a rotation event is emitted
                List<BeatMapObstacle> obstaclesToRemove   = new List<BeatMapObstacle>();
                List<BeatMapObstacle> secondPartObstacles = new List<BeatMapObstacle>();

                foreach (BeatMapObstacle ob in map.Obstacles.ToList()) // ToList() creates a copy of the collection.
                {
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
                            obstaclesToRemove.Add(ob);
                        }
                        else if (!isCustomWall && !settings.AllowLeanWalls && ((ob.noteLineIndex == 0 && ob.width == 2) || (ob.noteLineIndex == 2 && ob.width > 1)))//BW lean walls
                        {
                            obstaclesToRemove.Add(ob);
                        }
                        else if (!isCustomWall && !settings.AllowCrouchWalls && (ob.noteLineIndex == 0 && ob.width > 2))//BW crouch walls
                        {
                            obstaclesToRemove.Add(ob);
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
                                        width = ob.width
                                    };
                                    secondPartObstacles.Add(secondPart);
                                }
                                else if (firstPartDuration >= settings.MinWallDuration)
                                {
                                    // Just update the existing obstacle, the second piece of the cut wall is too small
                                    ob.duration =firstPartDuration;
                                }
                                else if (secondPartDuration >= settings.MinWallDuration)
                                {
                                    // Reuse the obstacle and use it as second part
                                    if (secondPartTime != ob.time && secondPartDuration != ob.duration)
                                    {
                                        //Debug.WriteLine("Queue 7");
                                        ob.time = secondPartTime;
                                        ob.duration = secondPartDuration;
                                        secondPartObstacles.Add(ob);
                                    }
                                }
                                else
                                {
                                    // When this wall is cut, both pieces are too small, remove it
                                    obstaclesToRemove.Add(ob);
                                }
#if DEBUG
                                Debug.WriteLine($"Split wall at {cutTime}: {originalTime}({originalDuration}) -> {firstPartTime}({firstPartDuration}) <|> {secondPartTime}({secondPartDuration}) cutMultiplier={cutMultiplier}");
#endif
                            }
                        }
                    }

                }

                foreach (BeatMapObstacle ob in obstaclesToRemove)
                {
                    map.Obstacles.Remove(ob);//remove the obstacles that met the criteria now
                }
                foreach (BeatMapObstacle secondPart in secondPartObstacles)
                {
                    map.Obstacles.Add(secondPart); //Add secondPart obstacles to map.Obstacles
                }

            }

            // Remove bombs (just problamatic ones)
            // ToList() is used so the Remove operation does not update the list that is being iterated
            if (BeatMapData.MajorVersion == 2)
            {
                foreach (BeatMapNote bomb in map.Notes.Where((e) => e.noteCutDirection == NoteCutDirection.None).ToList())
                {
                    foreach ((float cutTime, int cutAmount) in wallCutMoments)
                    {
                        if (bomb.time >= cutTime - settings.WallFrontCut && bomb.time < cutTime + settings.WallBackCut)
                        {
                            if ((bomb.noteLineIndex <= 2 && cutAmount < 0) || (bomb.noteLineIndex >= 1 && cutAmount > 0))
                            {
                                map.Notes.Remove(bomb);// Will be removed later
                            }
                        }
                    }
                }
            }
            else
            {
                List<BeatMapBombNote> bombNotesCopy = new List<BeatMapBombNote>(map.bombNotes);//create a new list so can iterate and remove items

                foreach (BeatMapBombNote bomb in bombNotesCopy)
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

            //Debug.WriteLine($"Emitted {eventCount} rotation events");
            //Debug.WriteLine($"LimitRotations: {LimitRotations}");
            //Debug.WriteLine($"BottleneckRotations: {BottleneckRotations}");

            //int rotationEventsCount = data.allBeatmapDataItems.OfType<SpawnRotationBeatmapEventData>().Count();
            //int obstaclesCount = data.allBeatmapDataItems.OfType<ObstacleData>().Count();

            //Debug.WriteLine($"rotationEventsCount: {rotationEventsCount}");
            //Debug.WriteLine($"obstaclesCount: {obstaclesCount}");

            //return data;

            //FROM MASTER
            map.Sort();
            return map;
        }
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
        public int BottleneckRotations { get; set; } = 10000;//BW 14 default. This is set by LevelUpdatePatcher which sets this to LimitRotations/2
        /// <summary>
        /// Enable the spin effect when no notes are coming.
        /// </summary>
        public bool EnableSpin { get; set; } = false;
        /// <summary>
        /// The total time 1 spin takes in seconds.
        /// </summary>
        [JsonIgnore]
        public float TotalSpinTime { get; set; } = 0.6f;
        /// <summary>
        /// Minimum amount of seconds between each spin effect.
        /// </summary>
        public float SpinCooldown { get; set; } = 10f;
        /// <summary>
        /// Amount of time in seconds to cut of the front of a wall when rotating towards it.
        /// </summary>
        public float WallFrontCut { get; set; } = 0.2f;
        /// <summary>
        /// Amount of time in seconds to cut of the back of a wall when rotating towards it.
        /// </summary>
        public float WallBackCut { get; set; } = 0.45f;
        /// <summary>
        /// True if you want to generate walls, walls are cool in 360 mode
        /// </summary>
        public bool WallGenerator { get; set; } = false;

        /// <summary>
        /// The minimum duration of a wall before it gets discarded
        /// </summary>
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
        public bool OnlyOneSaber { get; set; } = false;
        /// <summary>
        /// Left handed mode when OnlyOneSaber is activated
        /// </summary>
        public bool LeftHandedOneSaber { get; set; } = false;//BW added   
        //END FROM PLUGIN


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

        public float frameLength = 1f / 16f;               // in beats (default 1f/16f), the length of each generator loop cycle in beats, per this of a beat, a single spin rotation is possible
        public float beatLength = 1f;                      // in beats (default 1f), how the generator should interpret each beats length
        public float obstableBackCutoffSeconds = 0.38f;    // x seconds will be cut off a wall's back if it is in activeWallMaySpinPercentage
        public float obstacleFrontCutoffSeconds = 0.18f;   // x seconds will be cut off a wall's front if it is in activeWallMaySpinPercentage
        public float activeWallMaySpinPercentage = 0.6f;   // the percentage (0f - 1f) of an obstacles duration from which rotation is enabled again (0.4f), and wall cutoff will be used
        public bool enableSpin = false;                    // enable spin effect
        public RemoveOriginalWallsMode originalWallsMode = RemoveOriginalWallsMode.RemoveNotFun;
        public WallGeneratorMode wallGenerator = WallGeneratorMode.Enabled;

        public override bool Equals(object obj)//this is called when config file is missing and is created
        {
            if (obj is BeatMap360GeneratorSettings s)
            {
                return s.frameLength == frameLength
                    && beatLength == s.beatLength
                    && obstableBackCutoffSeconds == s.obstableBackCutoffSeconds
                    && obstacleFrontCutoffSeconds == s.obstacleFrontCutoffSeconds
                    && activeWallMaySpinPercentage == s.activeWallMaySpinPercentage
                    && enableSpin == s.enableSpin
                    && originalWallsMode == s.originalWallsMode
                    && wallGenerator == s.wallGenerator;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()//not sure when this gets called
        {
            int hash = 13;
            unchecked
            {
                hash = (hash * 7) + frameLength.GetHashCode();
                hash = (hash * 7) + beatLength.GetHashCode();
                hash = (hash * 7) + obstableBackCutoffSeconds.GetHashCode();
                hash = (hash * 7) + obstacleFrontCutoffSeconds.GetHashCode();
                hash = (hash * 7) + activeWallMaySpinPercentage.GetHashCode();
                hash = (hash * 7) + enableSpin.GetHashCode();
                hash = (hash * 7) + originalWallsMode.GetHashCode();
                hash = (hash * 7) + wallGenerator.GetHashCode();
            }
            return hash;
        }
    }
}