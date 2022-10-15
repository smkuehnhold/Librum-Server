using System.Net;
using Application.Common.DTOs;
using Application.Common.DTOs.Authors;
using Application.Common.Exceptions;
using Application.Interfaces.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Application.Common.ActionFilters;

public class ValidateAuthorExistsAttribute : IAsyncActionFilter
{
    private readonly IUserRepository _userRepository;
    private readonly IBookRepository _bookRepository;
    private readonly ILogger<ValidateAuthorExistsAttribute> _logger;


    public ValidateAuthorExistsAttribute(IUserRepository userRepository, IBookRepository bookRepository
        ,ILogger<ValidateAuthorExistsAttribute> logger)
    {
        _userRepository = userRepository;
        _bookRepository = bookRepository;
        _logger = logger;
    }
    
    
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        
        var authorForRemovalDto = (AuthorForRemovalDto)context.ActionArguments
                                                              .SingleOrDefault(arg => arg.Key.Contains("Dto"))
                                                              .Value;
        
        if (authorForRemovalDto == null)
        {
            throw new InternalServerException("Action filter: Expected parameter containing 'Dto' does not exist");
        }

        if(!context.ActionArguments.TryGetValue("bookGuid", out object bookGuidObject))
        {
            throw new InternalServerException("Action filter: Expected parameter 'bookGuid' does not exist");
        }
        
        
        var user = await _userRepository.GetAsync(context.HttpContext.User.Identity!.Name, trackChanges: true);
        
        var bookGuid = bookGuidObject.ToString();
        var book = user.Books.SingleOrDefault(book => book.BookId.ToString() == bookGuid);
        await _bookRepository.LoadRelationShipsAsync(book);

        if (!book!.Authors.Any(author =>
                author.FirstName == authorForRemovalDto.FirstName && author.LastName == authorForRemovalDto.LastName))
        {
            _logger.LogWarning("No author with this name exists");
            
            context.HttpContext.Response.ContentType = "application/json";
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            
            var response = new ApiExceptionDto(context.HttpContext.Response.StatusCode, 
                "No author with this name exists");

            await context.HttpContext.Response.WriteAsync(response.ToString());
            return;
        }
        
        
        await next();
    }
}