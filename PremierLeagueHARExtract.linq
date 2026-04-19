<Query Kind="Program">
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>Newtonsoft.Json</Namespace>
</Query>

void Main()
{
	bool sim = false;
	var data = JsonConvert.DeserializeObject<HARRoot>(File.ReadAllText(@"C:\temp\www.premierleague.com.har"));
	var plData = data.log.entries.Where(e => e.request.url.StartsWith("https://footballapi.pulselive.com/football/standings"));
	plData = plData.DistinctBy(d => d.request.url.Split('-').Last());
	plData.Count().Dump();
	foreach (var d in plData)
	{
		var mwData = JsonConvert.DeserializeObject<MatchweekRoot>(d.response.content.text);
		string season = int.Parse(mwData.compSeason.label.Split('/').First()).Do(year => $"{year}-{year + 1}");
		int mw = mwData.tables[0].entries.SelectMany(e => e.form).Max(f => f.gameweek.gameweek.Value);
		string targetFilename = $@"C:\Users\mbray\OneDrive\Data\{season}\{mw}.json";
		$"{d.request.url} -> {targetFilename}".Dump();

		if (!sim)
		{
			string targetDir = Path.GetDirectoryName(targetFilename);
			Directory.CreateDirectory(targetDir);
			File.WriteAllText(targetFilename, JsonConvert.SerializeObject(mwData, Newtonsoft.Json.Formatting.Indented));
		}
	}
}


public class Cache
{
}

public class CallFrame
{
	public string functionName { get; set; }
	public string scriptId { get; set; }
	public string url { get; set; }
	public int lineNumber { get; set; }
	public int columnNumber { get; set; }
}

public class Content
{
	public int size { get; set; }
	public string mimeType { get; set; }
	public string text { get; set; }
	public string encoding { get; set; }
}

public class Cookie
{
	public string name { get; set; }
	public string value { get; set; }
	public string path { get; set; }
	public string domain { get; set; }
	public DateTime expires { get; set; }
	public bool httpOnly { get; set; }
	public bool secure { get; set; }
	public string sameSite { get; set; }
}

public class Creator
{
	public string name { get; set; }
	public string version { get; set; }
}

public class HAREntry
{
	public Initiator _initiator { get; set; }
	public string _priority { get; set; }
	public string _resourceType { get; set; }
	public Cache cache { get; set; }
	public string connection { get; set; }
	public string pageref { get; set; }
	public Request request { get; set; }
	public Response response { get; set; }
	public string serverIPAddress { get; set; }
	public DateTime startedDateTime { get; set; }
	public double time { get; set; }
	public Timings timings { get; set; }
}

public class Header
{
	public string name { get; set; }
	public string value { get; set; }
}

public class Initiator
{
	public string type { get; set; }
	public Stack stack { get; set; }
}

public class Log
{
	public string version { get; set; }
	public Creator creator { get; set; }
	public List<Page> pages { get; set; }
	public List<HAREntry> entries { get; set; }
}

public class Page
{
	public DateTime startedDateTime { get; set; }
	public string id { get; set; }
	public string title { get; set; }
	public PageTimings pageTimings { get; set; }
}

public class PageTimings
{
	public double onContentLoad { get; set; }
	public double onLoad { get; set; }
}

public class Parent
{
	public string description { get; set; }
	public List<CallFrame> callFrames { get; set; }
	public Parent parent { get; set; }
}

public class QueryString
{
	public string name { get; set; }
	public string value { get; set; }
}

public class Request
{
	public string method { get; set; }
	public string url { get; set; }
	public string httpVersion { get; set; }
	public List<Header> headers { get; set; }
	public List<QueryString> queryString { get; set; }
	public List<Cookie> cookies { get; set; }
	public int headersSize { get; set; }
	public int bodySize { get; set; }
}

public class Response
{
	public int status { get; set; }
	public string statusText { get; set; }
	public string httpVersion { get; set; }
	public List<Header> headers { get; set; }
	public List<object> cookies { get; set; }
	public Content content { get; set; }
	public string redirectURL { get; set; }
	public int headersSize { get; set; }
	public int bodySize { get; set; }
	public int _transferSize { get; set; }
	public object _error { get; set; }
}

public class HARRoot
{
	public Log log { get; set; }
}

public class Stack
{
	public List<CallFrame> callFrames { get; set; }
	public Parent parent { get; set; }
}

public class Timings
{
	public double blocked { get; set; }
	public double dns { get; set; }
	public double ssl { get; set; }
	public double connect { get; set; }
	public double send { get; set; }
	public double wait { get; set; }
	public double receive { get; set; }
	public double _blocked_queueing { get; set; }
}

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

public class MWEntry
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

public class MatchweekRoot
{
	public CompSeason compSeason { get; set; }
	public bool? live { get; set; }
	public bool? dynamicallyGenerated { get; set; }
	public List<Table> tables { get; set; }
}

public class Table
{
	public int? gameWeek { get; set; }
	public List<MWEntry> entries { get; set; }
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