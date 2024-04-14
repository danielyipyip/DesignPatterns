
# Result Pattern

## Background

Error handling, few ways: 
1. Null checking
2. Exception as flow control
3. Result pattern

### Use case
Domain validation
API error handling

## Advantages/ Disadvantages/ Compare

### Advantages vs null

- No need null checking on all returns (still have to check status code)
- Flexible on returned Error (as it is decided in service)
- Consistent error across API endpoints (as it is decided in service)

### Advantages vs exception

- Better performance (No need cost to create & throw exception)
- Can distinguish real exception and non-happy path (flow control), e.g., in logs
- Do not require making many exception class (still need to make error records)
- Do not need a middleware to handle the exception 
  - Adding complexity to code
  - Performance overhead: adding a step to each request

### Disadvantages vs exception

- Exception: capture stack trace -> good for debugging
- Adding complexity to code base (e.g., generic, implicit casting)
- Not natively supported by .NET

### Compare

## Implementation/ Example code

### 1. Null checking

```
[HttpGet]
public ActionResult<int> Get(){
    var items = service.getAll();

    if(items is null) return Problem("No item");

    return Ok(items);
}
```

### 2. Exception as flow control
```
[HttpPost]
public ActionResult Create(Item item){
    try    {
        service.Create(item);

        return Ok(item);
    }
    catch(Exception e)    {
        return Problem(detail: exception.Message, statusCode: 400);
    }
}
```
The Service: 
```
public Item Create(Item item){
    if(item.name is null) throw new Exception("Name is empty");
}
```

Improvement: middleware to capture the exception & create the response

```
public class ExceptionHandleMiddleware{
    
    private readonly RequestDelegate _next;

    public ExceptionHandleMiddleware(RequestDelegate next)
    {
    _next = next;
    }

    public async Task InvokeAsync(HttpContext context){
        try{
            await _next(context);
        }catch{
            throw new Exception("useful message")
            //could use a switch case on the exception type caught, 
            // or even better, exception factory
            // it complies with the open close principle (switch case is open for modify due to exception changes)
        }
    }
}
```
Program.cs
```
app.UseMiddleware<ExceptionHandleMiddleware>();
```

### 3. Result pattern
Result Object with both value (for happy path) & error (for error path): implicit conversion in Result class
```
public class Result<TValue, TError>{
    public readonly TValue? Value;
    public readonly TError? Error;
    public bool _isSuccess {get;}

    //constructors for different scenerio
    private Result(TValue value){
        _isSuccess = true;
        Value = value;
        Error = default; 
    }

    private Result(TError error){
        _isSuccess = false;
        Value = default;
        Error = error; 
    }

    //implicit conversion for both case
    public static implicit operator Result<TValue, TError>(TValue value)
        => new Result<TValue, TError>(value);

    public static implicit operator Result<TValue, TError>(TError error)
        => new Result<TValue, TError>(error);
}
```

Result Object: (static constructor + implicit conversion in Error class)

Error: 
```
//using record instead of class since it is just a data (statuc code & error message) only type
public sealed record Error(string statusCode, string description = default)
```

We can have multiple Errors and have them documented in code
```
public static class Errors{
    public static readonly Error FirstError = new(400, "first");
    public static readonly Error SecondError = new(500, "second");
}
```

The Service: 
```
public Result<Item, Error> Create(Item item){
    //because of the implicit conversion in the Result class (the return type), the return is casted into a Result object. 
    if(item.name is null){
        return new Error();
    }

    return item;
}
```

The API: 
```
[HttpPost]
public ActionResult Create(Item item){
    var result = service.Create(item);
    if(result._isSuccess)
        return Ok(result.Value);

    return Problem(result.Error);
}
```

Or it can get further refactoring: that kind of work like a exception middleware
```
public class ResultControllerBase: ControllerBase{
    protected ActionResult Ok<T,E>(Result<T,E> result){
        if(result._isSuccessful){
            return base.Ok(result.Value);
        }

        if(result.Error is FirstError){
            return Problem(statusCode: result.Error.statusCode ,detail: result.Error.description);
        }
        //... check other types

        //for "read" unexpected exception
        throw new Exception();
    }
}
```

## Existing/ similar Implementation

### IResult<T>

Kind of a mix of exception & result pattern. 

Got a ISuccessResult<T> for a success case. And IFailureResult with an exception. (ignore INoneResult  for now)

It got the error & value in the IResult object. But the error handling is still defaults to using exception. 
