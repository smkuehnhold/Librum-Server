using System.ComponentModel.DataAnnotations;

namespace Application.Common.DTOs.Blogs;

public class BlogInDto
{
    [Required]
    [MinLength(4)]
    [MaxLength(500)]
    public string Title { get; set; }
    
    [Required]
    [MinLength(10)]
    [MaxLength(1000)]
    public string Introduction { get; set; }
}