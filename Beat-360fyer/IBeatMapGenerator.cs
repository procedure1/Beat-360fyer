﻿
namespace Stx.ThreeSixtyfyer
{
    public interface IBeatMapGenerator
    {
        string GeneratedGameModeName { get; }
        BeatMapData FromStandard(BeatMapData standard, float bpm, float timeOffset, string author);
        object Settings { get; set; }
    }
}
