using AngleSharp;
using AngleSharp.Html.Dom;
using Modulhandbuch.Parser;

const string ADDRESS = "https://www.informatik.kit.edu/formulare.php";

using var client = new HttpClient();
using var resp = await client.GetAsync(ADDRESS);

// Why can't it do the fetching itself? Who knows!
using var browser = BrowsingContext.New();
var page = await browser.OpenAsync(req => req.Content(resp.Content.ReadAsStream()).Address(ADDRESS));

var links = page.All
    .OfType<IHtmlAnchorElement>()
    .Where(x => x.Text == "de")
    .ToArray();

var bachelor = links.Last(x => x.Href.Contains("/downloads/stud/MHB_BScINFO_")).Href;
var master   = links.Last(x => x.Href.Contains("/downloads/stud/MHB_MScINFO_")).Href;

await Parallel.ForEachAsync(new[] { (bachelor, "bachelor.json"), (master, "master.json") }, async (x, _) => {
    var bytes = await client.GetByteArrayAsync(x.Item1);
    Parser.Parse(bytes, x.Item2);
});
