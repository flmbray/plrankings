<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Net.Http.dll</Reference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>System.Drawing</Namespace>
  <Namespace>System.Drawing.Imaging</Namespace>
  <Namespace>System.Drawing.Drawing2D</Namespace>
  <Namespace>System.Drawing.Text</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Net.Http.Headers</Namespace>
  <Namespace>System.Security.Cryptography</Namespace>
</Query>

static string Alpha = "aa";
static int ExtraWidth = 20;
static int PenWidth = 26;

string srcDir = @"C:\Users\mbray\OneDrive\dev\plrankings\Data";
bool UseTeamCodes = true;
int TeamSlotHeight = 50;
int WeekSlotWidth = 90 + ExtraWidth;
int InterWeekGap = 30 + ExtraWidth;
bool ShowScore = true;
bool ShowPoints = true;
bool ShowWDL = true;
bool HighlightWinner = true;
int EnhanceText = 3;
int XTextOffset = 2;
int YTextOffset = -10;
string TeamOnTop = "TOT"; // "ARS";
string OnlyShowWeeksWhereTeamChangesPosition = null; // "MCI";
int curSeasonNum = 2023;	// only change this once per year, and should always represent the year the current season started in
int seasonNum = 2023;		// change to tell program to look at different data
string season;
int NumGames;
int[] IncludeWeeks = null;
int LastNWeeks = 0;
//static int[] IncludeWeeks = new int[] { 1, 2, 3, 36, 37, 38 };
Dictionary<int, int> CompSeasonNums = new Dictionary<int, int> {
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
					
Font font = new Font("Arial", PenWidth / 2, FontStyle.Bold);
Font mwFont = new Font("Arial", 14);

async Task<string> GetData(int? week)
{
	int compSeason = CompSeasonNums[seasonNum];
	var client = new HttpClient();
	string url;
	if (week.HasValue) url = $"https://footballapi.pulselive.com/football/standings?compSeasons={compSeason}&altIds=true&detail=2&FOOTBALL_COMPETITION=1&gameweekNumbers=1-{week}&live=true";
	else url = $"https://footballapi.pulselive.com/football/standings?compSeasons={compSeason}&altIds=true&detail=2&FOOTBALL_COMPETITION=1&live=true";
	var request = new HttpRequestMessage(HttpMethod.Get, url);
	request.Headers.Add("authority", "footballapi.pulselive.com");
	request.Headers.Add("accept", "*/*");
	request.Headers.Add("accept-language", "en-US,en;q=0.9");
	request.Headers.Add("cache-control", "no-cache");
	//request.Headers.Add("content-type", "application/x-www-form-urlencoded; charset=UTF-8");
	request.Headers.Add("origin", "https://www.premierleague.com");
	request.Headers.Add("pragma", "no-cache");
	request.Headers.Add("referer", "https://www.premierleague.com/");
	request.Headers.Add("sec-ch-ua", "\"Chromium\";v=\"112\", \"Google Chrome\";v=\"112\", \"Not:A-Brand\";v=\"99\"");
	request.Headers.Add("sec-ch-ua-mobile", "?0");
	request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
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
	return resp;
}

async Task DownloadSeason()
{
	$"Downloading season: {season}".Dump();
	string fdata = await GetData(null);
	var js = JsonConvert.DeserializeObject<Root>(fdata);
	var seasonGames = js.tables[0].entries.Select(e => e.overall.played.GetValueOrDefault()).Max().Dump();
	string hash = string.Empty;
	
	Directory.CreateDirectory(srcDir);
	int wnum = 1;
	do
	{
		fdata = await GetData(wnum);
		File.WriteAllText(Path.Combine(srcDir, $"{wnum}.json"), fdata);
		string newHash = Convert.ToBase64String(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(fdata)));
		if (newHash == hash) break;
		hash = newHash;
		
		js = JsonConvert.DeserializeObject<Root>(fdata);
		var numPlayed = js.tables[0].entries.Select(e => e.overall.played.GetValueOrDefault()).Min();

		$"Week #{wnum} - MinNumPlayed={numPlayed}".Dump();
		if (numPlayed == seasonGames) break;
		wnum++;
	} while (true);
}

async Task Main()
{
	season = seasonNum.Do(c => $"{c}-{c + 1}");
	srcDir = Path.Combine(srcDir, season);
	if (!Directory.Exists(srcDir)) await DownloadSeason();

	NumGames = seasonNum <= 1994 ? 42 : 38;
	if (seasonNum == curSeasonNum)
	{
		int wnum = Directory.GetFiles(srcDir).Select(fn => int.Parse(Path.GetFileNameWithoutExtension(fn))).OrderBy(n => n).Last() - 1;
		string fdata;
		//fdata = await GetData(null);
		//var js = JsonConvert.DeserializeObject<Root>(fdata);
		//var maxPlayed = js.tables[0].entries.Select(e => e.overall.played.GetValueOrDefault()).Max();
		//maxPlayed.Dump();
		//File.WriteAllText(Path.Combine(srcDir, $"{maxPlayed}.json"), fdata);

		string hash = string.Empty;
		string fhash = string.Empty;
		do
		{
			wnum++;
			hash = fhash;
			fdata = await GetData(wnum);
			fhash = U.ComputeHash(fdata);
			if (fhash != hash) File.WriteAllText(Path.Combine(srcDir, $"{wnum}.json"), fdata);
		} while (fhash != hash);

	}

	if (ShowScore) WeekSlotWidth += 80;
	if (ShowPoints) WeekSlotWidth += 20;
	if (ShowWDL) WeekSlotWidth += 40;
	Dictionary<int, Root> MatchWeeks = Enumerable.Range(1, 50)
		.Select(matchWeek => (matchWeek, data: matchWeek.Do(mw =>
		{
			string filename = $@"{srcDir}\{mw}.json";
			if (!File.Exists(filename)) return null;
			return JsonConvert.DeserializeObject<Root>(File.ReadAllText(filename));
		})))
		.Where(m => m.data != null)
		.ToDictionary(m => m.matchWeek, m => m.data);
	if (IncludeWeeks == null) IncludeWeeks = Enumerable.Range(1, MatchWeeks.Count()).ToArray();

	if (OnlyShowWeeksWhereTeamChangesPosition != null)
	{
		int lastWeek = IncludeWeeks.Last();
		IncludeWeeks = MatchWeeks.Where(mw => mw.Key.IsOneOf(1, lastWeek) || mw.Value.tables[0].entries.Single(e => e.team.club.abbr == OnlyShowWeeksWhereTeamChangesPosition).Do(e => e.startingPosition.Value != e.position.Value)).Select(mw => mw.Key).ToArray();
	}

	MatchWeeks = MatchWeeks
					.Where(mw => IncludeWeeks == null || IncludeWeeks.Contains(mw.Key))
					.Do(mws => LastNWeeks == 0 ? mws : mws.OrderByDescending(m => m.Key).Take(LastNWeeks))
					.OrderBy(mw => mw.Key)
					.ToDictionary(mw => mw.Key, mw => mw.Value); //.Dump();
	var allTeams = MatchWeeks.Values.SelectMany(v => v.tables[0].entries).Select(v => v.team.club.abbr).Distinct().ToArray();
	IncludeWeeks = MatchWeeks.Keys.OrderBy(mw => mw).ToArray();

	if (HighlightWinner && TeamOnTop == null) TeamOnTop = MatchWeeks.Last().Value.tables[0].entries[0].team.club.abbr;

	int numWeeks = MatchWeeks.Count();
	int numTeams = allTeams.Length;
	var TeamColors = Enumerable.Range(0, numTeams).ToDictionary(i => allTeams[i], i => DaveKeenan[i]);

	Dictionary<string, List<Point>> TeamPaths = new Dictionary<string, List<Point>>();
	Dictionary<int, Dictionary<int, Dictionary<string, int>>> PositionOverrides = new Dictionary<int, Dictionary<int, Dictionary<string, int>>>
	{
		[578] = new Dictionary<int, Dictionary<string, int>>
		{
			[1] = "NEW,BHA,MCI,ARS,CRY,FUL,MUN,BRE,TOT,BOU,CHE,LIV,WHU,NFO,LUT,AVL,EVE,SHU,WOL,BUR".Split(',').Select((t, i) => (position: i, team: t)).ToDictionary(o => o.team, o => o.position+1)
		}
	};
	int week = -1;
	//var ordering = int position = PositionOverrides.ValueOr(mw.Value.compSeason.id.Value)?.ValueOr(mw.Key)?.ValueOr(t.team.club.abbr, t.position.Value) ?? t.position.Value;
	foreach (var mw in MatchWeeks)
	{
		week++;
		foreach (var t in mw.Value.tables.First().entries)
		{
			if (PositionOverrides.ContainsKey(mw.Value.compSeason.id.Value)
				&& PositionOverrides[mw.Value.compSeason.id.Value].ContainsKey(mw.Key)
				&& PositionOverrides[mw.Value.compSeason.id.Value][mw.Key].ContainsKey(t.team.club.abbr)) t.position = PositionOverrides[mw.Value.compSeason.id.Value][mw.Key][t.team.club.abbr];
			if (!TeamPaths.ContainsKey(t.team.club.abbr)) 
			{
				//t.team.club.abbr.Dump();
				int p1x = week * WeekSlotWidth + (week >= 1 ? InterWeekGap : 0);
				int p1y = t.position.Value * TeamSlotHeight + TeamSlotHeight / 2;
				int p2x = WeekSlotWidth - (week >= 1 ? 0 : InterWeekGap);
				int p2y = p1y;

				TeamPaths[t.team.club.abbr] = new List<Point>();
				TeamPaths[t.team.club.abbr].Add(new Point(p1x, p1y));   // start
				TeamPaths[t.team.club.abbr].Add(new Point(p2x, p1y));   // start-control
				TeamPaths[t.team.club.abbr].Add(new Point(p1x, p2y));   // end-control
				TeamPaths[t.team.club.abbr].Add(new Point(p2x, p2y));   // end
			}
			Point startPoint = TeamPaths[t.team.club.abbr].Last();
			Point startControl = new Point(week * WeekSlotWidth, startPoint.Y);
			Point endControl = new Point(startPoint.X, t.position.Value * TeamSlotHeight + TeamSlotHeight / 2);
			Point endPoint = new Point(startControl.X, endControl.Y);
			TeamPaths[t.team.club.abbr].Add(startControl);
			TeamPaths[t.team.club.abbr].Add(endControl);
			TeamPaths[t.team.club.abbr].Add(endPoint);
			
			endControl = endPoint;
			endPoint.Offset(WeekSlotWidth, 0);
			if (mw.Key != IncludeWeeks.Last()) endPoint.Offset(-InterWeekGap, 0);
			TeamPaths[t.team.club.abbr].Add(endPoint);
			TeamPaths[t.team.club.abbr].Add(endControl);
			TeamPaths[t.team.club.abbr].Add(endPoint);
		}
	}

	Bitmap bmp = new Bitmap(numWeeks * WeekSlotWidth, (numTeams + 1) * TeamSlotHeight, PixelFormat.Format24bppRgb);
	
	List<string> svgs = new List<string>();
	int maxx = 0;
	using (Graphics g = Graphics.FromImage(bmp))
	{
		g.SmoothingMode = SmoothingMode.AntiAlias;
		g.TextRenderingHint = TextRenderingHint.AntiAlias;
		g.InterpolationMode = InterpolationMode.HighQualityBicubic;

		foreach (var tp in TeamPaths.OrderBy(tp => TeamOnTop != null ? tp.Key == TeamOnTop : false))
		{
			if (TeamOnTop != null && tp.Key == TeamOnTop)
			{
				g.DrawBeziers(new Pen(Color.White, PenWidth + 8), tp.Value.ToArray());
			}
			g.DrawBeziers(TeamColors[tp.Key], tp.Value.ToArray());

			var bza = tp.Value.ToArray();
			var c = TeamColors[tp.Key].Color;
			//$"{tp.Key} = {TeamColors[tp.Key].Color.ToString()}".Dump();
			string path = $"M {bza.First().X} {bza.First().Y} {bza.Skip(1).Chunk(3).Select(chunk => chunk.ToArray()).Select(gg => $"C {gg[0].X} {gg[0].Y} {gg[1].X} {gg[1].Y} {gg[2].X} {gg[2].Y}").Join(" ")}";
			svgs.Add($"<path id='{tp.Key}Highlight' stroke='white' stroke-opacity='0.0' stroke-width='26' stroke-linecap='round' fill='none' d='{path}' />");
			svgs.Add($"<path id='{tp.Key}' stroke='rgb({c.R}, {c.G}, {c.B})' stroke-opacity='0.6' stroke-width='20' stroke-linecap='round' fill='none' onmouseenter='enterPath();' onmouseout='exitPath();' onclick='clickPath();' d='{path}' />");
		}
		
		week = -1;
		foreach (var mw in MatchWeeks)
		{
			week++;
			int px = week * WeekSlotWidth;
			g.DrawString($"Week {mw.Key}", mwFont, Brushes.White, px, 10);
			svgs.Add($"<text pointer-events='none' x='{px}' y='30' class='heavy big'>Week #{mw.Key}</text>");
			px += XTextOffset;
			foreach (var t in mw.Value.tables.First().entries)
			{
				int py = t.position.Value * TeamSlotHeight + TeamSlotHeight / 2 + YTextOffset;

				string text = t.team.club.abbr;
				var m = t.form.Last();
				char matchResult = '\0';
				if (m.gameweek.gameweek.Value == mw.Key)
				{
					if (m.teams[0].team.club != null)
					{
						if (ShowScore)
						{
							var curTeam = m.teams[0].team.club.abbr == t.team.club.abbr ? m.teams[0] : m.teams[1];
							var otherTeam = curTeam == m.teams[0] ? m.teams[1] : m.teams[0];
							matchResult = curTeam.score > otherTeam.score ? '+' : curTeam.score < otherTeam.score ? '-' : '=';
							text = $"{curTeam.team.club.abbr} ({matchResult}{curTeam.score}:{otherTeam.score} {otherTeam.team.club.abbr})";
						}
						//else if (ShowMatchResult)
						//{
						//	if (m.teams[0].team.club != null)
						//	{
						//		var homeTeam = m.teams[0].team.club.abbr;
						//		var awayTeam = m.teams[1].team.club.abbr;
						//		if (m.outcome == "H" && homeTeam == t.team.club.abbr || m.outcome == "A" && awayTeam == t.team.club.abbr) text += " +";
						//		if (m.outcome == "H" && awayTeam == t.team.club.abbr || m.outcome == "A" && homeTeam == t.team.club.abbr) text += " -";
						//		if (m.outcome == "D") text += " ·";
						//	}
						//}
					}
				}
				if (ShowWDL && ShowPoints) text = $"{t.overall.won} / {t.overall.drawn} / {t.overall.lost} = {t.overall.points} &nbsp; {text}";
				else if (ShowWDL) text = $"{t.overall.won} / {t.overall.drawn} / {t.overall.lost} &nbsp; {text}";
				else if (ShowPoints) text = $"{t.overall.points}   {text}";

				if (EnhanceText != 0) for (int xx = -EnhanceText; xx <= EnhanceText; xx++) for (int yy = -EnhanceText; yy <= EnhanceText; yy++) g.DrawString(text, font, Brushes.Black, px + xx, py + yy);
				svgs.Add($"<text pointer-events='none' x='{px}' y='{py+15}' class='heavy'>{text}</text>");
				g.DrawString(text.Replace("&nbsp;", " "), font, Brushes.White, px, py);

			}
		}
		//bmp.Dump();
		WriteSVGs(svgs, numWeeks * 250);
	}
}

private void WriteSVGs(IList<string> svgs, int w)
{
	List<string> lines = new List<string>();
	lines.AddRange(new [] { 
		"<!DOCTYPE html>",
		"<html>",
		"<head>",
		"<style type='text/css'>",
		"  body { background-color: black; }",
		"  .heavy {",
		"    font: bold 15px sans-serif;",
		"    fill: white;",
		"  }",
		"  .big {",
		"    font-size: 1.5em;",
		"  }",
		"  text {",
		"    filter: drop-shadow(2px 2px 1px black);",
		"  }",
		"</style>",
		"<script type='text/javascript'>",
		"	function enterPath() {",
		"        el = event.target;",
		"        el.style['stroke-opacity'] = 1.0;",
		"        el = document.getElementById(`${el.id}Highlight`);",
		"        el.style['stroke-opacity'] = 1.0;",
		"   }",
		"	function exitPath() {",
		"        el = event.target;",
		"        if (!keepPathsOn.includes(el)) {",
		"            el.style['stroke-opacity'] = 0.6;",
		"            el = document.getElementById(`${el.id}Highlight`);",
		"            el.style['stroke-opacity'] = 0.0;",
		"        }",
		"   }",
		"   var keepPathsOn = [];",
		"	function clickPath() {",
		"        el = event.target;",
		"        if (keepPathsOn.includes(el)) { var ii = keepPathsOn.indexOf(el); if (ii > -1) keepPathsOn.splice(ii, 1); }",
		"        else keepPathsOn.push(el);",
		"   }",
		"</script>",
		"</head>",
		"<body>",
		$"<svg height='1200' width='{w+50}'>",
	});
	lines.AddRange(svgs);
	lines.Add("</html>");
	string filename;
	
	if (seasonNum == curSeasonNum) filename = $@"C:\Users\mbray\OneDrive\dev\plrankings\pl.html";
	else filename = $@"C:\Users\mbray\OneDrive\dev\plrankings\pl{season}.html";
	File.WriteAllLines(filename, lines);
	Process.Start(filename);
}

// Define other methods and classes here
// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
public class AltIds
{
	public string opta { get; set; }
}

public class Annotation
{
	public string type { get; set; }
	public string destination { get; set; }
}

public class Away
{
	public int? played { get; set; }
	public int? won { get; set; }
	public int? drawn { get; set; }
	public int? lost { get; set; }
	public int? goalsFor { get; set; }
	public int? goalsAgainst { get; set; }
	public int? goalsDifference { get; set; }
	public int? points { get; set; }
	public int? position { get; set; }
}

public class Clock
{
	public int? secs { get; set; }
	public string label { get; set; }
}

public class Club
{
	public string name { get; set; }
	public string shortName { get; set; }
	public string abbr { get; set; }
	public int? id { get; set; }
}

public class Competition
{
	public string abbreviation { get; set; }
	public string description { get; set; }
	public string level { get; set; }
	public string source { get; set; }
	public int? id { get; set; }
	public AltIds altIds { get; set; }
}

public class CompSeason
{
	public string label { get; set; }
	public Competition competition { get; set; }
	public int? id { get; set; }
}

public class Entry
{
	public Team team { get; set; }
	public int? position { get; set; }
	public int? startingPosition { get; set; }
	public Overall overall { get; set; }
	public Home home { get; set; }
	public Away away { get; set; }
	public List<Annotation> annotations { get; set; }
	public List<Form> form { get; set; }
	public Ground ground { get; set; }
}

public class Form
{
	public Gameweek gameweek { get; set; }
	public Kickoff kickoff { get; set; }
	public ProvisionalKickoff provisionalKickoff { get; set; }
	public List<Team2> teams { get; set; }
	public bool? replay { get; set; }
	public Ground ground { get; set; }
	public bool? neutralGround { get; set; }
	public string status { get; set; }
	public string phase { get; set; }
	public string outcome { get; set; }
	public int? attendance { get; set; }
	public Clock clock { get; set; }
	public string fixtureType { get; set; }
	public bool? extraTime { get; set; }
	public bool? shootout { get; set; }
	public bool? behindClosedDoors { get; set; }
	public int? id { get; set; }
	public AltIds altIds { get; set; }
}

public class Gameweek
{
	public int? id { get; set; }
	public int? gameweek { get; set; }
}

public class Ground
{
	public string name { get; set; }
	public string city { get; set; }
	public string source { get; set; }
	public int? id { get; set; }
	public int? capacity { get; set; }
	public Location location { get; set; }
}

public class Home
{
	public int? played { get; set; }
	public int? won { get; set; }
	public int? drawn { get; set; }
	public int? lost { get; set; }
	public int? goalsFor { get; set; }
	public int? goalsAgainst { get; set; }
	public int? goalsDifference { get; set; }
	public int? points { get; set; }
	public int? position { get; set; }
}

public class Kickoff
{
	public int? completeness { get; set; }
	public object millis { get; set; }
	public string label { get; set; }
	public double? gmtOffset { get; set; }
}

public class Location
{
	public double? latitude { get; set; }
	public double? longitude { get; set; }
}

public class Overall
{
	public int? played { get; set; }
	public int? won { get; set; }
	public int? drawn { get; set; }
	public int? lost { get; set; }
	public int? goalsFor { get; set; }
	public int? goalsAgainst { get; set; }
	public int? goalsDifference { get; set; }
	public int? points { get; set; }
}

public class ProvisionalKickoff
{
	public int? completeness { get; set; }
	public object millis { get; set; }
	public string label { get; set; }
	public double? gmtOffset { get; set; }
}

public class Root
{
	public CompSeason compSeason { get; set; }
	public bool? live { get; set; }
	public bool? dynamicallyGenerated { get; set; }
	public List<Table> tables { get; set; }
}

public class Table
{
	public int? gameWeek { get; set; }
	public List<Entry> entries { get; set; }
}

public class Team
{
	public string name { get; set; }
	public Club club { get; set; }
	public string teamType { get; set; }
	public string shortName { get; set; }
	public int? id { get; set; }
	public AltIds altIds { get; set; }
}

public class Team2
{
	public Team team { get; set; }
	public int? score { get; set; }
}

static Pen[] GrayColors = Enumerable.Range(0, 20).Select(e => new Pen(Color.FromArgb(255, 256 * e / 20 + 10, 256 * e / 20 + 10, 256 * e / 20 + 10), PenWidth)).ToArray();
static Pen[] DaveKeenan = "#00B7FF, #004DFF, #00FFFF, #826400, #FF00FF, #00FF00, #C500FF, #B4FFD7, #FFCA00, #969600, #B4A2FF, #C20078, #0000C1, #FF8B00, #FFC8FF, #666666, #FF0000, #CCCCCC, #009E8F, #D7A870, #8200FF, #960000, #BBFF00, #FFFF00, #006F00, #580041, #3D441E, #F277C6"
	.Split(',').Select(c => c.Trim().Trim('#'))
	.Select(rgb => $"{Alpha}{rgb}")
	.Select(hex => int.Parse(hex, System.Globalization.NumberStyles.HexNumber))
	.Select(rgb => new Pen(Color.FromArgb(rgb), PenWidth))
	//.Take(20)
	.ToArray().Dump();

/*
{
	new Pen(Color.Red, PenWidth),
	new Pen(Color.LightSkyBlue, PenWidth),
	new Pen(Color.Blue, PenWidth),
	new Pen(Color.IndianRed, PenWidth),
	new Pen(Color.Pink, PenWidth),
	new Pen(Color.Navy, PenWidth),
	new Pen(Color.GreenYellow, PenWidth),
	new Pen(Color.DarkBlue, PenWidth),
	new Pen(Color.DarkGray, PenWidth),
	new Pen(Color.Yellow, PenWidth),
	new Pen(Color.CornflowerBlue, PenWidth),
	new Pen(Color.Magenta, PenWidth),
	new Pen(Color.LightBlue, PenWidth),
	new Pen(Color.Orange, PenWidth),
	new Pen(Color.Brown, PenWidth),
	new Pen(Color.OrangeRed, PenWidth),
	new Pen(Color.DarkGreen, PenWidth),
	new Pen(Color.White, PenWidth),
	new Pen(Color.DarkRed, PenWidth),
	new Pen(Color.Gold, PenWidth),
};
*/