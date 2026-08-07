using F1Predictor.Data;
using F1Predictor.Features;
using F1Predictor.Ingestion;
using F1Predictor.MachineLearning;
using F1Predictor.OpenF1;
using Microsoft.EntityFrameworkCore;

var year = args.Length > 0 && int.TryParse(args[0], out var y) ? y : 2024;

await using var db = new AppDbContext();
await db.Database.EnsureCreatedAsync();

// 1. Ingest season -> raw tables in SQLite
var client = new OpenF1Client(new HttpClient());
var ingestor = new SeasonIngestor(client, db);
await ingestor.IngestSeasonAsync(year);

// 2. Build features -> DriverRaceFeature table
var featureBuilder = new FeatureBuilder(db);
await featureBuilder.BuildAsync();

var allFeatures = await db.DriverRaceFeatures.ToListAsync();
var raceCount = allFeatures.Select(f => f.SessionKey).Distinct().Count();

// 3. Bail out early if fewer than 3 races have usable feature data
if (raceCount < 3)
{
    Console.WriteLine($"Only {raceCount} race(s) have usable feature data for {year} - need at least 3 to train.");
    Console.WriteLine("Try a different year, or wait for more of the season to be ingestible.");
    return;
}

// 4. Identify holdout race (most recent by date_start), exclude from training set
var sessions = await db.RaceSessions.ToDictionaryAsync(s => s.SessionKey);
var holdoutSessionKey = allFeatures
    .Select(f => f.SessionKey)
    .Distinct()
    .OrderByDescending(sk => sessions[sk].DateStart)
    .First();

var holdoutMeeting = await db.Meetings.FindAsync(sessions[holdoutSessionKey].MeetingKey);

var trainingFeatures = allFeatures.Where(f => f.SessionKey != holdoutSessionKey).ToList();
var holdoutFeatures = allFeatures.Where(f => f.SessionKey == holdoutSessionKey).OrderBy(f => f.FinishPosition).ToList();

Console.WriteLine($"Training on {raceCount - 1} races ({trainingFeatures.Count} driver-race rows).");
Console.WriteLine($"Holdout race: {holdoutMeeting?.MeetingName} ({holdoutFeatures.Count} drivers).");
Console.WriteLine();

// 5. Train podium model, print metrics
var trainer = new ModelTrainer();
const string podiumModelPath = "podium-model.zip";
const string pointsModelPath = "points-model.zip";

var podiumModel = trainer.TrainAndSave(trainingFeatures, usePodiumLabel: true, podiumModelPath);

// 6. Train points model, print metrics
var pointsModel = trainer.TrainAndSave(trainingFeatures, usePodiumLabel: false, pointsModelPath);

// 7. Predict holdout race, print comparison table
Console.WriteLine($"=== Holdout predictions: {holdoutMeeting?.MeetingName} ===");
Console.WriteLine($"{"Driver#",-8}{"Grid",-6}{"Finish",-8}{"Podium?",-9}{"P(Podium)",-11}{"P(Points)",-11}");
foreach (var f in holdoutFeatures)
{
    var podiumPred = trainer.Predict(podiumModel, f);
    var pointsPred = trainer.Predict(pointsModel, f);
    Console.WriteLine($"{f.DriverNumber,-8}{f.GridPosition,-6}{f.FinishPosition,-8}{(f.Podium ? "Yes" : "No"),-9}{podiumPred.Probability,-11:P1}{pointsPred.Probability,-11:P1}");
}

// 8. Print final summary
Console.WriteLine();
Console.WriteLine("=== Summary ===");
Console.WriteLine($"Database: {Path.GetFullPath("f1predictor.db")}");
Console.WriteLine($"Podium model: {Path.GetFullPath(podiumModelPath)}");
Console.WriteLine($"Points model: {Path.GetFullPath(pointsModelPath)}");
