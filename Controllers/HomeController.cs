using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Htmx;

using dotnet_html_sortable_table.Models;
using dotnet_html_sortable_table.Data;

namespace dotnet_html_sortable_table.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly SqliteContext _context;
    List<TodoItem> TodoList;

    public HomeController(ILogger<HomeController> logger, SqliteContext context)
    {
        _logger = logger;
        _context = context;
        TodoList = [ new TodoItem { Id = 1, Name = "Grocery shop" }, new TodoItem { Id = 2, Name = "Cook" }, new TodoItem { Id = 3, Name = "Sleep" } ];
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet("GetDateTime")]
    public IActionResult GetDateTime() {
        return PartialView("_LayoutDatetime", DateTime.Now);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }


    [HttpGet("Accounts")]
    public IActionResult AccountsList([FromQuery] int? size) {
        size ??= 100;
        var accounts = _context.Accounts.Take(size.Value).ToList();

        TempData["count"] = _context.Accounts.Count();
        ViewData["isFullRender"] = true;
        ViewData["size"] = size;
        return View("AccountsList", accounts);
    }

    [HttpPost("AccountsListFilter")]
    public async Task<IActionResult> AccountsListFilter(string search, [FromQuery] int? size) {
        size ??= 100;

        IEnumerable<Accounts> accounts;
        int count;
        if (search != null && !search.Equals("")) {
            accounts = (from row in _context.Accounts
            where row.FirstName.Contains(search)
            select row).Take(size.Value).ToList();
            
            count = (from row in _context.Accounts
            where row.FirstName.Contains(search)
            select row).Count();
        } else {
            accounts = _context.Accounts.Take(size.Value).ToList();
            count = _context.Accounts.Count();
        }

        TempData["count"] = count;
        ViewData["isFullRender"] = false;
        ViewData["size"] = size;
        // Custom event that triggers the count to reload -- good for if multiple different sources on page can alter count
        Response.Htmx(h => {
                h.WithTrigger("updateCount");
            });
        Response.Headers.Add("Vary", "HX-Request");
        return PartialView("_SearchTable", accounts);
    }

    [HttpGet("AccountsListCount")]
    public IActionResult AccountsListCount() {
        if (!Request.IsHtmx())
            return RedirectToAction("AccountsList", "Home");

        return PartialView("_SearchTableCount", (int)TempData["count"]);
    }


    [HttpGet("Scroll/{id?}")]
    public IActionResult Scroll([FromQuery] int? size, [FromQuery] int? offset = 0, int? id = 1, [FromQuery] bool? split = false) {
        size ??= 100;
        DemoObject d = _context.TableContainer.First(m => m.Id == id);
        var table = 
            (from row in _context.Entries 
                where row.DemoObjectId == d.Id && row.Id >= size * offset 
                select row)
            .OrderBy(m => m.Id)
            .Take(size.Value)
            .ToList();

        d.Table = table;

        if (HttpContext.Request.IsHtmx())
        {
            offset++;
            ViewData["offset"] = offset;
            ViewData["size"] = size;
            ViewData["split"] = split;
            Response.Headers.Add("Vary", "HX-Request");
            return PartialView("_ScrollTable", table);
        } else {
            ViewData["offset"] = 1;
            ViewData["size"] = size;
            ViewData["split"] = split;
            return View("InfiniteScroll", d);
        }
    }

    [HttpGet("OffsetInfo/{offset}")]
    public IActionResult OffsetInfo(int offset){
        Response.Headers.Add("Vary", "HX-Request");
        return PartialView("OffsetInfo", offset);
    }

    [HttpGet("Demo/{sortIdx?}")]
    public IActionResult Demo(int? sortIdx, [FromQuery] int? size, [FromQuery] int id = 1) {
        size ??= 100;
        ViewData["Size"] = size;
        DemoObject d = _context.TableContainer.First(m => m.Id == id);
        List<DemoTable> table = 
            (from row in _context.Entries where row.DemoObjectId == d.Id select row).Take(size.Value).ToList();
        d.Table = table;

        if (HttpContext.Request.IsHtmx())
        {
            sortIdx ??= 1;
            ChangeSort(d, sortIdx.Value);

            Response.Headers.Add("Vary", "HX-Request");
            return PartialView("_TableData", d.Table);
        } else {
            return View("Table", d);
        }
    }

    private void ChangeSort(DemoObject data, int sortIdx) {
        foreach (var key in TempData.Keys)
            Console.WriteLine($"Key in TempData: {key}");
        Func<bool, string> sortType = (bool sort) => sort ? "Asc" : "Desc";
        switch (sortIdx) {
            case 1: {
                        bool temp = !data.IdSort;
                        Console.WriteLine($"Sort Id: {sortType(temp)}");
                        data.Table = temp ? data.Table.OrderBy(m => m.Id).ToList() : data.Table.OrderByDescending(m => m.Id).ToList();
                        data.IdSort = temp;
                        break;
                    }
            case 2: {
                        bool temp = !data.RandIntSort;
                        Console.WriteLine($"Sort RandInt: {sortType(temp)}");
                        data.Table = temp ? data.Table.OrderBy(m => m.RandInt).ToList() : data.Table.OrderByDescending(m => m.RandInt).ToList();
                        data.RandIntSort = temp;
                        break;
                    }
            case 3: {
                        bool temp = !data.NameSort;
                        Console.WriteLine($"Sort Name: {sortType(temp)}");
                        data.Table = temp ? data.Table.OrderBy(m => m.Name).ToList() : data.Table.OrderByDescending(m => m.Name).ToList();
                        data.NameSort = temp;
                        break;
                    }

            default: {
                         bool temp = !data.IdSort;
                        Console.WriteLine($"Sort Id: {sortType(temp)}");
                         data.Table = temp ? data.Table.OrderBy(m => m.Id).ToList() : data.Table.OrderByDescending(m => m.Id).ToList();
                         data.IdSort = temp;
                         break;
                     }
        }
    }

    [HttpGet("Pagination")]
    public IActionResult Pagination([FromQuery] int? size, [FromQuery] int? offset) {
        size ??= 100;
        offset ??= 0;
        int id = 1;

        var table = 
            (from row in _context.Entries 
                where row.DemoObjectId == id && row.Id >= size * offset 
                select row)
            .OrderBy(m => m.Id)
            .Take(size.Value)
            .ToList();

        int backwardOffset, forwardOffset;
        DeterminePageOffset(id, size.Value, offset.Value, out backwardOffset, out forwardOffset);

        TempData["size"] = size;
        TempData["forwardoffset"] = forwardOffset;
        TempData["backwardoffset"] = backwardOffset;
        TempData["count"] = table.Count();
        return View("PaginationTable", table);
    }

    [HttpGet("PaginationTable")]
    public IActionResult PaginationTable([FromQuery] int? size, [FromQuery] int? offset) {
        if (!Request.IsHtmx()) {
            return RedirectToAction("Pagination");
        }

        size ??= 100;
        offset ??= 0;
        int id = 1;

        var table = 
            (from row in _context.Entries 
                where row.DemoObjectId == id && row.Id >= size * offset 
                select row)
            .OrderBy(m => m.Id)
            .Take(size.Value)
            .ToList();

        int backwardOffset, forwardOffset;
        DeterminePageOffset(id, size.Value, offset.Value, out backwardOffset, out forwardOffset);

        TempData["size"] = size;
        TempData["forwardoffset"] = forwardOffset;
        TempData["backwardoffset"] = backwardOffset;
        TempData["count"] = table.Count();
        Response.Htmx(h => {
                h.WithTrigger("updateCount");
            }); // Trigger button to refresh
        return PartialView("_PageTable", table);
    }

    [HttpGet("PaginationCount")]
    public IActionResult PaginationCount() {
        return PartialView("_PageCount");
    }

    private void DeterminePageOffset(int id, int size, int currentOffset, out int backOffset, out int forOffset) {
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

    [HttpGet("TodoList")]
    public IActionResult GetTodoList() {
        return View("TodoList", TodoList);
    }

    [HttpPost("AddTodoItem")]
    public void AddTodoItem([FromForm] string ItemName) {
        int maxId = (from row in TodoList select row.Id).Max();
        TodoList.Add(new TodoItem { Id = maxId+1, Name = ItemName });
    }

    [HttpDelete("DeleteTodoItem/{id}")]
    public void DeleteTodoItem(int id) {
        TodoItem removed = (from row in TodoList where row.Id == id select row).First();
        TodoList.Remove(removed);
    }
}

public class TodoItem {
    public int Id { get; set; }
    public string Name { get; set; } = default!;

}
