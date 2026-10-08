using EventTicketing.Api.Models;

namespace EventTicketing.Api.Data;

public static class EventSeeder
{
    // Demo times are wall-clock times in this zone, so "Jazz Night 19:00" shows as 19:00 locally.
    private static readonly TimeZoneInfo DemoTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    private record SeedEvent(
        string Title, string Description, int Day, TimeOnly Start, int EndDay, TimeOnly End,
        string Venue, string EventType, int TotalSeats, int AvailableSeats, int PriceCents, string ImageUrl);

    // The old repo's demo events; Day is the offset from the first demo day, which is
    // re-anchored to tomorrow on every fresh database so the demo always shows upcoming events.
    private static readonly SeedEvent[] Events =
    [
        new(
            "Jazz Night",
            "An intimate evening of live jazz featuring the city's finest musicians. Expect smooth bebop, cool jazz, and soulful improvisation.",
            Day: 0, Start: new TimeOnly(19, 0), EndDay: 0, End: new TimeOnly(22, 0),
            Venue: "Blue Note Club", EventType: "Music",
            TotalSeats: 120, AvailableSeats: 48, PriceCents: 2500,
            ImageUrl: "https://images.unsplash.com/photo-1548163111-bc419d75fef4?w=800&q=80"),
        new(
            "Tech Conference 2026",
            "A full-day conference on modern software development — AI, distributed systems, DevOps and beyond. Keynotes from industry leaders.",
            Day: 3, Start: new TimeOnly(9, 0), EndDay: 3, End: new TimeOnly(18, 0),
            Venue: "City Convention Centre", EventType: "Tech",
            TotalSeats: 500, AvailableSeats: 212, PriceCents: 14900,
            ImageUrl: "https://images.unsplash.com/photo-1582192730841-2a682d7375f9?w=800&q=80"),
        new(
            "Comedy Showcase",
            "Stand-up comedy night featuring five rising comedians. Uncensored, hilarious, and perfect for a night out.",
            Day: 5, Start: new TimeOnly(20, 0), EndDay: 5, End: new TimeOnly(22, 30),
            Venue: "Laugh Factory", EventType: "Comedy",
            TotalSeats: 200, AvailableSeats: 134, PriceCents: 1800,
            ImageUrl: "https://images.unsplash.com/photo-1527224857830-43a7acc85260?w=800&q=80"),
        new(
            "Championship Basketball",
            "Conference finals — top two city teams battle for the championship. High energy, packed arena, one winner.",
            Day: 7, Start: new TimeOnly(19, 0), EndDay: 7, End: new TimeOnly(21, 30),
            Venue: "City Arena", EventType: "Sports",
            TotalSeats: 3000, AvailableSeats: 870, PriceCents: 4500,
            ImageUrl: "https://images.unsplash.com/photo-1546519638-68e109498ffc?w=800&q=80"),
        new(
            "Startup Summit",
            "A full-day gathering for founders, investors, and innovators. Panels on fundraising, product-market fit, and scaling.",
            Day: 10, Start: new TimeOnly(9, 0), EndDay: 10, End: new TimeOnly(18, 0),
            Venue: "Grand Ballroom", EventType: "Business",
            TotalSeats: 300, AvailableSeats: 91, PriceCents: 19900,
            ImageUrl: "https://images.unsplash.com/photo-1515187029135-18ee286d815b?w=800&q=80"),
        new(
            "Broadway Hits Gala",
            "A spectacular evening of Broadway's greatest hits performed by a cast of West End and Broadway veterans.",
            Day: 12, Start: new TimeOnly(19, 30), EndDay: 12, End: new TimeOnly(22, 0),
            Venue: "Empire Theatre", EventType: "Theater",
            TotalSeats: 400, AvailableSeats: 155, PriceCents: 8500,
            ImageUrl: "https://images.unsplash.com/photo-1507676184212-d03ab07a01bf?w=800&q=80"),
        new(
            "Food & Wine Festival",
            "Over 50 local restaurants and wineries in one place. Tastings, live cooking demos, and expert-led wine pairings.",
            Day: 14, Start: new TimeOnly(12, 0), EndDay: 14, End: new TimeOnly(20, 0),
            Venue: "Riverside Park", EventType: "Food",
            TotalSeats: 1000, AvailableSeats: 623, PriceCents: 3500,
            ImageUrl: "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?w=800&q=80"),
        new(
            "Modern Art Exhibition",
            "A curated showcase of contemporary works from 30 emerging artists. Sculpture, digital art, painting, and installation.",
            Day: 15, Start: new TimeOnly(10, 0), EndDay: 15, End: new TimeOnly(18, 0),
            Venue: "City Gallery", EventType: "Art",
            TotalSeats: 500, AvailableSeats: 388, PriceCents: 1500,
            ImageUrl: "https://images.unsplash.com/photo-1579783900882-c0d3dad7b119?w=800&q=80"),
        new(
            "Rock Legends Live",
            "An explosive night of classic rock anthems. Three tribute bands covering Led Zeppelin, Queen, and AC/DC back to back.",
            Day: 18, Start: new TimeOnly(19, 0), EndDay: 18, End: new TimeOnly(23, 0),
            Venue: "Civic Stadium", EventType: "Music",
            TotalSeats: 5000, AvailableSeats: 2140, PriceCents: 6500,
            ImageUrl: "https://images.unsplash.com/photo-1470229722913-7c0e2dbbafd3?w=800&q=80"),
        new(
            "Marathon City Run",
            "Annual 42 km city marathon through iconic landmarks. Open to all levels with 5 km and 10 km fun-run categories.",
            Day: 19, Start: new TimeOnly(7, 0), EndDay: 19, End: new TimeOnly(13, 0),
            Venue: "City Park", EventType: "Sports",
            TotalSeats: 2000, AvailableSeats: 1047, PriceCents: 3000,
            ImageUrl: "https://images.unsplash.com/photo-1552674605-db6ffd4facb5?w=800&q=80"),
        new(
            "Italian Wine Tasting",
            "Journey through Italy's finest wine regions. Six guided tastings paired with artisan charcuterie and cheese.",
            Day: 22, Start: new TimeOnly(18, 0), EndDay: 22, End: new TimeOnly(21, 0),
            Venue: "Vineyard Lounge", EventType: "Food",
            TotalSeats: 80, AvailableSeats: 22, PriceCents: 5500,
            ImageUrl: "https://images.unsplash.com/photo-1414235077428-338989a2e8c0?w=800&q=80"),
        new(
            "Photography Workshop",
            "Hands-on evening workshop covering lighting, composition, and post-processing. Bring your camera or use a loaner.",
            Day: 22, Start: new TimeOnly(17, 0), EndDay: 22, End: new TimeOnly(20, 0),
            Venue: "Art Studio", EventType: "Art",
            TotalSeats: 30, AvailableSeats: 9, PriceCents: 7500,
            ImageUrl: "https://images.unsplash.com/photo-1452780212461-64df12a92440?w=800&q=80"),
        new(
            "Symphony Orchestra",
            "The City Philharmonic performs Beethoven's 5th and Dvořák's New World Symphony in a grand evening concert.",
            Day: 26, Start: new TimeOnly(19, 30), EndDay: 26, End: new TimeOnly(22, 0),
            Venue: "Concert Hall", EventType: "Music",
            TotalSeats: 600, AvailableSeats: 204, PriceCents: 7000,
            ImageUrl: "https://images.unsplash.com/photo-1514320291840-2e0a9bf2a9ae?w=800&q=80"),
        new(
            "City Comedy Night",
            "The biggest comedy event of the month. Seven headliners, two hours of non-stop laughs, free drinks on arrival.",
            Day: 26, Start: new TimeOnly(20, 0), EndDay: 26, End: new TimeOnly(22, 30),
            Venue: "Laugh House", EventType: "Comedy",
            TotalSeats: 200, AvailableSeats: 67, PriceCents: 2200,
            ImageUrl: "https://images.unsplash.com/photo-1527224857830-43a7acc85260?w=800&q=80"),
        new(
            "Ballet Gala",
            "A prestigious evening of classical and contemporary ballet performed by the National Dance Company.",
            Day: 28, Start: new TimeOnly(19, 0), EndDay: 28, End: new TimeOnly(21, 30),
            Venue: "City Theater", EventType: "Theater",
            TotalSeats: 350, AvailableSeats: 173, PriceCents: 9000,
            ImageUrl: "https://images.unsplash.com/photo-1598899134739-24c46f58b8c0?w=800&q=80"),
        new(
            "Street Food Carnival",
            "70+ street food vendors from 30 countries. Flavours, music, and fun for the whole family.",
            Day: 30, Start: new TimeOnly(11, 0), EndDay: 30, End: new TimeOnly(21, 0),
            Venue: "Central Square", EventType: "Food",
            TotalSeats: 2000, AvailableSeats: 1532, PriceCents: 1000,
            ImageUrl: "https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=800&q=80"),
        new(
            "Tennis Open",
            "City-wide tennis open tournament. Singles and doubles brackets for all skill levels. Spectator entry included.",
            Day: 32, Start: new TimeOnly(10, 0), EndDay: 32, End: new TimeOnly(18, 0),
            Venue: "Sports Complex", EventType: "Sports",
            TotalSeats: 1500, AvailableSeats: 789, PriceCents: 4000,
            ImageUrl: "https://images.unsplash.com/photo-1461896836934-ffe607ba8211?w=800&q=80"),
        new(
            "Indie Film Screening",
            "Festival-circuit award-winning indie films screened back to back with Q&A sessions with the directors.",
            Day: 34, Start: new TimeOnly(19, 0), EndDay: 34, End: new TimeOnly(23, 0),
            Venue: "Cinema House", EventType: "Art",
            TotalSeats: 200, AvailableSeats: 118, PriceCents: 2200,
            ImageUrl: "https://images.unsplash.com/photo-1536924430914-91f9e2041b83?w=800&q=80"),
        new(
            "Jazz Brunch",
            "Lazy Sunday brunch with live jazz quartet. Three-course brunch menu with bottomless mimosas included.",
            Day: 36, Start: new TimeOnly(11, 0), EndDay: 36, End: new TimeOnly(14, 0),
            Venue: "Rooftop Lounge", EventType: "Music",
            TotalSeats: 60, AvailableSeats: 14, PriceCents: 4500,
            ImageUrl: "https://images.unsplash.com/photo-1548163111-bc419d75fef4?w=800&q=80"),
        new(
            "Web Dev Bootcamp",
            "Intensive one-day bootcamp. Build and deploy a full-stack web app from scratch. Beginners welcome.",
            Day: 37, Start: new TimeOnly(9, 0), EndDay: 37, End: new TimeOnly(17, 0),
            Venue: "Tech Hub", EventType: "Tech",
            TotalSeats: 40, AvailableSeats: 11, PriceCents: 29900,
            ImageUrl: "https://images.unsplash.com/photo-1451187580459-43490279c0fa?w=800&q=80"),
        new(
            "Comedy Marathon",
            "Four hours of non-stop stand-up comedy. 12 comedians, rotating every 20 minutes. Your cheeks will hurt.",
            Day: 41, Start: new TimeOnly(19, 0), EndDay: 41, End: new TimeOnly(23, 0),
            Venue: "Comedy Palace", EventType: "Comedy",
            TotalSeats: 300, AvailableSeats: 201, PriceCents: 2800,
            ImageUrl: "https://images.unsplash.com/photo-1527224857830-43a7acc85260?w=800&q=80"),
        new(
            "Salsa Dance Night",
            "Latin beats, professional instructors, and a packed dance floor. Beginners' crash course at 7pm before the social.",
            Day: 42, Start: new TimeOnly(20, 0), EndDay: 42, End: new TimeOnly(23, 0),
            Venue: "Latin Club", EventType: "Music",
            TotalSeats: 150, AvailableSeats: 63, PriceCents: 3000,
            ImageUrl: "https://images.unsplash.com/photo-1504609813442-a8924e83f76e?w=800&q=80"),
        new(
            "Blockchain Summit",
            "Deep dives into DeFi, Web3 infrastructure, and enterprise blockchain. Networking dinner included.",
            Day: 43, Start: new TimeOnly(9, 0), EndDay: 43, End: new TimeOnly(18, 0),
            Venue: "Conference Center", EventType: "Business",
            TotalSeats: 200, AvailableSeats: 88, PriceCents: 24900,
            ImageUrl: "https://images.unsplash.com/photo-1542744173-8e7e53415bb0?w=800&q=80"),
        new(
            "Outdoor Cinema Night",
            "Classic films under the stars. Bring a blanket, grab a cocktail from the bar, and enjoy cinema the way it was meant to be.",
            Day: 44, Start: new TimeOnly(20, 0), EndDay: 44, End: new TimeOnly(23, 0),
            Venue: "Rooftop Garden", EventType: "Art",
            TotalSeats: 200, AvailableSeats: 144, PriceCents: 1800,
            ImageUrl: "https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=800&q=80"),
        new(
            "Evening Art Market",
            "Pop-up art market showcasing 40 local artists. Original paintings, prints, ceramics, and live art demonstrations.",
            Day: 44, Start: new TimeOnly(18, 0), EndDay: 44, End: new TimeOnly(22, 0),
            Venue: "Warehouse District", EventType: "Art",
            TotalSeats: 500, AvailableSeats: 322, PriceCents: 500,
            ImageUrl: "https://images.unsplash.com/photo-1579783900882-c0d3dad7b119?w=800&q=80"),
        new(
            "Summer Rock Fest",
            "The biggest outdoor music festival of the summer. Eight bands across two stages, food trucks, and fireworks finale.",
            Day: 48, Start: new TimeOnly(16, 0), EndDay: 48, End: new TimeOnly(23, 0),
            Venue: "Lakeside Amphitheatre", EventType: "Music",
            TotalSeats: 8000, AvailableSeats: 3211, PriceCents: 8500,
            ImageUrl: "https://images.unsplash.com/photo-1493225457124-a3eb161ffa5f?w=800&q=80"),
        new(
            "Food Truck Rally",
            "30 food trucks, craft beers, and live acoustic music. Vote for your favourite truck and win prizes.",
            Day: 50, Start: new TimeOnly(12, 0), EndDay: 50, End: new TimeOnly(20, 0),
            Venue: "Waterfront Plaza", EventType: "Food",
            TotalSeats: 3000, AvailableSeats: 2455, PriceCents: 800,
            ImageUrl: "https://images.unsplash.com/photo-1565123409695-7b5ef63a2efb?w=800&q=80"),
        new(
            "Theater Night: Hamlet",
            "A modern retelling of Shakespeare's Hamlet set in a near-future dystopia. Critically acclaimed production.",
            Day: 55, Start: new TimeOnly(19, 0), EndDay: 55, End: new TimeOnly(21, 30),
            Venue: "Civic Theatre", EventType: "Theater",
            TotalSeats: 300, AvailableSeats: 198, PriceCents: 6500,
            ImageUrl: "https://images.unsplash.com/photo-1507676184212-d03ab07a01bf?w=800&q=80"),
        new(
            "Summer Jazz Concert",
            "Al fresco jazz concert in the park. Bring a picnic, bring friends, and let the music carry the evening.",
            Day: 57, Start: new TimeOnly(18, 0), EndDay: 57, End: new TimeOnly(21, 0),
            Venue: "Park Amphitheatre", EventType: "Music",
            TotalSeats: 1000, AvailableSeats: 567, PriceCents: 3500,
            ImageUrl: "https://images.unsplash.com/photo-1548163111-bc419d75fef4?w=800&q=80"),
        new(
            "Blues & Soul Evening",
            "An electric night of blues and soul with back-to-back sets from four acclaimed artists. Late bar, warm vibes, unforgettable music.",
            Day: 0, Start: new TimeOnly(20, 30), EndDay: 0, End: new TimeOnly(23, 0),
            Venue: "The Rusty String", EventType: "Music",
            TotalSeats: 150, AvailableSeats: 92, PriceCents: 2000,
            ImageUrl: "https://images.unsplash.com/photo-1516280440614-37939bbacd81?w=800&q=80"),
    ];

    public static void Seed(AppDbContext db, DateOnly today)
    {
        if (db.Events.Any()) return;

        var firstDay = today.AddDays(1);
        db.Events.AddRange(Events.Select(e => new Event
        {
            Title = e.Title,
            Description = e.Description,
            StartTime = ToUtc(firstDay.AddDays(e.Day), e.Start),
            EndTime = ToUtc(firstDay.AddDays(e.EndDay), e.End),
            Venue = e.Venue,
            EventType = e.EventType,
            TotalSeats = e.TotalSeats,
            AvailableSeats = e.AvailableSeats,
            PriceCents = e.PriceCents,
            ImageUrl = e.ImageUrl,
        }));
        db.SaveChanges();
    }

    private static DateTime ToUtc(DateOnly day, TimeOnly time) =>
        TimeZoneInfo.ConvertTimeToUtc(day.ToDateTime(time), DemoTimeZone);
}
