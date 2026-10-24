using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace EventTicketing.E2ETests;

public class AdminTests : E2ETestBase
{
    private async Task SignInAsAdminAsync()
    {
        await Page.GotoAsync($"{BaseUrl}/login");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).Nth(1).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/$"));
    }

    [Test]
    public async Task AdminOpensTheAdminPageFromTheHeader()
    {
        await SignInAsAdminAsync();

        await Page.GetByRole(AriaRole.Banner).GetByRole(AriaRole.Link, new() { Name = "Events", Exact = true }).ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Admin" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task AdminPageListsEventsWithEditLinks()
    {
        await SignInAsAdminAsync();
        await Page.GotoAsync($"{BaseUrl}/admin");

        var list = Page.GetByRole(AriaRole.List, new() { Name = "admin events" });
        await Expect(list.GetByRole(AriaRole.Listitem).Filter(new() { HasText = "Jazz Night" })).ToContainTextAsync("Blue Note Club");
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Edit Jazz Night" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task AdminDeletesAnEventAfterConfirming()
    {
        await SignInAsAdminAsync();
        var title = $"E2E Delete {Guid.NewGuid():N}"[..20];
        var created = await Page.APIRequest.PostAsync($"{BaseUrl}/api/v1/events", new()
        {
            DataObject = new
            {
                title,
                description = "Created by an E2E test.",
                startTime = DateTime.UtcNow.AddDays(30),
                endTime = DateTime.UtcNow.AddDays(30).AddHours(2),
                venue = "Test Hall",
                eventType = "Other",
                totalSeats = 10,
                availableSeats = 10,
                priceCents = 100,
            },
        });
        Assert.That(created.Status, Is.EqualTo(201));
        await Page.GotoAsync($"{BaseUrl}/admin");

        await Page.GetByRole(AriaRole.Button, new() { Name = $"Delete {title}" }).ClickAsync();
        await Page.GetByRole(AriaRole.Dialog).GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();

        await Expect(Page.GetByRole(AriaRole.List, new() { Name = "admin events" }).GetByText(title)).ToHaveCountAsync(0);
        var gone = await Page.APIRequest.GetAsync($"{BaseUrl}/api/v1/events?q={Uri.EscapeDataString(title)}");
        Assert.That((await gone.TextAsync()).Contains(title), Is.False);
    }

    [Test]
    public async Task AdminArchivesAnEventThatHasOrders()
    {
        await SignInAsAdminAsync();
        var title = $"E2E Archive {Guid.NewGuid():N}"[..21];
        var created = await Page.APIRequest.PostAsync($"{BaseUrl}/api/v1/events", new()
        {
            DataObject = new
            {
                title,
                description = "Created by an E2E test.",
                startTime = DateTime.UtcNow.AddDays(30),
                endTime = DateTime.UtcNow.AddDays(30).AddHours(2),
                venue = "Test Hall",
                eventType = "Other",
                totalSeats = 10,
                availableSeats = 10,
                priceCents = 0,
            },
        });
        Assert.That(created.Status, Is.EqualTo(201));
        var id = (await created.JsonAsync())!.Value.GetProperty("data").GetProperty("id").GetInt32();
        try
        {
            await Page.GotoAsync($"{BaseUrl}/admin");
            await Expect(Page.GetByRole(AriaRole.Button, new() { Name = $"Delete {title}" })).ToBeVisibleAsync();

            // A guest books a free ticket from a separate, signed-out context (admins can't book).
            await using var guest = await Playwright.APIRequest.NewContextAsync();
            var booked = await guest.PostAsync($"{BaseUrl}/api/v1/events/{id}/bookings", new() { DataObject = new { email = "e2e-guest@example.com" } });
            Assert.That(booked.Status, Is.EqualTo(201));

            // The open page hears about the booking over SignalR and swaps Delete for Archive without a reload.
            var archive = Page.GetByRole(AriaRole.Button, new() { Name = $"Archive {title}" });
            await Expect(archive).ToBeVisibleAsync();
            await archive.ClickAsync();
            await Page.GetByRole(AriaRole.Dialog).GetByRole(AriaRole.Button, new() { Name = "Archive" }).ClickAsync();

            await Expect(Page.GetByRole(AriaRole.List, new() { Name = "admin events" }).GetByText(title)).ToHaveCountAsync(0);
            await Page.GotoAsync($"{BaseUrl}/events/{id}");
            await Expect(Page.GetByText("This event has been archived and no longer sells tickets.")).ToBeVisibleAsync();
        }
        finally
        {
            await RemoveEventAsync(id);
        }
    }

    [Test]
    public async Task AdminCreatesAnEventThroughTheForm()
    {
        await SignInAsAdminAsync();
        var title = $"E2E Create {Guid.NewGuid():N}"[..20];
        await Page.GotoAsync($"{BaseUrl}/admin/new");

        await Page.GetByLabel("Title").FillAsync(title);
        await Page.GetByLabel("Venue").FillAsync("Test Hall");
        await Page.GetByLabel("Description").FillAsync("Created through the admin form by an E2E test.");
        await Page.GetByLabel("Start time").FillAsync(DateTime.Now.AddDays(40).ToString("yyyy-MM-dd'T'19:00"));
        await Page.GetByLabel("End time").FillAsync(DateTime.Now.AddDays(40).ToString("yyyy-MM-dd'T'22:00"));
        await Page.GetByLabel("Total seats").FillAsync("50");
        await Page.GetByLabel("Available seats").FillAsync("50");
        await Page.GetByLabel("Price ($)").FillAsync("19.99");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create event" }).ClickAsync();

        try
        {
            await Expect(Page).ToHaveURLAsync(new Regex("/admin$"));
            var search = await Page.APIRequest.GetAsync($"{BaseUrl}/api/v1/events?q={Uri.EscapeDataString(title)}");
            var created = (await search.JsonAsync())!.Value.GetProperty("data")[0];
            Assert.That(created.GetProperty("priceCents").GetInt32(), Is.EqualTo(1999));
        }
        finally
        {
            // Leave the shared dev database as we found it.
            var search = await Page.APIRequest.GetAsync($"{BaseUrl}/api/v1/events?q={Uri.EscapeDataString(title)}");
            foreach (var ev in (await search.JsonAsync())!.Value.GetProperty("data").EnumerateArray())
                await RemoveEventAsync(ev.GetProperty("id").GetInt32());
        }
    }

    // A 1×1 transparent PNG.
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=");

    [Test]
    public async Task AdminUploadsAnEventImageThatTheEventPageShows()
    {
        await SignInAsAdminAsync();
        var title = $"E2E Image {Guid.NewGuid():N}"[..19];
        await Page.GotoAsync($"{BaseUrl}/admin/new");

        // The browser PUTs the file to Azurite with the API's write-only URL.
        await Page.GetByLabel("Image").SetInputFilesAsync(new FilePayload { Name = "poster.png", MimeType = "image/png", Buffer = Png });
        await Expect(Page.GetByRole(AriaRole.Img, new() { Name = "Event image preview" })).ToBeVisibleAsync();
        await Page.GetByLabel("Title").FillAsync(title);
        await Page.GetByLabel("Venue").FillAsync("Test Hall");
        await Page.GetByLabel("Description").FillAsync("Has an image uploaded by an E2E test.");
        await Page.GetByLabel("Start time").FillAsync(DateTime.Now.AddDays(41).ToString("yyyy-MM-dd'T'19:00"));
        await Page.GetByLabel("End time").FillAsync(DateTime.Now.AddDays(41).ToString("yyyy-MM-dd'T'22:00"));
        await Page.GetByLabel("Total seats").FillAsync("50");
        await Page.GetByLabel("Available seats").FillAsync("50");
        await Page.GetByLabel("Price ($)").FillAsync("5");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create event" }).ClickAsync();

        try
        {
            await Expect(Page).ToHaveURLAsync(new Regex("/admin$"));
            var search = await Page.APIRequest.GetAsync($"{BaseUrl}/api/v1/events?q={Uri.EscapeDataString(title)}");
            var id = (await search.JsonAsync())!.Value.GetProperty("data")[0].GetProperty("id").GetInt32();
            await Page.GotoAsync($"{BaseUrl}/events/{id}");

            var image = Page.GetByRole(AriaRole.Img, new() { Name = title });
            await Expect(image).ToBeVisibleAsync();
            await Expect(image).ToHaveJSPropertyAsync("naturalWidth", 1);
        }
        finally
        {
            var search = await Page.APIRequest.GetAsync($"{BaseUrl}/api/v1/events?q={Uri.EscapeDataString(title)}");
            foreach (var ev in (await search.JsonAsync())!.Value.GetProperty("data").EnumerateArray())
                await RemoveEventAsync(ev.GetProperty("id").GetInt32());
        }
    }

    [Test]
    public async Task AdminEditsAnEventThroughTheForm()
    {
        await SignInAsAdminAsync();
        var title = $"E2E Edit {Guid.NewGuid():N}"[..18];
        var created = await Page.APIRequest.PostAsync($"{BaseUrl}/api/v1/events", new()
        {
            DataObject = new
            {
                title,
                description = "Edited by an E2E test.",
                startTime = DateTime.UtcNow.AddDays(45),
                endTime = DateTime.UtcNow.AddDays(45).AddHours(2),
                venue = "Test Hall",
                eventType = "Other",
                totalSeats = 10,
                availableSeats = 10,
                priceCents = 1000,
            },
        });
        var id = (await created.JsonAsync())!.Value.GetProperty("data").GetProperty("id").GetInt32();

        try
        {
            await Page.GotoAsync($"{BaseUrl}/admin/{id}/edit");
            await Expect(Page.GetByLabel("Title")).ToHaveValueAsync(title);
            await Page.GetByLabel("Title").FillAsync(title + " v2");
            await Page.GetByLabel("Price ($)").FillAsync("12.50");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Save changes" }).ClickAsync();

            await Expect(Page).ToHaveURLAsync(new Regex("/admin$"));
            var saved = (await (await Page.APIRequest.GetAsync($"{BaseUrl}/api/v1/events/{id}")).JsonAsync())!.Value.GetProperty("data");
            Assert.That(saved.GetProperty("title").GetString(), Is.EqualTo(title + " v2"));
            Assert.That(saved.GetProperty("priceCents").GetInt32(), Is.EqualTo(1250));
        }
        finally
        {
            await RemoveEventAsync(id);
        }
    }

    [Test]
    public async Task EventsCalendarShowsSeededEvents()
    {
        await SignInAsAdminAsync();
        await Page.GotoAsync($"{BaseUrl}/admin/calendar");

        // Seeded Jazz Night is always tomorrow; step to next month when tomorrow falls in it.
        if (DateTime.Now.AddDays(1).Month != DateTime.Now.Month)
            await Page.GetByRole(AriaRole.Button, new() { Name = "next" }).ClickAsync();

        await Expect(Page.GetByText("Jazz Night – 48")).ToBeVisibleAsync();
    }

    [Test]
    public async Task ClickingACalendarEventOpensItsDetails()
    {
        await SignInAsAdminAsync();
        await Page.GotoAsync($"{BaseUrl}/admin/calendar");
        if (DateTime.Now.AddDays(1).Month != DateTime.Now.Month)
            await Page.GetByRole(AriaRole.Button, new() { Name = "next" }).ClickAsync();

        await Page.GetByText("Jazz Night – 48").ClickAsync();

        var popup = Page.GetByRole(AriaRole.Dialog, new() { Name = "Jazz Night" });
        await Expect(popup).ToContainTextAsync("Blue Note Club");
        await Expect(popup).ToContainTextAsync("48 / 120 seats remaining");
        await popup.GetByRole(AriaRole.Link, new() { Name = "Edit" }).ClickAsync();
        await Expect(Page.GetByLabel("Title")).ToHaveValueAsync("Jazz Night");
    }

    [Test]
    public async Task AdminOpensAllOrders()
    {
        await SignInAsAdminAsync();
        await Page.GotoAsync($"{BaseUrl}/admin");

        await Page.GetByRole(AriaRole.Link, new() { Name = "All orders" }).ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "All orders" })).ToBeVisibleAsync();
        await Expect(Page.GetByText("Failed to load orders.")).ToHaveCountAsync(0);
    }

    [Test]
    public async Task AdminSeesTheSeatSummary()
    {
        await SignInAsAdminAsync();
        await Page.GotoAsync($"{BaseUrl}/admin");

        await Page.GetByRole(AriaRole.Link, new() { Name = "Seat summary" }).ClickAsync();

        var jazz = Page.GetByRole(AriaRole.Row).Filter(new() { HasText = "Jazz Night" });
        await Expect(jazz).ToContainTextAsync("120");
        await Expect(jazz).ToContainTextAsync("72");
        await Expect(jazz).ToContainTextAsync("$1,800.00");
    }
}
