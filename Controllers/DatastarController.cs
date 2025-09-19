using System.Diagnostics;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using StarFederation.Datastar.DependencyInjection;

using dotnet_html_sortable_table.Models;
using dotnet_html_sortable_table.Data;
using dotnet_html_sortable_table.Extensions;
using dotnet_html_sortable_table.Services;
using StarFederation.Datastar.ModelBinding;
using System.Collections.Concurrent;

namespace dotnet_html_sortable_table.Controllers;

[Route("")]
[Route("Datastar")]
public class DatastarController : Controller
{
    private readonly ILogger<DatastarController> _logger;
    private readonly SqliteContext _context;
    private readonly SessionQueueStore _sessionQueueStore;
    private readonly MessagesContext _messagesContext;
    private readonly BroadcastQueueStore _broadcastQueue;

    public DatastarController(ILogger<DatastarController> logger, SqliteContext context, SessionQueueStore sessionQueueStore, MessagesContext messagesContext, BroadcastQueueStore broadcastQueue)
    {
        _logger = logger;
        _context = context;
        _sessionQueueStore = sessionQueueStore;
        _messagesContext = messagesContext;
        _broadcastQueue = broadcastQueue;
    }

#region NormalRoutes

    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index()
    {
        return View();
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

#endregion

#region SortableList

    public record Signals(SortJson sort, int count);
    public record SortJson(int col, bool direction, int size = 100);

    [HttpGet("SortableList")]
    public async Task SortableList([FromServices] IDatastarService sse) 
    {
        // Fetch a session key stored within the browser session
        var sessionKey = HttpContext.Session.GetString("sortable");

        if (sessionKey == null)
        {
            await sse.ExecuteScriptAsync("location.reload();");
            return;
        }

        _logger.LogInformation($"Grabbing queue for {sessionKey} in {nameof(SortableList)}");
        
        // Grab the queue to listen for incoming requests from
        var queue = _sessionQueueStore.GetOrCreate(sessionKey);

        while (true) 
        {
            // Blocking "take" from queue
            var sortEvent = queue.Take(HttpContext.RequestAborted);

            if (HttpContext.RequestAborted.IsCancellationRequested)
            {
                return;
            }

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

#region Conversation
    public record NewMessage(Guid senderid, string message, string roomcode, string ipv4);
    public record MessageEvent(string Type, Guid sourceSessionId, string roomCode);
    private readonly string ConversationCookieString = "conversation";

    [HttpGet("SelectRoom")]
    public IActionResult SelectRoom()
    {
        return View("SelectRoom");
    }


    [HttpPost("SelectRoom")]
    [ValidateAntiForgeryToken]
    public IActionResult SelectRoom([Bind] SelectRoomViewModel room)
    {
        if (!ModelState.IsValid)
        {
            // Invalid room code given (empty)
            _logger.LogWarning($"{DateTime.Now.ToString("G")}: Invalid room code given");
            return View("SelectRoom", room);
        }

        return RedirectToAction("Conversation", new { room.RoomCode });
    }


    [HttpGet("Conversation")]
    public IActionResult Conversation([FromQuery] string roomCode)
    {
        // This is the initial endpoint hit for the page to load everything

        // Fetch a session key stored within the browser session
        var sessionKey = HttpContext.Session.GetString(ConversationCookieString);
        var isParsed = Guid.TryParse(sessionKey, out Guid currentSenderId);
        var models = 
            _messagesContext.Messages
            .Where(m => m.ChatRoomKey == roomCode)
            .Select(m => new MessageViewModel() { DateCreated = m.DateCreated, MessageContent = m.MessageContent, SenderSessionID = m.SenderSessionID, IsMine = m.SenderSessionID == currentSenderId })
            .ToList();

        Guid myId = isParsed ? currentSenderId : Guid.NewGuid();
        if (!isParsed)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("G")}: New user joined a chat room");

            HttpContext.Session.SetString(ConversationCookieString, myId.ToString());
            var newUser = new ConversationUserModel()
            {
                IsStreaming = true, 
                SessionId = myId,
            };
            _messagesContext.Add(newUser);
            _messagesContext.SaveChanges();
        }

        // Check if user is currently running SSE updates
        bool sseRunning = !isParsed ? true : _messagesContext.ConversationUsers.AsNoTracking().First(m => m.SessionId == myId).IsStreaming;
        return View(new ChatViewModel { Messages = models, MySenderId = myId, SSERunning = sseRunning, RoomCode = roomCode });
    }

    [HttpGet("ConversationSSE")]
    public async Task ConversationSSE([FromServices] IDatastarService sse)
    {
        // This is the SSE connection endpoint that 

        // Fetch a session key stored within the browser session
        string? sessionKey = HttpContext.Session.GetString(ConversationCookieString);
        var sessionObj = await sse.ReadSignalsAsync<NewMessage>();

        bool sessionFlag = false, sessionFlag1 = false;
        if (sessionKey == null || !Guid.TryParse(sessionKey, out Guid sessionKeyGuid))
        {
            _logger.LogWarning($"{DateTime.Now.ToString("G")}: SSE Endpoint has no key: {sessionKey}");
            sessionFlag = true;
        } else if (sessionObj == null || sessionObj.senderid == Guid.Empty)
        {
            _logger.LogWarning($"{DateTime.Now.ToString("G")}: SSE Endpoint has no key: {JsonSerializer.Serialize(sessionObj)}");
            sessionFlag1 = true;
        }

        if (sessionFlag && sessionFlag1)
        {
            _logger.LogWarning($"{DateTime.Now.ToString("G")}: SSE Endpoint has no key whatsoever");

            await sse.ExecuteScriptAsync("location.reload();");
            return;
        }

        _logger.LogInformation($"{DateTime.Now.ToString("G")}: SSE Endpoint has key: {sessionKey}");
        sessionKey ??= sessionObj.senderid.ToString();

        // Grab the queue to listen for incoming requests from
        var queue = _broadcastQueue.GetOrCreate(sessionKey);

        while (true)
        {
            var eventString = queue.Take(HttpContext.RequestAborted);

            if (HttpContext.RequestAborted.IsCancellationRequested)
            {
                _logger.LogInformation($"{DateTime.Now.ToString("G")}: Request was cancelled for user {sessionKey}");
                return;
            }

            _logger.LogInformation($"{DateTime.Now.ToString("G")}: Event found: {eventString}");

            MessageEvent? eventObj = JsonSerializer.Deserialize<MessageEvent>(eventString);

            if (eventObj == null)
            {
                _logger.LogError($"{DateTime.Now.ToString("G")}: Event could not be parsed: {eventString}");
                continue;
            }

            if (eventObj.roomCode != sessionObj.roomcode && eventObj.Type == "refresh")
            {
                _logger.LogInformation($"{DateTime.Now.ToString("G")}: Event in SSE endpoint does not have matching room code so we are skipping the event: {JsonSerializer.Serialize(eventObj)}");
                continue;
            }

            var html = eventObj.Type switch
            {
                "refresh" => await MessageHelper(sessionKey, eventObj.sourceSessionId, eventObj.roomCode),
                "stop" => await UserStreamingChange(sessionKey, false),
                "start" => await UserStreamingChange(sessionKey, true),
                _ => ""
            };

            _logger.LogInformation($"{DateTime.Now.ToString("G")}: Patching new refresh contents with length: {html.Length}");
            await sse.PatchElementsAsync(html);
        }
    }

    private async Task<string> UserStreamingChange(string? sessionKey, bool status)
    {
        if (sessionKey == null || !Guid.TryParse(sessionKey, out Guid sessionKeyGuid))
        {
            _logger.LogError($"{DateTime.Now.ToString("G")}: Could not parse guid: {sessionKey}");
            return "";
        }

        var user = _messagesContext.ConversationUsers.First(m => m.SessionId == sessionKeyGuid);
        user.IsStreaming = status;
        await _messagesContext.SaveChangesAsync();

        return await this.RenderViewToStringAsync("_ConversationSSE", status);
    }

    private async Task<string> MessageHelper(string? sessionKey, Guid eventSource, string roomCode)
    {

        if (sessionKey == null || !Guid.TryParse(sessionKey, out Guid sessionKeyGuid))
        {
            _logger.LogError($"{DateTime.Now.ToString("G")}: Could not parse guid: {sessionKey}");
            return "";
        }

        var models =
            _messagesContext.Messages
            .Where(m => m.ChatRoomKey == roomCode)
            .Select(m => new MessageViewModel() { DateCreated = m.DateCreated, MessageContent = m.MessageContent, SenderSessionID = m.SenderSessionID, IsMine = m.SenderSessionID == sessionKeyGuid })
            .ToList();

        var user = _messagesContext.ConversationUsers.AsNoTracking().First(m => m.SessionId == sessionKeyGuid);
        bool sseRunning = user.IsStreaming;

        if (!sessionKeyGuid.Equals(eventSource) && !sseRunning)
        {
            // Event isn't from current user and the current user doesn't have sse running so return nothing
            return "";
        }

        var viewModel = new ChatViewModel() { Messages = models, MySenderId = sessionKeyGuid, SSERunning = sseRunning, RoomCode = roomCode };
        return await this.RenderViewToStringAsync("Conversation", viewModel, isPartial: false);
    }


    [HttpPost("ConversationMessage")]
    public async Task ConversationMessage([FromServices] IDatastarService sse)
    {
        // Fetch a session key stored within the browser session
        var sessionKey = HttpContext.Session.GetString(ConversationCookieString);
        NewMessage? message = await sse.ReadSignalsAsync<NewMessage>();

        if (message is null || !Guid.TryParse(sessionKey, out Guid sessionKeyGuid) || sessionKeyGuid != message.senderid)
        {
            // TODO Patch something to refresh the page
            _logger.LogError($"{DateTime.Now.ToString("G")}: Something went wrong with creating a message: {sessionKey}");
            await sse.ExecuteScriptAsync("location.reload();");
            return;
        }

        // Create a new conversation message to be applied to chatroom
        MessageModel model = new()
        {
            MessageContent = message.message,
            SenderSessionID = sessionKeyGuid,
            ChatRoomKey = message.roomcode,
            SendIPv4 = message.ipv4
        };

        _messagesContext.Add(model);
        await _messagesContext.SaveChangesAsync();

        // Grab the queue to listen for incoming requests from
        var queue = _broadcastQueue.GetOrCreate(sessionKey);

        await sse.PatchSignalsAsync(message with { message = string.Empty });

        _broadcastQueue.Broadcast(JsonSerializer.Serialize(new MessageEvent("refresh", sessionKeyGuid, message.roomcode)));
    }

    [HttpGet("StopConversation")]
    public async Task StopConversation([FromServices] IDatastarService sse)
    {
        // Stop the conversation from flowing to the client browser by sending fragment without the SSE connection initiator
        var sessionKey = HttpContext.Session.GetString(ConversationCookieString);
        if (Guid.TryParse(sessionKey, out Guid sessionKeyGuid))
        {
            if (_broadcastQueue.TryGet(sessionKey, out var queue))
            {
                queue.Add(JsonSerializer.Serialize(new MessageEvent("stop", sessionKeyGuid, string.Empty)));
            }
        }
    }

    [HttpGet("PlayConversation")]
    public async Task PlayConversation([FromServices] IDatastarService sse)
    {
        // Resume/start the conversation flowing to the client browser by sending fragment with the SSE connection initiator
        var sessionKey = HttpContext.Session.GetString(ConversationCookieString);
        if (Guid.TryParse(sessionKey, out Guid sessionKeyGuid))
        {
            if (_broadcastQueue.TryGet(sessionKey, out var queue))
            {
                queue.Add(JsonSerializer.Serialize(new MessageEvent("start", sessionKeyGuid, string.Empty)));
            }
        }
    }
 
#endregion
}
