using System;
using System.Linq;

namespace Stx.ThreeSixtyfyer.Generators
{
    [BeatMapGenerator("Example 90Degree Generator", 3, "CodeStix", "This simple generator just swings to the left and to the right each x seconds.\n" +
        "This is to showcase how a generator is made.\n" +
        "Follow this example to create your own.")]
    public class ExampleGenerator : IBeatMapGenerator
    {
        public string GeneratedGameModeName => "90Degree";
        public object Settings { get; set; } = new ExampleGeneratorSettings(); // Set the default settings

        public BeatMapData FromStandard(BeatMapData standard, float bpm, float timeOffset, string author)
        {
            ExampleGeneratorSettings settings = (ExampleGeneratorSettings)Settings;
            BeatMapData map = new BeatMapData(standard, bpm); // Copy the original map

            // Implement your generator's logic
            int direction = 0;
            for(int i = 0; i < map.Notes.Count; i++)
            {
                BeatMapNote currentNote = map.Notes[i];
                if (i % settings.rotateEachNoteCount == 0)
                {
                    if (++direction % settings.rotateCountPerDirectionSwitch < settings.rotateCountPerDirectionSwitch / 2)//Each rotation amount is a 15 degree increment step so 24 positive rotations is 360. Negative numbers rotate to the left, positive to the right
                        map.AddRotationEvent(currentNote.time, 1, (int)BeatmapEventType.Late);
                    else
                        map.AddRotationEvent(currentNote.time, -1, (int)BeatmapEventType.Late);
                }
            }

            // Sort the BeatMap so that the inserted rotation events are in the right spot and not appended at the end of the events list
            map.SortAndConvertToBeats(bpm);

            // Return the modfied BeatMap
            return map;
        }
    }

    // Mark Serializable
    [Serializable]
    public class ExampleGeneratorSettings
    {
        public int rotateEachNoteCount = 2;
        public int rotateCountPerDirectionSwitch = 4;

        // Override Equals() is REQUIRED to check if generator settings are equal. Update the equals condition when adding more fields.
        public override bool Equals(object obj)
        {
            if (obj is ExampleGeneratorSettings s)
            {
                return rotateEachNoteCount == s.rotateEachNoteCount
                    && rotateCountPerDirectionSwitch == s.rotateCountPerDirectionSwitch;
            }
            else
            {
                return false;
            }
        }

        // Override GetHashCode() is recommended to check if generator settings are equal
        public override int GetHashCode()
        {
            int hash = 13;
            unchecked
            {
                hash = (hash * 7) + rotateEachNoteCount.GetHashCode();
                hash = (hash * 7) + rotateCountPerDirectionSwitch.GetHashCode();
            }
            return hash;
        }
    }
}
