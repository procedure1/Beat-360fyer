
namespace Stx.ThreeSixtyfyer
{
    public interface IBeatMapGenerator
    {
        string GeneratedGameModeName { get; }
        BeatMapData FromStandard(string mapVersion, BeatMapData standard, float bpm, float timeOffset);
        object Settings { get; set; }
    }
}
