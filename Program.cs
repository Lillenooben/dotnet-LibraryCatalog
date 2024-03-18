
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<DbObject>(new DbObject());//Lägger till en referens till en DbObject som ska användas som en Singleton genom hela programmet

var app = builder.Build();

app.Use(async (context, next) => {
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow}] Started.");
    await next(context);
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow}] Finished.");
});

app.MapGet("/", () => "Hello World!");

//---------CATEGORIES ENDPOINTS---------
//skapar en grupp till alla endpoints som börjar med /categories
app.MapGroup("/categories")
    .MapCategoriesAPI().WithTags("Categories API");

//---------LIBRARYITEMS ENDPOINTS---------
//skapar en grupp till alla endpoints som börjar med /libraryitems
app.MapGroup("/libraryitems")
    .MapLibraryItemAPI().WithTags("LibraryItems API");

app.Run();

//används för att identifiera/verifiera en libraryitem.type
public enum LibraryItemType {
    BOOK,
    REFERENCEBOOK,
    DVD,
    AUDIOBOOK
}

//används när i olika input och display scenarion
public record LibraryItemInput(int CategoryId, string? Title, string Type, string? Author, int? Pages, int? RunTimeMinutes);

public record NameInput(string Name);

public record Category(long Id, string Name);

public record LibraryItem(long Id, long CategoryId, string? Title, string Type, string? Author, long? Pages, long? RunTimeMinutes, bool IsBorrowable, string? Borrower, string? BorrowDate);
