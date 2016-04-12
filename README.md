# x-editable-net

This library supports [x-editable](https://github.com/vitalets/x-editable) updates with EntityFramework.

  public class DefaultRequest
  {
      public string pk { get; set; }
      public string name { get; set; }
      public string value { get; set; }
      public string entity { get; set; }
  }

Api endpoint for handling the updates, int the the primary key type. 

  [HttpPatch]
  public dynamic InlineEdit([FromBody]DefaultRequest request)
  {
      try
      {
          var command = new UpdateEntityCommand<int>(Convert.ToInt32(request.pk), request.value, request.name, exportedTypes[request.entity]);

          handler.BeforeSave += Handler_BeforeSave;
          handler.Handle(command);
          return Ok();
      }
      catch (DbEntityValidationException ex)
      {
          var error = ex.EntityValidationErrors.First().ValidationErrors.First();
          ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
          return BadRequest($"{error.PropertyName} { error.ErrorMessage}");
      }
      catch (Exception ex)
      {
          return BadRequest($"Failed due to  {JsonConvert.SerializeObject(ex, Formatting.Indented)}");
      }
  }
