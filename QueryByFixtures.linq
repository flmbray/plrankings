<Query Kind="Program">
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
</Query>

async Task Main()
{
	int maxWeek = 38;
	bool showLiveData = false;
	List<Root> matchData = new List<UserQuery.Root>();
	foreach(var gwn in Enumerable.Range(1, maxWeek))
	{
		var data = await GetData(2024, gwn);
		matchData.Add(data);
	}
	//var weekData = root.content.Where(c => c.kickoff.completeness > 0);
	var weekInfos = 
		matchData
			.SelectMany(d => d.content
				.Where(c => c.kickoff.completeness > 0)
				.Select(c => new 
				{ 
					weekNum = (int)c.gameweek.gameweek, 
					kickoff = DateTimeOffset.FromUnixTimeMilliseconds((long)c.provisionalKickoff.millis).ToLocalTime() 
				})
			)
			.GroupBy(d => d.weekNum)
			.Select(d => new { weekNum = d.Key, start = d.Min(dd => dd.kickoff), end = d.Max(dd => dd.kickoff) })
			.ToArray();
	weekInfos.Dump();			
	
	var allGameStats = matchData.SelectMany(md =>
	{
		return md.content.SelectMany(gwm =>
		{
			return new summarizedGame[]
			{
				new summarizedGame {
					week = (int)gwm.gameweek.gameweek,
					club = gwm.teams[0].team.club.abbr,
					clubIsHome = true,
					opponent = gwm.teams[1].team.club.abbr,
					goalsFor = (int)gwm.teams[0].score,
					goalsAgainst = (int)gwm.teams[1].score,
					goalsDiff = (int)gwm.teams[0].score - (int)gwm.teams[1].score,
					points = gwm.status switch {
						"U" => 0,
						"L" when !showLiveData => 0,
						_ => (int)gwm.teams[0].score > (int)gwm.teams[1].score ? 3 : (int)gwm.teams[0].score == (int)gwm.teams[1].score ? 1 : 0
					},
					status = gwm.status
				},
				new summarizedGame {
					week = (int)gwm.gameweek.gameweek,
					club = gwm.teams[1].team.club.abbr,
					clubIsHome = false,
					opponent = gwm.teams[0].team.club.abbr,
					goalsFor = (int)gwm.teams[1].score,
					goalsAgainst = (int)gwm.teams[0].score,
					goalsDiff = (int)gwm.teams[1].score - (int)gwm.teams[0].score,
					points = gwm.status switch {
						"U" => 0,
						"L" when !showLiveData => 0,
						_ => (int)gwm.teams[1].score > (int)gwm.teams[0].score ? 3 : (int)gwm.teams[1].score == (int)gwm.teams[0].score ? 1 : 0
					},
					status = gwm.status
				}
			};
		});
	});

	var curWeek = allGameStats.Where(gs => gs.points > 0).Max(gs => gs.week);
	var weeklyRankings =
		Enumerable.Range(1, allGameStats.Max(gs => gs.week))
			.Select(week =>
			{
				var cumulativeStats = allGameStats
						//.Concat(new[] {
							//new summarizedGame { week = 13, club = "EVE", points = -8 },
							//new summarizedGame { week = 30, club = "NFO", points = -4 },
						//})
						.Where(gs => gs.week <= week)
						.ToArray();
				return new
				{
					rank = cumulativeStats
						.GroupBy(gs => gs.club)
						.Select(gs => new weeklyRankedTeam
						{
							club = gs.Key,
							points = gs.Sum(g => g.points),
							goalsDiff = gs.Sum(g => g.goalsDiff),
							goalsFor = gs.Sum(g => g.goalsFor),
							wins = gs.Count(g => (g.status == "L" && showLiveData || g.status == "C") && g.status.IsOneOf("C", "L") && g.points == 3),
							draws = gs.Count(g => (g.status == "L" && showLiveData || g.status == "C") && g.points == 1),
							losses = gs.Count(g => (g.status == "L" && showLiveData || g.status == "C") && g.points == 0)
						})
						.OrderByDescending(t => t.points)
						.ThenByDescending(t => t.goalsDiff)
						.ThenByDescending(t => t.goalsFor)
						.ThenByDescending(t => t, new HeadToHeadComparer(cumulativeStats, HeadToHeadComparer.Mode.Points))
						.ThenByDescending(t => t, new HeadToHeadComparer(cumulativeStats, HeadToHeadComparer.Mode.AwayGoals))
						.Select((t, i) => new weeklyRankedTeam { 
							club = t.club, 
							rank = i, 
							week = week, 
							points = t.points, 
							goalsDiff = t.goalsDiff, 
							goalsFor = t.goalsFor,
							wins = t.wins,
							draws = t.draws,
							losses = t.losses
						})
				};
			})
			.SelectMany(week => week.rank)
			.OrderBy(week => week.week)
			.ThenBy(week => week.rank)
			.ToArray();
	Dictionary<string, List<string>> TeamPaths = new Dictionary<string, System.Collections.Generic.List<string>>();
	Dictionary<string, weeklyRankedTeam[]> TeamRanks =
		weeklyRankings
			.GroupBy(r => r.club)
			.ToDictionary(r => r.Key, r => r.OrderBy(rr => rr.week).ToArray());

	int VerticalSpacing = 42;
	int XOffset = 25;
	int YOffset = 50;
	int WeekWidth = 60;
	int InterWeekGap = 100;
	int InfoBoxHeight = 110;
	int InfoBoxWidth = WeekWidth + 2 * 20;
	int PathWidth = 22;
	List<string> teamInfos = new List<string>();
	int colorId = -1;
	foreach (var team in TeamRanks.OrderBy(tr => tr.Key))
	{
		int x, y;
		var ranks = team.Value;
		List<string> path = new List<string>();
		path.Add($"M {XOffset} {ranks[0].rank * VerticalSpacing + YOffset}");
		x = WeekWidth + XOffset;
		y = ranks[0].rank * VerticalSpacing + YOffset;
		path.Add($"L {x} {y}");
		for (int wk = 1; wk < ranks.Length; wk++)
		{
			x += InterWeekGap / 2;
			int cp1x = x, cp1y = y, cp2x = x;
			x += InterWeekGap / 2;
			y = ranks[wk].rank * VerticalSpacing + YOffset;
			int cp2y = y;
			path.Add($"C {cp1x} {cp1y}, {cp2x} {cp2y}, {x} {y}");
			x += WeekWidth;
			path.Add($"L {x} {y}");

			//int cp1x = wk * (WeekWidth + InterWeekGap) + WeekWidth + InterWeekGap / 2 + XOffset;
			//int cp1y = ranks[wk] * VerticalSpacing + YOffset;
			//int wk2 = wk + 1;
			//int cp2x = cp1x;
			//int cp2y = ranks[wk2] * VerticalSpacing + YOffset;
			//int x = wk2 * (WeekWidth + InterWeekGap) + XOffset;
			//int y = ranks[wk2] * VerticalSpacing + YOffset;
			//path.Add($"C {cp1x} {cp1y}, {cp2x} {cp2y}, {x} {y}");
			//path.Add($"L {x + WeekWidth} {y}");
		}
		var clubGames = allGameStats.Where(gs => gs.club == team.Key).ToArray();
		teamInfos.Add($"\t\t\t{team.Key}: {{ path: new Path2D('{path.Join(" ")}'), color: '{teamColors[++colorId]}', weeks: {JsonConvert.SerializeObject(ranks)}, games: {JsonConvert.SerializeObject(clubGames)} }}");
		//break;
	}
	canvas = canvas
		.Replace("{{xOffset}}", XOffset.ToString())
		.Replace("{{yOffset}}", YOffset.ToString())
		.Replace("{{weekWidth}}", WeekWidth.ToString())
		.Replace("{{interWeekGap}}", InterWeekGap.ToString())
		.Replace("{{verticalSpacing}}", VerticalSpacing.ToString())
		.Replace("{{pathWidth}}", PathWidth.ToString())
		.Replace("{{infoBoxHeight}}", InfoBoxHeight.ToString())
		.Replace("{{infoBoxWidth}}", InfoBoxWidth.ToString())
		.Replace("{{curWeek}}", curWeek.ToString())
		.Replace("{{canvasHeight}}", (VerticalSpacing * 20 + InfoBoxHeight + YOffset).ToString())
		.Replace("{{canvasWidth}}", ((WeekWidth + InterWeekGap) * matchData.Count() - InterWeekGap + XOffset * 2).ToString())
		.Replace("{{weekInfos}}", JsonConvert.SerializeObject(weekInfos))
		.Replace("{{teamInfos}}", teamInfos.Join($",{Environment.NewLine}"));
	await File.WriteAllTextAsync(@"C:\temp\canvas2.html", canvas);
	ProcessStartInfo psi = new ProcessStartInfo {
		FileName = @"C:\temp\canvas2.html",
		WorkingDirectory = @"C:\temp\",
		UseShellExecute = true
	};
	Process.Start(psi);
	//await STATask.Run(() => System.Windows.Clipboard.SetText(canvas));
}

#region Support Classes

public class HeadToHeadComparer : IComparer<weeklyRankedTeam>
{
	IList<summarizedGame> games;
	Mode mode;

	public HeadToHeadComparer(IList<summarizedGame> games, Mode mode)
	{
		//"NewH2H".Dump();
		this.games = games;
		this.mode = mode;
	}

	public int Compare(weeklyRankedTeam x, weeklyRankedTeam y)
	{
		if (x.club == y.club) return 0;
		int res, xc, yc;
		if (mode == Mode.Points)
		{
			xc = games.Where(g => g.club == x.club && g.opponent == y.club).Sum(g => g.points);
			yc = games.Where(g => g.club == y.club && g.opponent == x.club).Sum(g => g.points);
		}
		else if (mode == Mode.AwayGoals)
		{
			xc = games.Where(g => g.club == x.club && g.opponent == y.club && !g.clubIsHome).Sum(g => g.goalsFor);
			yc = games.Where(g => g.club == y.club && g.opponent == x.club && !g.clubIsHome).Sum(g => g.goalsFor);
		}
		else return 0;
		res = xc.CompareTo(yc);
		//$"H2h-{mode}: x={x.club}={xc}   y={y.club}={yc}  res = {res}".Dump();
		return res;
	}

	public enum Mode
	{
		Points,
		AwayGoals
	}
}

public class summarizedGame
{
	public int week;
	public string club;
	public string opponent;
	public bool clubIsHome;
	public int goalsFor;
	public int goalsAgainst;
	public int goalsDiff;
	public int points;
	public string status;
}
public class weeklyRankedTeam
{
	public string club;
	public int week;
	public int rank;
	public int points;
	public int goalsDiff;
	public int goalsFor;
	public int goalsAgainst;
	public int wins;
	public int draws;
	public int losses;
}

async Task<Root> GetData(int seasonYear, int matchweekNumber)
{
	Root root;
	string filename = @$"C:\Users\mbray\OneDrive\dev\plrankings\Data\{seasonYear}-{seasonYear+1}\gameweekNumber-{matchweekNumber}.json";
	if (File.Exists(filename))
	{
		root = JsonConvert.DeserializeObject<Root>(await File.ReadAllTextAsync(filename));

		Func<Content, long> getMillis = c => (long)(c.kickoff.millis ?? c.provisionalKickoff.millis);
		var weekData = root.content.Where(c => c.kickoff.completeness > 0);
		bool isWeekInFuture = weekData.All(c => DateTimeOffset.FromUnixTimeMilliseconds(getMillis(c)) > DateTimeOffset.Now);
		bool isWeekInPast = weekData.All(c => DateTimeOffset.FromUnixTimeMilliseconds(getMillis(c)) < DateTimeOffset.Now);

		// week hasn't even been played yet - there's no more data I could obtain
		// most of the time this is OK assumption but it's not always true - sometimes they make schedule changes that can be reflected in the future
		// if there is future data to retrieve, just comment this line out temporarily
		if (isWeekInFuture) return root;
		
		// week is already played, but not sure if I have all the data...  if I do, then there's no more data I could obtain
		if (isWeekInPast && root.content.All(c => c.status == "C")) return root;
		
		// week is past but not all games are complete, or week is the current week...   continue (retrieve the data)
	}

	$"Retrieving {seasonYear}:{matchweekNumber}".Dump();
	
	HttpClient client = new HttpClient();
	int compSeason = CompSeasonNums[seasonYear];
	// pageSize=20 ensures that if every team plays, but some teams play more than 1 game, both games will be included (the default is 10)
	// &statuses=U,L,C removed - doesn't seem to be needed
	string url = $"https://footballapi.pulselive.com/football/fixtures?comps=1&compSeasons={compSeason}&gameweekNumbers={matchweekNumber}-{matchweekNumber}&pageSize=20";
	var request = new HttpRequestMessage(HttpMethod.Get, url);
	request.Headers.Add("authority", "footballapi.pulselive.com");
	request.Headers.Add("accept", "*/*");
	request.Headers.Add("accept-language", "en-US,en;q=0.9");
	request.Headers.Add("cache-control", "no-cache");
	//request.Headers.Add("content-type", "application/x-www-form-urlencoded; charset=UTF-8");
	request.Headers.Add("origin", "https://www.premierleague.com");
	request.Headers.Add("pragma", "no-cache");
	request.Headers.Add("referer", "https://www.premierleague.com/");
	//request.Headers.Add("sec-ch-ua", "\"Chromium\";v=\"127\", \"Google Chrome\";v=\"127\", \"Not:A-Brand\";v=\"99\"");
	//request.Headers.Add("sec-ch-ua-mobile", "?0");
	//request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
	request.Headers.Add("sec-fetch-dest", "empty");
	request.Headers.Add("sec-fetch-mode", "cors");
	request.Headers.Add("sec-fetch-site", "cross-site");
	request.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Safari/537.36");
	//var content = new StringContent(string.Empty);
	//content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded; charset=UTF-8");
	//request.Content = content;
	var response = await client.SendAsync(request);
	response.EnsureSuccessStatusCode();
	string resp = await response.Content.ReadAsStringAsync();

	root = JsonConvert.DeserializeObject<Root>(resp);
	Directory.CreateDirectory(Path.GetDirectoryName(filename));
	await File.WriteAllTextAsync(filename, resp);
	return root;
}


// Define other methods and classes here
// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
public class AltIds
{
	public string opta { get; set; }
}

public class Clock
{
	public double secs { get; set; }
	public string label { get; set; }
}

public class Club
{
	public string name { get; set; }
	public string shortName { get; set; }
	public string abbr { get; set; }
	public double id { get; set; }
}

public class Competition
{
	public string abbreviation { get; set; }
	public string description { get; set; }
	public string level { get; set; }
	public string source { get; set; }
	public double id { get; set; }
	public AltIds altIds { get; set; }
}

public class CompetitionPhase
{
	public double id { get; set; }
	public string type { get; set; }
	public List<double> gameweekRange { get; set; }
}

public class CompSeason
{
	public string label { get; set; }
	public Competition competition { get; set; }
	public double id { get; set; }
}

public class Content
{
	public Gameweek gameweek { get; set; }
	public Kickoff kickoff { get; set; }
	public ProvisionalKickoff provisionalKickoff { get; set; }
	public List<Team> teams { get; set; }
	public bool replay { get; set; }
	public Ground ground { get; set; }
	public bool neutralGround { get; set; }
	public string status { get; set; }
	public string phase { get; set; }
	public string outcome { get; set; }
	public double attendance { get; set; }
	public Clock clock { get; set; }
	public string fixtureType { get; set; }
	public bool extraTime { get; set; }
	public bool shootout { get; set; }
	public List<Goal> goals { get; set; }
	public List<object> penaltyShootouts { get; set; }
	public bool behindClosedDoors { get; set; }
	public double id { get; set; }
	public AltIds altIds { get; set; }
}

public class Gameweek
{
	public double id { get; set; }
	public CompSeason compSeason { get; set; }
	public double gameweek { get; set; }
	public CompetitionPhase competitionPhase { get; set; }
}

public class Goal
{
	public double personId { get; set; }
	public double assistId { get; set; }
	public Clock clock { get; set; }
	public string phase { get; set; }
	public string type { get; set; }
	public string description { get; set; }
}

public class Ground
{
	public string name { get; set; }
	public string city { get; set; }
	public string source { get; set; }
	public double id { get; set; }
}

public class Kickoff
{
	public double completeness { get; set; }
	public double? millis { get; set; }
	public string label { get; set; }
	public double gmtOffset { get; set; }
}

public class PageInfo
{
	public int page { get; set; }
	public int numPages { get; set; }
	public int pageSize { get; set; }
	public int numEntries { get; set; }
}

public class ProvisionalKickoff
{
	public double completeness { get; set; }
	public double millis { get; set; }
	public string label { get; set; }
	public double gmtOffset { get; set; }
}

public class Root
{
	public PageInfo pageInfo { get; set; }
	public List<Content> content { get; set; }
}

public class Team
{
	public Team2 team { get; set; }
	public double score { get; set; }
}

public class Team2
{
	public string name { get; set; }
	public Club club { get; set; }
	public string teamType { get; set; }
	public string shortName { get; set; }
	public double id { get; set; }
	public AltIds altIds { get; set; }
}


Dictionary<int, int> CompSeasonNums = new Dictionary<int, int>
{
	[2024] = 719,
	[2023] = 578,
	[2022] = 489,
	[2021] = 418,
	[2020] = 363,
	[2019] = 274,
	[2018] = 210,
	[2017] = 79,
	[2016] = 54,
	[2015] = 42,
	[2014] = 27,
	[2013] = 22,
	[2012] = 21,
	[2011] = 20,
	[2010] = 19,
	[2009] = 18,
	[2008] = 17,
	[2007] = 16,
	[2006] = 15,
	[2005] = 14,
	[2004] = 13,
	[2003] = 12,
	[2002] = 11,
	[2001] = 10,
	[2000] = 9,
	[1999] = 8,
	[1998] = 7,
	[1997] = 6,
	[1996] = 5,
	[1995] = 4,
	[1994] = 3,
	[1993] = 2,
	[1992] = 1
};

const decimal alpha = 0.5m;
string[] teamColors = new[] {
	$"rgba(0, 183, 255, {alpha})",
	$"rgba(0, 77, 255, {alpha})",
	$"rgba(0, 255, 255, {alpha})",
	$"rgba(130, 100, 0, {alpha})",
	$"rgba(255, 0, 255, {alpha})",
	$"rgba(0, 255, 0, {alpha})",
	$"rgba(197, 0, 255, {alpha})",
	$"rgba(180, 255, 215, {alpha})",
	$"rgba(255, 202, 0, {alpha})",
	$"rgba(150, 150, 0, {alpha})",
	$"rgba(180, 162, 255, {alpha})",
	$"rgba(194, 0, 120, {alpha})",
	$"rgba(200, 50, 193, {alpha})",
	$"rgba(255, 139, 0, {alpha})",
	$"rgba(255, 200, 255, {alpha})",
	$"rgba(102, 102, 102, {alpha})",
	$"rgba(255, 0, 0, {alpha})",
	$"rgba(204, 204, 204, {alpha})",
	$"rgba(0, 158, 143, {alpha})",
	$"rgba(215, 168, 112, {alpha})",
	$"rgba(130, 0, 255, {alpha})",
	$"rgba(150, 0, 0, {alpha})",
	$"rgba(187, 255, 0, {alpha})",
	$"rgba(255, 255, 0, {alpha})",
	$"rgba(0, 111, 0, {alpha})",
	$"rgba(88, 0, 65, {alpha})",
	$"rgba(61, 68, 30, {alpha})",
	$"rgba(242, 119, 198, {alpha})"
};

#endregion

string canvas = """
<html>
<head>
</head>
<body style='background-color: #cccccc;' onclick='doBodyMouse(event)'>
	<canvas id='pl' width="{{canvasWidth}}" height="{{canvasHeight}}" onclick='doMouse(event)' x-style="border: 2px solid black;">

	</canvas>
	<script type='text/javascript'>
		const canvas = document.getElementById('pl');
		const ctx = canvas.getContext('2d');
		const xOffset = {{xOffset}}, yOffset = {{yOffset}}, weekWidth = {{weekWidth}}, interWeekGap = {{interWeekGap}}, verticalSpacing = {{verticalSpacing}}, pathWidth = {{pathWidth}};
		const infoBoxHeight = {{infoBoxHeight}}, infoBoxWidth = {{infoBoxWidth}};
		const curWeek = {{curWeek}};
		var selectedClub = null;
		ctx.lineCap = "round";
		ctx.lineJoin = "round";
		const weekInfos = {{weekInfos}};
		const teams = {
{{teamInfos}}
		};
		var allClubs = Object.keys(teams);
		function drawCanvas()
		{
			ctx.shadowColor = "yellow";
			for(var week = 0; week < weekInfos.length; week++) {
				ctx.fillStyle = week < curWeek ? "black" : "gray";
				var x = week * (weekWidth + interWeekGap) + xOffset;
				ctx.font = "normal 16px arial";
				lText(ctx, `Week #${week+1}`, x, 15);
				ctx.font = "normal 12px arial";
				lText(ctx, new Date(weekInfos[week].start).toDateString(), x, 28);
			}
			allClubs.forEach(function (club) {
				ctx.lineWidth = pathWidth;
				ctx.shadowBlur = 5;
				ctx.shadowColor = 'yellow';
				ctx.strokeStyle = teams[club].color;
				ctx.stroke(teams[club].path);
				
				var weeks = teams[club].weeks;

				ctx.lineWidth = 2;
				ctx.shadowBlur = 0;

				ctx.save();
				ctx.font = "normal 12px arial";
				for(var week = 0; week < curWeek; week++)
				{
					var x = week * (weekWidth + interWeekGap) + xOffset,
						y = verticalSpacing * weeks[week].rank + yOffset + 5;
					ctx.fillStyle = clubHasGameweek(club, week+1) ? "black" : "gray";
					ctx.fillText(teams[club].weeks[week].points, x, y);
				}
				ctx.restore();
				
				ctx.font = "bold 16px arial";
				for(var week = 0; week < weeks.length; week++)
				{
					var x = week * (weekWidth + interWeekGap) + xOffset + 5,
						y = verticalSpacing * weeks[week].rank + yOffset + 5;
					ctx.fillStyle = week < curWeek && clubHasGameweek(club, week+1) ? "black" : "gray";
					ctx.fillText(club, x + 15, y);
				}
			});
		}
		function clubHasGameweek(club, weekNum) { return teams[club].games.some(g => g.week == weekNum && ['L', 'C'].includes(g.status)); }
		function doBodyMouse(ev) {
			if (selectedClub !== null) {
				ctx.clearRect(0, 0, canvas.width, canvas.height);
				drawCanvas();
				selectedClub = null;
			}
		}
		function doMouse(ev)
		{
			ev.stopPropagation();
			ctx.lineWidth = pathWidth;
			if (selectedClub !== null)
			{
				if (!ctx.isPointInStroke(teams[selectedClub].path, ev.offsetX, ev.offsetY))
				{
					ctx.clearRect(0, 0, canvas.width, canvas.height);
					drawCanvas();
					selectedClub = null;
				}
			}
			if (selectedClub == null)
			{
				for(var i=0; i<allClubs.length; i++)
				{
					var club = allClubs[i];
					if (ctx.isPointInStroke(teams[club].path, ev.offsetX, ev.offsetY))
					{
						//ctx.strokeStyle = teams[club].color.replace("0.2", "1");
						ctx.lineWidth = pathWidth;
						ctx.strokeStyle = "black";
						ctx.shadowBlur = 10;
						ctx.shadowColor = 'yellow';
						ctx.stroke(teams[club].path);

						ctx.fillStyle = "white";
						var weeks = teams[club].weeks;
						for(var week = 0; week < weeks.length; week++)
						{
							ctx.fillText(club, week * (weekWidth + interWeekGap) + xOffset + 15, verticalSpacing * weeks[week].rank + yOffset + 5);
						}

						selectedClub = club;
						
						break;
					}				
				}
			}
			if (selectedClub != null) {
				ctx.lineWidth = 3;
				var clubInfo = teams[selectedClub];
				var weeks = clubInfo.weeks;
				var games = clubInfo.games;
				ctx.fillStyle = "black";
				ctx.shadowBlur = 5;
				ctx.shadowColor = 'yellow';
				for(var week = 0; week < weeks.length; week++) {
					var x = week * (weekWidth + interWeekGap) + xOffset - 20,
						y = verticalSpacing * weeks[week].rank + yOffset + pathWidth;
					var weekGames = games.filter(g => g.week == week+1);
					roundedRect(ctx, x, y, infoBoxWidth, infoBoxHeight + 40 * (weekGames.length - 1), 10);
				}
				
				ctx.lineWidth = 2;
				ctx.fillStyle = "yellow";
				ctx.shadowBlur = 0;
				for(var week = 0; week < weeks.length; week++) {
					var weekInfo = weeks[week];
					var weekGames = games.filter(g => g.week == week+1);
					if (weekGames.length > 0)
					{
						var g;
						var y = verticalSpacing * weeks[week].rank + yOffset + pathWidth + 15;
						var cp = week * (weekWidth + interWeekGap) + xOffset + weekWidth / 2;
						for(var gameNum in weekGames)
						{
							g = weekGames[gameNum];

							var x = cp - 4;
							if (week >= curWeek || g.status == "U") ctx.fillStyle = "white";
							else if (g.points == 3) ctx.fillStyle = g.clubIsHome ? "lightgreen" : "red";
							else if (g.points == 1) ctx.fillStyle = "yellow";
							else ctx.fillStyle = g.clubIsHome ? "red" : "lightgreen";
							rText(ctx, g.clubIsHome ? g.club : g.opponent, x, y);
							if (week < curWeek) rText(ctx, g.clubIsHome ? g.goalsFor : g.goalsAgainst, x, y + 15);
							
							x = cp + 4;
							if (week >= curWeek || g.status == "U") ctx.fillStyle = "white";
							else if (g.points == 3) ctx.fillStyle = g.clubIsHome ? "red" : "lightgreen";
							else if (g.points == 1) ctx.fillStyle = "yellow";
							else ctx.fillStyle = g.clubIsHome ? "lightgreen" : "red";
							ctx.fillText(g.clubIsHome ? g.opponent : g.club, x, y);
							if (week < curWeek) ctx.fillText(g.clubIsHome ? g.goalsAgainst : g.goalsFor, x, y + 15);
							y += 40;
						}
						if (week < curWeek) {
							x = week * (weekWidth + interWeekGap) + xOffset;
							x = cp;
							
							ctx.save();
							
							ctx.font = "normal 12px arial";
							ctx.fillStyle = "white";
							rText(ctx, "P", x, y+14*0);
							rText(ctx, "GD", x, y+14*1);
							rText(ctx, "GF", x, y+14*2);
							x += 8;
							lText(ctx, weekInfo.points, x, y+14*0);
							lText(ctx, weekInfo.goalsDiff, x, y+14*1);
							lText(ctx, weekInfo.goalsFor, x, y+14*2);

							y += 50;
							cText(ctx, `${weekInfo.wins} - ${weekInfo.draws} - ${weekInfo.losses}`, cp, y);

							ctx.restore();
						}
					}
				}
			}
		}
		function roundedRect(ctx, x, y, width, height, radius) {
			ctx.beginPath();
			ctx.moveTo(x, y + radius);
			ctx.arcTo(x, y + height, x + radius, y + height, radius);
			ctx.arcTo(x + width, y + height, x + width, y + height - radius, radius);
			ctx.arcTo(x + width, y, x + width - radius, y, radius);
			ctx.arcTo(x, y, x, y + radius, radius);
			ctx.stroke();
			ctx.fill();
		}
		function lText(ctx, text, x, y) {
			ctx.fillText(text, x, y);
		}
		function cText(ctx, text, x, y) {
			var tm = ctx.measureText(text);
			ctx.fillText(text, x - tm.width/2, y);
		}
		function rText(ctx, text, x, y) {
			var tm = ctx.measureText(text);
			ctx.fillText(text, x - tm.width, y);
		}
		function lcrText(ctx, ltext, ctext, rtext, x, y, gap) {
			var w = ctx.measureText(ctext).width;
			rText(ltext, x - w/2 - gap, y);
			ctx.fillText(text, x - w/2, y);
			lText(rtext, x + w/2 + gap, y);
		}
		function tableText(ctx, cells, x, y) {
			ctx.save();
			//var cellMeasures = cells.map(r => r.map(c => ctx.measureText(c
			ctx.restore();
		}
		
		drawCanvas();
	</script>
</body>
""";
