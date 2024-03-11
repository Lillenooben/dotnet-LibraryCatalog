using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<DbObject>(new DbObject());

var app = builder.Build();

app.Use(async (context, next) => {
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow}] Started.");
    await next(context);
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow}] Finished.");
});

app.MapGet("/", () => "Hello World!");

//---------CATEGORIES ENDPOINTS---------

app.MapGet("/categories", (DbObject db) => {
    //Returns a list of categories 
    var query = db.SelectCategories();
    var entries = new List<Category>();

    while(query.Read()) {
        var categoryId = query.GetInt32(0);
        var categoryName = query.GetString(1);
        entries.Add(new Category(categoryId, categoryName));
    }

    return entries;
});

app.MapGet("/categories/{id}", Results<Ok<List<Category>>, NotFound, IResult> (DbObject db, int id) => {
    //Returns a category by its id 
    var query = db.SelectCategoryById(id);
    var entries = new List<Category>();

    while(query.Read()) {
        var categoryId = query.GetInt32(0);
        var categoryName = query.GetString(1);
        entries.Add(new Category(categoryId, categoryName));
    }

    return entries.Count > 0 ? TypedResults.Ok(entries) : TypedResults.NotFound();
});

app.MapPost("/categories", (DbObject db, CategoryName category) => {
    //Creates a new category (the category model should be passed in the request body)
    try {
        var query = db.InsertCategory(category.Name);

        return TypedResults.Created("/categories");
    } catch(Exception e) {
        var errors = new Dictionary<string, string[]>{
            { nameof(category)+"."+nameof(category.Name), [e.Message] }
        };

        return Results.ValidationProblem(errors);
    }
})
.AddEndpointFilter(async (context, next) => {
    var categoryNameArgument = context.GetArgument<CategoryName>(1);
    var errors = new Dictionary<string, string[]>();

    if(categoryNameArgument.Name.Length < 1) {
        errors.Add(nameof(categoryNameArgument), ["Category name must not be empty"]);
    }
    if(errors.Count > 0) {
        return Results.ValidationProblem(errors);
    }

    return await next(context);
});

app.MapPut("/categories/{id}", (int id) => {
    //Updates a category 
    throw new NotImplementedException();
});

app.MapDelete("/categories/{id}", (int id) => {
    //Deletes a category by its id 
    throw new NotImplementedException();
});

//---------LIBRARYITEMS ENDPOINTS---------

app.MapGet("/libraryitems", (HttpContext context) => {
    //return all libraryitems with sorting
    var hasSortValue = context.Request.Query.TryGetValue("sort", out var sortValue);

    return hasSortValue ? sortValue[0] : "NoResults";
});

app.MapGet("/libraryitems/{id}", (int id) => {
    //Returns a specific library item by its id 
    throw new NotImplementedException();
});

app.MapPost("/libraryitems", (HttpContext context) => {
    //Creates a new library item. Depending on the type of item being created certain rules may apply.
    throw new NotImplementedException();
});

app.MapPost("/libraryitems/{id}/borrow", (int id) => {
    //Borrow a library item. The Borrower name should be passed in the request body.
    throw new NotImplementedException();
});

app.MapPost("/libraryitems/{id}/return", (int id) => {
    //Returns a borrowed library item. The item should now be able to be borrowed by another user.
    throw new NotImplementedException();
});

app.MapPut("/libraryitems/{id}", (int id) => {
    //Updates an existing library item. Depending on the type of item certain rules may apply. 
    throw new NotImplementedException();
});

app.MapDelete("/libraryitems/{id}", (int id) => {
    //Deletes a library item by its id 
    throw new NotImplementedException();
});

app.Run();

public record CategoryName(string Name);

public record Category(int Id, string Name);

public record LibraryItem(int Id, int CategoryId, string Title, string Type, string Author, int Pages, int RunTimeMinutes, bool IsBorrowable, string Borrower, DateTime Date);
