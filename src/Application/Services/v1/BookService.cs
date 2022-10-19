using Application.Common.DTOs.Authors;
using Application.Common.DTOs.Books;
using Application.Common.Exceptions;
using Application.Common.RequestParameters;
using Application.Extensions;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using AutoMapper;
using Domain.Entities;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services.v1;

public class BookService : IBookService
{
    private readonly IMapper _mapper;
    private readonly IBookRepository _bookRepository;
    private readonly IUserRepository _userRepository;

    public BookService(IMapper mapper, IBookRepository bookRepository, 
        IUserRepository userRepository)
    {
        _mapper = mapper;
        _bookRepository = bookRepository;
        _userRepository = userRepository;
    }


    public async Task CreateBookAsync(string email, BookInDto bookInDto, string guid)
    {
        var user = await _userRepository.GetAsync(email, trackChanges: true);

        var book = _mapper.Map<Book>(bookInDto);
        book.BookId = new System.Guid(guid);
        user.Books.Add(book);

        await _bookRepository.SaveChangesAsync();
    }

    public async Task<IList<BookOutDto>> GetBooksAsync(string email, BookRequestParameter bookRequestParameter)
    {
        var user = await _userRepository.GetAsync(email, trackChanges: false);

        var books = _bookRepository.GetAllAsync(user.Id);
        await _bookRepository.LoadRelationShipsAsync(books);
        

        var processedBooks = books
            .FilterByTags(bookRequestParameter.Tag?.Name)
            .FilterByAuthor(bookRequestParameter.Author.ToLower())
            .FilterByTimeSinceAdded(bookRequestParameter.TimePassed)
            .FilterByFormat(bookRequestParameter.Format)
            .FilterByOptions(bookRequestParameter)
            .SortByBestMatch(bookRequestParameter.SearchString.ToLower())
            .SortByCategories(bookRequestParameter.SortBy, bookRequestParameter.SearchString)
            .PaginateBooks(bookRequestParameter.PageNumber, bookRequestParameter.PageSize);
        
        return await processedBooks.Select(book => _mapper.Map<BookOutDto>(book)).ToListAsync();
    }

    public async Task AddTagsToBookAsync(string email, string bookGuid, IEnumerable<string> tagNames)
    {
        var user = await _userRepository.GetAsync(email, trackChanges: true);
        
        var book = user.Books.Single(book => book.BookId.ToString() == bookGuid);
        await _bookRepository.LoadRelationShipsAsync(book);
        
        foreach (var tagName in tagNames)
        {
            var tag = GetTagIfExist(user, tagName);
            CheckIfBookAlreadyHasTag(book, tagName);
            book.Tags.Add(tag);
        }

        await _bookRepository.SaveChangesAsync();
    }

    private static void CheckIfBookAlreadyHasTag(Book book, string tagName)
    {
        if (book.Tags.Any(tag => tag.Name == tagName))
        {
            throw new InvalidParameterException("The book already has the given tag");
        }
    }

    private static Tag GetTagIfExist(User user, string tagName)
    {
        var tag = user.Tags.SingleOrDefault(tag => tag.Name == tagName);
        if (tag == null)
        {
            throw new InvalidParameterException("No tag called " + tagName + " exists");
        }

        return tag;
    }
    
    public async Task RemoveTagFromBookAsync(string email, string bookGuid, string tagName)
    {
        var user = await _userRepository.GetAsync(email, trackChanges: true);
        
        var book = user.Books.Single(book => book.BookId.ToString() == bookGuid);
        await _bookRepository.LoadRelationShipsAsync(book);

        var tag = book.Tags.SingleOrDefault(tag => tag.Name == tagName);

        book.Tags.Remove(tag);
        await _bookRepository.SaveChangesAsync();
    }

    public async Task DeleteBooksAsync(string email, IEnumerable<string> bookGuids)
    {
        var user = await _userRepository.GetAsync(email, trackChanges: true);

        foreach (var bookGuid in bookGuids)
        {
            var book = user.Books.SingleOrDefault(book => book.BookId.ToString() == bookGuid);
            if (book == null)
            {
                throw new InvalidParameterException("No book with this title exists");
            }
            await _bookRepository.LoadRelationShipsAsync(book);
            
            _bookRepository.DeleteBook(book);
        }

        await _bookRepository.SaveChangesAsync();
    }
    
    public async Task PatchBookAsync(string email, JsonPatchDocument<BookForUpdateDto> patchDoc, string bookGuid,
        ControllerBase controllerBase)
    {
        var user = await _userRepository.GetAsync(email, trackChanges: true);
        var book = user.Books.Single(book => book.BookId.ToString() == bookGuid);

        var bookToPatch = _mapper.Map<BookForUpdateDto>(book);

        patchDoc.ApplyTo(bookToPatch, controllerBase.ModelState);
        controllerBase.TryValidateModel(controllerBase.ModelState);

        if (!controllerBase.ModelState.IsValid || !bookToPatch.DataIsValid)
        {
            throw new InvalidParameterException("The provided data is invalid");
        }
        
        _mapper.Map(bookToPatch, book);

        await _bookRepository.SaveChangesAsync();
    }

    public async Task AddAuthorToBookAsync(string email, string bookGuid, AuthorInDto authorToAdd)
    {
        var user = await _userRepository.GetAsync(email, trackChanges: true);
        
        var book = user.Books.Single(book => book.BookId.ToString() == bookGuid);
        await _bookRepository.LoadRelationShipsAsync(book);

        var author = _mapper.Map<Author>(authorToAdd);
        book.Authors.Add(author);
        
        await _bookRepository.SaveChangesAsync();
    }

    public async Task RemoveAuthorFromBookAsync(string email, string bookGuid, AuthorForRemovalDto authorToRemove)
    {
        var user = await _userRepository.GetAsync(email, trackChanges: true);
        
        var book = user.Books.SingleOrDefault(book => book.BookId.ToString() == bookGuid);
        await _bookRepository.LoadRelationShipsAsync(book);

        var author = book!.Authors.SingleOrDefault(author => 
            author.FirstName == authorToRemove.FirstName && author.LastName == authorToRemove.LastName);

        book.Authors.Remove(author);
        await _bookRepository.SaveChangesAsync();
    }
}