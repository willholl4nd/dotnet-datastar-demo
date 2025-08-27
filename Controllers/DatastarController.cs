using System.Diagnostics;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using StarFederation.Datastar.DependencyInjection;

using dotnet_html_sortable_table.Models;
using dotnet_html_sortable_table.Data;
using dotnet_html_sortable_table.Extensions;
using dotnet_html_sortable_table.Services;

namespace dotnet_html_sortable_table.Controllers;

[Route("")]
[Route("Datastar")]
public class DatastarController : Controller
{
    private readonly ILogger<DatastarController> _logger;
    private readonly SqliteContext _context;
    private readonly SessionQueueStore _sessionQueueStore;

    public DatastarController(ILogger<DatastarController> logger, SqliteContext context, SessionQueueStore sessionQueueStore)
    {
        _logger = logger;
        _context = context;
        _sessionQueueStore = sessionQueueStore;
    }

#region NormalRoutes

    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet("GetDateTime")]
    public IActionResult GetDateTime() 
    {
        return PartialView("_LayoutDatetime", DateTime.Now);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

#endregion

#region IncrementalSearch

    public record Filter(int size, string search);

    [HttpGet("Accounts")]
    public IActionResult AccountsList() 
    {
        return View("AccountsList");
    }

    [HttpPost("AccountsListFilter")]
    public async Task AccountsListFilter([FromBody] Filter filter, [FromServices] IDatastarService sse) 
    {
        IEnumerable<Accounts> accounts;
        int count;
        if (filter != null && !filter.Equals("")) {
            accounts = (from row in _context.Accounts
            where row.FirstName.Contains(filter.search)
            select row).Take(filter.size).ToList();
            
            count = (from row in _context.Accounts
            where row.FirstName.Contains(filter.search)
            select row).Count();
        } else {
            accounts = _context.Accounts.Take(filter.size).ToList();
            count = _context.Accounts.Count();
        }

        var tableHtml = await this.RenderViewToStringAsync("_AccountListTable", accounts, true);
        await sse.PatchElementsAsync(tableHtml);

        var countHtml = await this.RenderViewToStringAsync("_AccountListCount", count, true);
        await sse.PatchElementsAsync(countHtml);
    }

#endregion

#region InfiniteScroll

    public record Infinite(bool split, int offset, int size);

    [HttpPost("Scroll")]
    public async Task Scrollv1([FromBody] Infinite signals, [FromServices] IDatastarService sse) 
    {
        DemoObject d = _context.TableContainer.First(m => m.Id == 1);
        var table = 
            (from row in _context.Entries 
                where row.DemoObjectId == d.Id && row.Id >= signals.offset 
                select row)
            .OrderBy(m => m.Id)
            .Take(signals.size)
            .ToList();
        d.Table = table;

        var options = new PatchElementsOptions 
        {
            Selector = "#tablecontent", 
            PatchMode = StarFederation.Datastar.ElementPatchMode.Append
        };

        if (signals.split) 
        {
            bool divBy2 = signals.size % 2 == 0;
            int takeAmount = divBy2 ? signals.size / 2 : (signals.size - 1) / 2;

            var first = table.Take(takeAmount);
            var second = table.TakeLast(signals.size - takeAmount);

            var moreRows = await this.RenderViewToStringAsync("_InfiniteData", first, true);
            await sse.PatchElementsAsync(moreRows, options);


            if (table.Count() > 0) 
            {
                var intersector = await this.RenderViewToStringAsync("_InfiniteIntersector", null, true);
                await sse.PatchElementsAsync(intersector, options);
            }

            var moreRows2 = await this.RenderViewToStringAsync("_InfiniteData", second, true);
            await sse.PatchElementsAsync(moreRows2, options);
        }
        else 
        {
            var moreRows = await this.RenderViewToStringAsync("_InfiniteData", table, true);
            await sse.PatchElementsAsync(moreRows, options);

            if (table.Count() > 0) 
            {
                var intersector = await this.RenderViewToStringAsync("_InfiniteIntersector", null, true);
                await sse.PatchElementsAsync(intersector, options);
            }
        }

        await sse.PatchSignalsAsync(signals with { offset = signals.offset + signals.size });
    }

    [HttpGet("Scroll")]
    public IActionResult Scroll() 
    {
        DemoObject d = _context.TableContainer.First(m => m.Id == 1);
        var table = 
            (from row in _context.Entries 
                where row.DemoObjectId == d.Id && row.Id >= 100 * 1
                select row)
            .OrderBy(m => m.Id)
            .Take(100)
            .ToList();

        d.Table = table;

        return View("InfiniteScroll", d);
    }

    [HttpGet("OffsetInfo/{offset}")]
    public IActionResult OffsetInfo(int offset)
    {
        Response.Headers.Add("Vary", "HX-Request");
        return PartialView("OffsetInfo", offset);
    }

#endregion

#region SortableList

    public record Signals(SortJson sort, int count);
    public record SortJson(int col, bool direction, int size = 100);

    [HttpGet("SortableList")]
    public async Task SortableList([FromServices] IDatastarService sse) 
    {
        // Fetch a session key stored within the browser session
        var sessionKey = HttpContext.Session.GetString("sortable");

        _logger.LogInformation($"Grabbing queue for {sessionKey} in {nameof(SortableList)}");
        
        // Grab the queue to listen for incoming requests from
        var queue = _sessionQueueStore.GetOrCreate(sessionKey);

        while (true) 
        {
            // Blocking "take" from queue
            var sortEvent = queue.Take();

            // Now that a request has come through, fetch all details from the queue
            SortJson? sort = (SortJson?) JsonSerializer.Deserialize(sortEvent, typeof(SortJson));

            _logger.LogInformation($"Event found in {nameof(SortableList)} with value {sortEvent}");

            if (sort != null) 
            {
                // Sort the table according to the request
                DemoObject d = _context.TableContainer.First(m => m.Id == 1);
                List<DemoTable> table = 
                    (from row in _context.Entries where row.DemoObjectId == d.Id select row).Take(sort.size).ToList();
                d.Table = table;

                _logger.LogInformation("Changing the sort of the table");
                ChangeSort(d, sort.col, !sort.direction);

                // Render the table as HTML
                _logger.LogInformation("Rendering Table view to HTML");
                var htmlString = await this.RenderViewToStringAsync("_TableData", d.Table, true);
                _logger.LogInformation("Finished rendering table to HTML");

                // Send the table down to the client
                await sse.PatchElementsAsync(htmlString, new PatchElementsOptions { PatchMode = StarFederation.Datastar.ElementPatchMode.Outer });
                _logger.LogInformation("Finished sending table to client");

            }

            // Send the loading indicator down
            var loading = await this.RenderViewToStringAsync("_TableLoading", false, true);
            await sse.PatchElementsAsync(loading);

            // Update signal for loading indicator to reset it
            if (sort != null)
                await sse.PatchSignalsAsync(new { count = 0 });

            // Send down updated table headers so that user can sort the table again
            var headers = await this.RenderViewToStringAsync("_TableHeaders", false, true);
            await sse.PatchElementsAsync(headers);

            // Send down time operation was completed
            _logger.LogInformation("Sending Last Updated fragment");
            await sse.PatchElementsAsync($"<div id='test' class='text-center mb-3 ft-2'>Last Updated {DateTime.Now.ToLongTimeString()}</div>");
            _logger.LogInformation("Finished sending Last Updated fragment");
        }
    }

    [HttpPost("SortableSortBy")]
    public async Task SortableSortBy([FromBody] Signals signals, [FromServices] IDatastarService sse) 
    {
        var sessionKey = HttpContext.Session.GetString("sortable");

        _logger.LogInformation($"Grabbing queue for {sessionKey} in {nameof(SortableSortBy)}");

        // Grab the queue to make requests
        var queue = _sessionQueueStore.GetOrCreate(sessionKey);

        // Send fragment down to client to show loading indicator
        var loading = await this.RenderViewToStringAsync("_TableLoading", true, true);
        await sse.PatchElementsAsync(loading);

        // Send fragment down to client to disable further sorting
        var headers = await this.RenderViewToStringAsync("_TableHeaders", true, true);
        await sse.PatchElementsAsync(headers);

        // Create request on queue for long-living endpoint to pickup
        queue.Add(JsonSerializer.Serialize(signals.sort));

        _logger.LogInformation($"Triggered the {nameof(SortableSortBy)} with value {signals.sort.col} and {signals.sort.direction}");
    }


    [HttpGet("Demo")]
    public IActionResult Demo() 
    {
        HttpContext.Session.SetString("sortable", Guid.NewGuid().ToString());

        int size = 100;
        DemoObject d = _context.TableContainer.First(m => m.Id == 1);
        List<DemoTable> table = 
            (from row in _context.Entries where row.DemoObjectId == d.Id select row).Take(size).ToList();
        d.Table = table;

        return View("Table", d);
    }


    private void ChangeSort(DemoObject data, int sortIdx, bool direction) 
    {
        Func<bool, string> sortType = (bool sort) => sort ? "Asc" : "Desc";
        switch (sortIdx) {
            case 1: {
                        bool temp = direction;
                        data.Table = temp ? data.Table.OrderBy(m => m.Id).ToList() : data.Table.OrderByDescending(m => m.Id).ToList();
                        data.IdSort = temp;
                        break;
                    }
            case 2: {
                        bool temp = direction;
                        data.Table = temp ? data.Table.OrderBy(m => m.RandInt).ToList() : data.Table.OrderByDescending(m => m.RandInt).ToList();
                        data.RandIntSort = temp;
                        break;
                    }
            case 3: {
                        bool temp = direction;
                        data.Table = temp ? data.Table.OrderBy(m => m.Name).ToList() : data.Table.OrderByDescending(m => m.Name).ToList();
                        data.NameSort = temp;
                        break;
                    }

           default: {
                        bool temp = direction;
                        data.Table = temp ? data.Table.OrderBy(m => m.Id).ToList() : data.Table.OrderByDescending(m => m.Id).ToList();
                        data.IdSort = temp;
                        break;
                    }
        }
    }

#endregion

#region Pagination

    [HttpGet("Pagination")]
    public IActionResult Pagination() 
    {
        int size = 100;
        int offset = 0;
        int id = 1;

        var table = 
            (from row in _context.Entries 
                where row.DemoObjectId == id && row.Id >= size * offset 
                select row)
            .OrderBy(m => m.Id)
            .Take(size)
            .ToList();

        int backwardOffset, forwardOffset;
        DeterminePageOffset(id, size, offset, out backwardOffset, out forwardOffset);

        return View("PaginationTable", table);
    }

    [HttpPost("PaginationTable")]
    public async Task PaginationTable([FromQuery] int offset, [FromBody] PaginationRecord pagination, [FromServices] IDatastarService sse) 
    {
        var table = 
            (from row in _context.Entries 
                where row.DemoObjectId == 1 && row.Id >= pagination.size * offset 
                select row)
            .OrderBy(m => m.Id)
            .Take(pagination.size)
            .ToList();

        int backwardOffset, forwardOffset;
        DeterminePageOffset(1, pagination.size, offset, out backwardOffset, out forwardOffset);

        var paginationTable = await this.RenderViewToStringAsync("_PageTable", table, true);
        await sse.PatchElementsAsync(paginationTable);

        var pageData = new PaginationData(offset, forwardOffset, backwardOffset, table.Count());
        var paginationButtons = await this.RenderViewToStringAsync("_PageCount", pageData, true);
        await sse.PatchElementsAsync(paginationButtons);
    }

    private void DeterminePageOffset(int id, int size, int currentOffset, out int backOffset, out int forOffset) 
    {
        double count = _context.Entries.Where(m => m.DemoObjectId == id).Count();
        double divisions = count / size;

        backOffset = currentOffset switch {
            0 => (int)divisions,
            > 0 => currentOffset - 1,
            _ => throw new Exception("How are we even here?")
        };

        if ((int) divisions == currentOffset) {
            forOffset = 0;
        } else if((int) divisions > currentOffset) {
            forOffset = currentOffset + 1;
        } else {
            throw new Exception("How are we even here?");
        }
    }

#endregion
}
