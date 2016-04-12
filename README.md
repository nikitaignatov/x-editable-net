# x-editable-net

This library supports [x-editable](https://github.com/vitalets/x-editable) updates with EntityFramework.

````csharp
public class DefaultRequest
{
    public int pk { get; set; }
    public string name { get; set; }
    public string value { get; set; }
    public string entity { get; set; }
}
````

Api endpoint for handling the updates, int the the primary key type. 

````csharp
[HttpPatch]
public dynamic InlineEdit([FromBody]DefaultRequest request)
{
    try
    {
        var command = new UpdateEntityCommand<int>(request.pk, request.value, request.name, exportedTypes[request.entity]);
        handler.Handle(command);
        return Ok();
    }
    catch (Exception ex)
    {
        return BadRequest($"Failed due to  {JsonConvert.SerializeObject(ex, Formatting.Indented)}");
    }
}
````
