using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

public static class LibraryItemRouteBuilder {
    public static RouteGroupBuilder MapLibraryItemAPI(this RouteGroupBuilder group) {
        List<string> sortTypes = ["type", "category"];
        
        group.MapGet("/", Results<Ok<List<LibraryItem>>, BadRequest<Dictionary<string, string[]>>>(HttpContext context, DbObject db) => {
            //return all libraryitems with sorting
            var hasSortTypeValue = context.Request.Query.TryGetValue("sort", out var sortType);//tar fram sortType ifall det finns 

            try {
                var listOfLibraryItems = db.SelectLibraryItems(hasSortTypeValue, sortType);
                
                return TypedResults.Ok(listOfLibraryItems);
            } catch (Exception e) {
                Console.WriteLine(e);
                Console.WriteLine(e.Message);
                var errors = new Dictionary<string, string[]>{
                    { "Error: ", [e.Message] }
                };

                return TypedResults.BadRequest(errors);
            }
        })
        .AddEndpointFilter(async (context, next) => {
            var httpContext = context.HttpContext;

            var hasSortTypeValue = httpContext.Request.Query.TryGetValue("sort", out var sortValue);

            var errors = new Dictionary<string, string[]>();

            if(hasSortTypeValue) {//ifall sortValue
                if(!sortTypes.Exists(x => x.Equals(sortValue.ToString().ToLower(), StringComparison.CurrentCultureIgnoreCase))) {
                    errors.Add(nameof(sortTypes), ["Sort query doenst match available sorting types ['type', 'category']"]);
                }
            }
            if(errors.Count > 0) {
                return Results.ValidationProblem(errors);
            }

            return await next(context);
        });

        group.MapGet("/{id}", Results<Ok<LibraryItem>, BadRequest<Dictionary<string, string[]>>> (DbObject db, int id) => {
            //Returns a specific library item by its id 
            try {
                LibraryItem singleLibraryitem = db.SelectLibraryItemById(id);
                return TypedResults.Ok(singleLibraryitem);

            } catch (Exception e) {
                Console.WriteLine(e);
                var errors = new Dictionary<string, string[]>{
                    { "LibraryItem id: " + id, [e.Message] }
                };
                return TypedResults.BadRequest(errors);
            }
        });

        group.MapPost("/", Results<Created, BadRequest<Dictionary<string, string[]>>> (DbObject db, LibraryItemInput libraryItemInput) => {
            //Creates a new library item. Depending on the type of item being created certain rules may apply.
            //long Id, int CategoryId, string? Title, string Type, string? Author, int? Pages, int? RunTimeMinutes, bool IsBorrowable, string? Borrower, DateTime? BorrowDate
            try {
                db.InsertLibraryItem(libraryItemInput);
                return TypedResults.Created("/libraryitems");
            } catch(Exception e) {
                Console.WriteLine(e);
                Console.WriteLine(e.Message);
                var errors = new Dictionary<string, string[]>{
                    { nameof(libraryItemInput) , [e.Message] }
                };
                
                return TypedResults.BadRequest(errors);
            }
        })
        .AddEndpointFilter(async (context, next) => {
            var libraryItem = context.GetArgument<LibraryItemInput>(1);
            var errors = new Dictionary<string, string[]>();

            var validLibraryItem = Enum.TryParse(libraryItem.Type.ToUpper(), out LibraryItemType libraryItemType);

            if(!validLibraryItem) {
                errors.Add(nameof(libraryItem.Type), ["Field: Type is invalid " + libraryItem.Type]);
                return Results.ValidationProblem(errors);
            }

            if(libraryItemType == LibraryItemType.BOOK || libraryItemType == LibraryItemType.REFERENCEBOOK) {
                if(libraryItem.Title == null || libraryItem.Title.Trim().Length <= 0) {
                    errors.Add(nameof(libraryItem.Title), ["Field: Title must not be empty for type: " + libraryItem.Type]);
                }
                if(libraryItem.Author == null || libraryItem.Author.Trim().Length <= 0) {
                    errors.Add(nameof(libraryItem.Author), ["Field: Author must not be empty for type: " + libraryItem.Type]);
                }
                if(libraryItem.Pages == null || libraryItem.Pages <= 0) {
                    errors.Add(nameof(libraryItem.Pages), ["Field: Pages is empty or invalid for type: " + libraryItem.Type]);
                }
            } else { //if Type is DVD or AudioBook
                if(libraryItem.Title == null || libraryItem.Title.Trim().Length <= 0) {
                    errors.Add(nameof(libraryItem.Title), ["Field: Title must not be empty for type: " + libraryItem.Type]);
                }
                if(libraryItem.RunTimeMinutes == null || libraryItem.RunTimeMinutes <= 0) {
                    errors.Add(nameof(libraryItem.RunTimeMinutes), ["Field: RunTimeMinutes is empty or invalid for type: " + libraryItem.Type]);
                }

            }
            if(errors.Count > 0) {
                return Results.ValidationProblem(errors);
            }

            return await next(context);
        });

        group.MapPost("/{id}/borrow", Results<Ok, BadRequest<Dictionary<string, string[]>>> (DbObject db, int id, [FromBody] NameInput name) => {
            //Borrow a library item. The Borrower name should be passed in the request body.
            try {
                db.BorrowLibraryItem(id, name.Name.Trim());
                return TypedResults.Ok();
            } catch (Exception e) {
                Console.WriteLine(e);
                Console.WriteLine(e.Message);
                var errors = new Dictionary<string, string[]>{
                    { "LibraryItem id: " + id, [e.Message] }
                };
                
                return TypedResults.BadRequest(errors);
            }
        })
        .AddEndpointFilter(async (context, next) => {
            var libraryItemId = context.GetArgument<int>(1);
            var errors = new Dictionary<string, string[]>();

            var borrowerName = context.GetArgument<NameInput>(2).Name;

            if(borrowerName.Trim().Length <= 0)
                errors.Add(nameof(borrowerName), ["Field: Name is empty"]);
            if(libraryItemId <= 0)
                errors.Add(nameof(libraryItemId), ["Field: Id is out of scope"]);
            
            if(errors.Count > 0) {
                return Results.ValidationProblem(errors);
            }

            return await next(context);
        });

        group.MapPost("/{id}/return", Results<Ok, BadRequest<Dictionary<string, string[]>>> (DbObject db, int id) => {
            //Returns a borrowed library item. The item should now be able to be borrowed by another user.
            try {
                db.ReturnLibraryItem(id);
                return TypedResults.Ok();
            } catch (Exception e) {
                Console.WriteLine(e);
                Console.WriteLine(e.Message);
                var errors = new Dictionary<string, string[]>{
                    { "LibraryItem id: " + id, [e.Message] }
                };
                
                return TypedResults.BadRequest(errors);
            }
        })
        .AddEndpointFilter(async (context, next) => {
            var libraryItemId = context.GetArgument<int>(1);
            var errors = new Dictionary<string, string[]>();

            if(libraryItemId <= 0)
                    errors.Add(nameof(libraryItemId), ["Field: Id is out of scope"]);
            
            if(errors.Count > 0) {
                return Results.ValidationProblem(errors);
            }

            return await next(context);
        });

        group.MapPut("/{id}", Results<Ok, BadRequest<Dictionary<string, string[]>>> (DbObject db, int id, [FromBody]LibraryItemInput libraryItemInput) => {
            //Updates an existing library item. Depending on the type of item certain rules may apply. 
            try {
                Console.WriteLine("\n\n" + libraryItemInput);
                db.UpdateLibraryItem(id, libraryItemInput);
                return TypedResults.Ok();
            } catch (Exception e) {
                Console.WriteLine(e);
                Console.WriteLine(e.Message);
                var errors = new Dictionary<string, string[]>{
                    { "LibraryItem id: " + id, [e.Message] }
                };
                
                return TypedResults.BadRequest(errors);
            }
        })
        .AddEndpointFilter(async (context, next) => {
            var libraryItemInput = context.GetArgument<LibraryItemInput>(2);
            var errors = new Dictionary<string, string[]>();

            var validLibraryItem = Enum.TryParse(libraryItemInput.Type.ToUpper(), out LibraryItemType libraryItemType);

            if(!validLibraryItem) {
                errors.Add(nameof(libraryItemInput.Type), ["Field: Type is invalid " + libraryItemInput.Type]);
                return Results.ValidationProblem(errors);
            }

            if(libraryItemType == LibraryItemType.BOOK || libraryItemType == LibraryItemType.REFERENCEBOOK) {
                if(libraryItemInput.Title == null || libraryItemInput.Title.Trim().Length <= 0) {
                    errors.Add(nameof(libraryItemInput.Title), ["Field: Title must not be empty for type: " + libraryItemInput.Type]);
                }
                if(libraryItemInput.Author == null || libraryItemInput.Author.Trim().Length <= 0) {
                    errors.Add(nameof(libraryItemInput.Author), ["Field: Author must not be empty for type: " + libraryItemInput.Type]);
                }
                if(libraryItemInput.Pages == null || libraryItemInput.Pages <= 0) {
                    errors.Add(nameof(libraryItemInput.Pages), ["Field: Pages is empty or invalid for type: " + libraryItemInput.Type]);
                }
            } else { //om det är av typ DVD eller AUDIOBOOK
                if(libraryItemInput.Title == null || libraryItemInput.Title.Trim().Length <= 0) {
                    errors.Add(nameof(libraryItemInput.Title), ["Field: Title must not be empty for type: " + libraryItemInput.Type]);
                }
                if(libraryItemInput.RunTimeMinutes == null || libraryItemInput.RunTimeMinutes <= 0) {
                    errors.Add(nameof(libraryItemInput.RunTimeMinutes), ["Field: RunTimeMinutes is empty or invalid for type: " + libraryItemInput.Type]);
                }

            }
            if(errors.Count > 0) {
                return Results.ValidationProblem(errors);
            }

            return await next(context);
        });

        group.MapDelete("/{id}", Results<NoContent, BadRequest<Dictionary<string, string[]>>> (DbObject db, int id) => {
            //Deletes a library item by its id 
            try {
                db.DeleteLibraryItemById(id);
                return TypedResults.NoContent();
            } catch (Exception e) {
                var errors = new Dictionary<string, string[]>{
                    { "LibraryItem id: " + id, [e.Message] }
                };
                return TypedResults.BadRequest(errors);
            }
        });

        return group;
    }
}