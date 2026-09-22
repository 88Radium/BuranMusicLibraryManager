using Buran.SQLite;

namespace Buran.Tests;

public class CaseGhostPlanTests {
    [Fact]
    public void MissingCaseVariant_IsTheStaleRow() {
        using var dir = new TempAudioDir();
        var live = dir.Add("2Pac - Life Goes On.mp3");
        var dead = dir.PathOf("2pac - Life Goes On.mp3");

        var planned = DBConnector.PlanCaseGhostRemovals([live, dead], File.Exists);

        var ghost = Assert.Single(planned);
        Assert.Equal(dead, ghost.RemovedPath);
        Assert.Equal("2pac - Life Goes On.mp3", ghost.Ghost.RemovedFileName);
        Assert.Equal("2Pac - Life Goes On.mp3", ghost.Ghost.SurvivingFileName);
    }

    [Fact]
    public void BothFilesPresent_AreKept() {
        using var dir = new TempAudioDir();
        var lower = dir.Add("2pac - Life Goes On.mp3");
        var upper = dir.Add("2Pac - Life Goes On.mp3");

        var planned = DBConnector.PlanCaseGhostRemovals([lower, upper], File.Exists);

        Assert.Empty(planned);
    }

    [Fact]
    public void SamePathTwice_IsNotACaseCollision() {
        using var dir = new TempAudioDir();
        var live = dir.Add("2Pac - Life Goes On.mp3");

        var planned = DBConnector.PlanCaseGhostRemovals([live, live], File.Exists);

        Assert.Empty(planned);
    }

    [Fact]
    public void NeitherFilePresent_IsListedWithoutASurvivor() {
        var dir = Path.Combine(Path.GetTempPath(), "buran-missing-" + Guid.NewGuid().ToString("N"));
        var lower = Path.Combine(dir, "2pac - Life Goes On.mp3");
        var upper = Path.Combine(dir, "2Pac - Life Goes On.mp3");

        var planned = DBConnector.PlanCaseGhostRemovals([lower, upper], File.Exists);

        Assert.Equal(2, planned.Count);
        Assert.All(planned, item => Assert.Equal("", item.Ghost.SurvivingFileName));
    }

    private sealed class TempAudioDir : IDisposable {
        private readonly string _dir = Directory.CreateTempSubdirectory("buran-case-").FullName;

        public string Add(string fileName) {
            var path = PathOf(fileName);
            File.WriteAllBytes(path, []);
            return path;
        }

        public string PathOf(string fileName) => Path.Combine(_dir, fileName);

        public void Dispose() => Directory.Delete(_dir, recursive: true);
    }
}
