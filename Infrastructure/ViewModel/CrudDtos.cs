using System;
using System.Collections.Generic;

namespace Infrastructure.ViewModel
{
    /// <summary>
    /// One-shot CRUD feedback carried in TempData between a POST and the next GET.
    /// Nothing is persisted; durable Hangfire alerts use notifications instead.
    /// </summary>
    public sealed class FlashMessageDto
    {
        public const string SuccessType = "success";
        public const string ErrorType = "error";

        public string Type { get; set; } = ErrorType;
        public string Title { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;

        public static FlashMessageDto Success(string title, string text) =>
            new() { Type = SuccessType, Title = title, Text = text };

        public static FlashMessageDto Error(string title, string text) =>
            new() { Type = ErrorType, Title = title, Text = text };
    }

    public class AuthorDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? LastUpdatedOn { get; set; }
    }

    public class AuthorCommand
    {
        public string Name { get; set; } = string.Empty;
    }

    public class CategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Currentstste { get; set; }
    }

    public class CategoryCommand
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    /// <summary>Category form input; the Id is the edit target (empty for create).</summary>
    public class NewCategory : CategoryCommand
    {
    }

    public class SubCategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid CategoryId { get; set; }
        public int Currentstate { get; set; }
    }

    public class SubCategoryCommand
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid CategoryId { get; set; }
    }

    public class BookDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid AuthorId { get; set; }
        public AuthorDto? Author { get; set; }
        public string? Publisher { get; set; }
        public DateTime? PublishingDate { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImageThumbnailUrl { get; set; }
        public string? ImagePublicId { get; set; }
        public string? Hall { get; set; }
        public bool IsAvailableForRental { get; set; }
        public string? Description { get; set; }
        public Guid? CategoryId { get; set; }
        public CategoryDto? Category { get; set; }
        public Guid? SubCategoryId { get; set; }
        public SubCategoryDto? SubCategory { get; set; }
        public int CurrentState { get; set; }
    }

    public class BookCommand
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid AuthorId { get; set; }
        public string? Publisher { get; set; }
        public DateTime? PublishingDate { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImageThumbnailUrl { get; set; }
        public string? ImagePublicId { get; set; }
        public string? Hall { get; set; }
        public bool IsAvailableForRental { get; set; } = true;
        public string? Description { get; set; }
        public Guid? CategoryId { get; set; }
        public Guid? SubCategoryId { get; set; }
    }

    public class BookCopyDto
    {
        public Guid Id { get; set; }
        public Guid BookId { get; set; }
        public BookDto? Book { get; set; }
        public bool IsAvailableForRental { get; set; }
        public int EditionNumber { get; set; }
        public int SerialNumber { get; set; }
    }

    public class BookCopyCommand
    {
        public Guid Id { get; set; }
        public Guid BookId { get; set; }
        public bool IsAvailableForRental { get; set; } = true;
        public int EditionNumber { get; set; }
        public int SerialNumber { get; set; }
    }

    public class GovernorateDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<AreaDto> Areas { get; set; } = new();
    }

    public class GovernorateCommand
    {
        public string Name { get; set; } = string.Empty;
    }

    public class AreaDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid GovernorateId { get; set; }
        public GovernorateDto? Governorate { get; set; }
    }

    public class AreaCommand
    {
        public string Name { get; set; } = string.Empty;
        public Guid GovernorateId { get; set; }
    }

    public class SubscriberDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}";
        public DateTime DateOfBirth { get; set; }
        public string NationalId { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public bool HasWhatsApp { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? ImageThumbnailUrl { get; set; }
        public Guid AreaId { get; set; }
        public AreaDto? Area { get; set; }
        public Guid GovernorateId { get; set; }
        public GovernorateDto? Governorate { get; set; }
        public string? Address { get; set; }
        public bool IsBlackListed { get; set; }
        public DateTime CreatedOn { get; set; }
        public List<SubscriptionDto> Subscriptions { get; set; } = new();
    }

    public class SubscriberCommand
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string NationalId { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public bool HasWhatsApp { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? ImageThumbnailUrl { get; set; }
        public Guid AreaId { get; set; }
        public Guid GovernorateId { get; set; }
        public string? Address { get; set; }
        public bool IsBlackListed { get; set; }
    }

    public class SubscriptionDto
    {
        public Guid Id { get; set; }
        public Guid SubscriberId { get; set; }
        public SubscriberDto? Subscriber { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? CreatedById { get; set; }
        public DateTime CreatedOn { get; set; }
        public bool IsActive => DateTime.UtcNow >= StartDate && DateTime.UtcNow <= EndDate;
    }

    public class SubscriptionCommand
    {
        public Guid SubscriberId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? CreatedById { get; set; }
    }

    public class RentalCopyDto
    {
        public Guid RentalId { get; set; }
        public Guid BookCopyId { get; set; }
        public BookCopyDto? BookCopy { get; set; }
        public DateTime RentalDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public DateTime? ExtendedOn { get; set; }
    }

    public class RentalDto
    {
        public Guid Id { get; set; }
        public Guid SubscriberId { get; set; }
        public SubscriberDto? Subscriber { get; set; }
        public DateTime StartDate { get; set; }
        public bool PenaltyPaid { get; set; }
        public decimal? TotalFine { get; set; }
        public List<RentalCopyDto> RentalCopies { get; set; } = new();
    }

    public class RentalCopyCommand
    {
        public Guid BookCopyId { get; set; }
        public DateTime RentalDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public DateTime? ExtendedOn { get; set; }
    }

    public class RentalCommand
    {
        public Guid SubscriberId { get; set; }
        public DateTime StartDate { get; set; }
        public bool PenaltyPaid { get; set; }
        public decimal? TotalFine { get; set; }
        public List<RentalCopyCommand> RentalCopies { get; set; } = new();
    }

    public class LogCategoryDto
    {
        public Guid Id { get; set; }
        public string? Action { get; set; }
        public DateTime Date { get; set; }
        public Guid UserId { get; set; }
        public Guid CategoryId { get; set; }
        public CategoryDto? category { get; set; }
    }

    /// <summary>
    /// DTO-owned equivalent of Domin.Entity.NotificationType so jobs and views
    /// never depend on a persistence entity.
    /// </summary>
    public enum NotificationKindDto
    {
        SubscriptionExpiring = 1,
        RentalOverdue = 2,
        FineApplied = 3
    }

    public class NotificationDto
    {
        public Guid Id { get; set; }
        public NotificationKindDto Type { get; set; }
        public string DedupeKey { get; set; } = string.Empty;
        public string SubscriberName { get; set; } = string.Empty;
        public string? BookTitle { get; set; }
        public int? Days { get; set; }
        public decimal? Amount { get; set; }
        public string? TargetUrl { get; set; }
        public DateTime CreatedOn { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadOn { get; set; }
    }

    public class NotificationCommand
    {
        public NotificationKindDto Type { get; set; }
        public string DedupeKey { get; set; } = string.Empty;
        public string SubscriberName { get; set; } = string.Empty;
        public string? BookTitle { get; set; }
        public int? Days { get; set; }
        public decimal? Amount { get; set; }
        public string? TargetUrl { get; set; }
    }
}
