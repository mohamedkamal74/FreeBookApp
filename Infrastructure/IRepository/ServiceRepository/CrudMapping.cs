using System;
using System.Collections.Generic;
using System.Linq;
using Domin.Entity;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.IRepository.ServiceRepository
{
    /// <summary>
    /// Explicit entity to DTO mapping kept inside Infrastructure. Persistence
    /// entities never cross a repository contract; navigation properties are
    /// mapped from the related entities loaded by each query.
    /// </summary>
    internal static class CrudMapping
    {
        internal static RoleDto ToDto(IdentityRole entity) => new()
        {
            Id = entity.Id ?? string.Empty,
            Name = entity.Name ?? string.Empty
        };

        internal static UserDto ToDto(VwUser entity) => new()
        {
            Id = entity.Id ?? string.Empty,
            Name = entity.Name ?? string.Empty,
            Email = entity.Email ?? string.Empty,
            ImageUser = entity.ImageUser,
            ActiveUser = entity.ActiveUser,
            Role = entity.Role
        };

        internal static AuthorDto ToDto(Author entity) => new()
        {
            Id = entity.Id,
            Name = entity.Name,
            IsDeleted = entity.IsDeleted,
            CreatedOn = entity.CreatedOn,
            LastUpdatedOn = entity.LastUpdatedOn
        };

        internal static CategoryDto ToDto(Category entity) => new()
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Currentstste = entity.Currentstste
        };

        internal static SubCategoryDto ToDto(SubCategory entity) => new()
        {
            Id = entity.Id,
            Name = entity.Name,
            CategoryId = entity.CategoryId,
            Currentstate = entity.Currentstate
        };

        internal static BookDto ToDto(Book entity) => new()
        {
            Id = entity.Id,
            Name = entity.Name,
            AuthorId = entity.AuthorId,
            Author = entity.Author is null ? null : ToDto(entity.Author),
            Publisher = entity.Publisher,
            PublishingDate = entity.PublishingDate,
            ImageUrl = entity.ImageUrl,
            ImageThumbnailUrl = entity.ImageThumbnailUrl,
            ImagePublicId = entity.ImagePublicId,
            Hall = entity.Hall,
            IsAvailableForRental = entity.IsAvailableForRental,
            Description = entity.Description,
            CategoryId = entity.CategoryId,
            Category = entity.Category is null ? null : ToDto(entity.Category),
            SubCategoryId = entity.SubCategoryId,
            SubCategory = entity.SubCategory is null ? null : ToDto(entity.SubCategory),
            CurrentState = entity.CurrentState
        };

        internal static BookCopyDto ToDto(BookCopy entity) => new()
        {
            Id = entity.Id,
            BookId = entity.BookId,
            Book = entity.Book is null ? null : ToDto(entity.Book),
            IsAvailableForRental = entity.IsAvailableForRental,
            EditionNumber = entity.EditionNumber,
            SerialNumber = entity.SerialNumber
        };

        internal static GovernorateDto ToDto(Governorate entity) => new()
        {
            Id = entity.Id,
            Name = entity.Name,
            Areas = entity.Areas
                .Select(area => new AreaDto
                {
                    Id = area.Id,
                    Name = area.Name,
                    GovernorateId = area.GovernorateId,
                    Governorate = null
                })
                .ToList()
        };

        internal static AreaDto ToDto(Area entity) => new()
        {
            Id = entity.Id,
            Name = entity.Name,
            GovernorateId = entity.GovernorateId,
            Governorate = entity.Governorate is null ? null : ToDto(entity.Governorate)
        };

        internal static SubscriberDto ToDto(Subscriber entity) => new()
        {
            Id = entity.Id,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            DateOfBirth = entity.DateOfBirth,
            NationalId = entity.NationalId,
            MobileNumber = entity.MobileNumber,
            HasWhatsApp = entity.HasWhatsApp,
            Email = entity.Email,
            ImageUrl = entity.ImageUrl,
            ImageThumbnailUrl = entity.ImageThumbnailUrl,
            AreaId = entity.AreaId,
            Area = entity.Area is null ? null : ToDto(entity.Area),
            GovernorateId = entity.GovernorateId,
            Governorate = entity.Governorate is null ? null : ToDto(entity.Governorate),
            Address = entity.Address,
            IsBlackListed = entity.IsBlackListed,
            CreatedOn = entity.CreatedOn,
            ApplicationUserId = entity.ApplicationUserId,
            Subscriptions = entity.Subscriptions
                .Select(s => ToDto(s, includeSubscriber: false))
                .ToList()
        };

        internal static SubscriptionDto ToDto(Subscription entity, bool includeSubscriber = true) => new()
        {
            Id = entity.Id,
            SubscriberId = entity.SubscriberId,
            Subscriber = includeSubscriber ? ToSubscriberSummary(entity.Subscriber) : null,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            CreatedById = entity.CreatedById,
            CreatedOn = entity.CreatedOn
        };

        internal static RentalDto ToDto(Rental entity) => new()
        {
            Id = entity.Id,
            SubscriberId = entity.SubscriberId,
            Subscriber = ToSubscriberSummary(entity.Subscriber),
            StartDate = entity.StartDate,
            PenaltyPaid = entity.PenaltyPaid,
            TotalFine = entity.TotalFine,
            RentalCopies = entity.RentalCopies.Select(ToDto).ToList()
        };

        internal static RentalCopyDto ToDto(RentalCopy entity) => new()
        {
            RentalId = entity.RentalId,
            BookCopyId = entity.BookCopyId,
            BookCopy = entity.BookCopy is null ? null : ToDto(entity.BookCopy),
            RentalDate = entity.RentalDate,
            EndDate = entity.EndDate,
            ReturnDate = entity.ReturnDate,
            ExtendedOn = entity.ExtendedOn
        };

        internal static RentalRequestDto ToDto(RentalRequest entity) => new()
        {
            Id = entity.Id,
            SubscriberId = entity.SubscriberId,
            SubscriberName = entity.Subscriber?.FullName ?? string.Empty,
            ApplicationUserId = entity.Subscriber?.ApplicationUserId,
            BookId = entity.BookId,
            BookTitle = entity.Book?.Name ?? string.Empty,
            BookCopyId = entity.BookCopyId,
            BookCopyLabel = entity.BookCopy is null
                ? null
                : $"{entity.BookCopy.EditionNumber}/{entity.BookCopy.SerialNumber}",
            Status = ToDto(entity.Status),
            CreatedOn = entity.CreatedOn,
            ReviewedOn = entity.ReviewedOn,
            RejectionReason = entity.RejectionReason
        };

        internal static RentalRequestStatusDto ToDto(RentalRequestStatus status) => (RentalRequestStatusDto)(int)status;

        internal static RentalRequestStatus ToEntity(RentalRequestStatusDto status) => (RentalRequestStatus)(int)status;

        internal static LogCategoryDto ToDto(LogCategory entity) => new()
        {
            Id = entity.Id,
            Action = entity.Action,
            Date = entity.Date,
            UserId = entity.UserId,
            CategoryId = entity.CategoryId,
            category = entity.category is null ? null : ToDto(entity.category)
        };

        internal static NotificationDto ToDto(Notification entity) => new()
        {
            Id = entity.Id,
            Type = ToDto(entity.Type),
            DedupeKey = entity.DedupeKey,
            SubscriberName = entity.SubscriberName,
            BookTitle = entity.BookTitle,
            Days = entity.Days,
            Amount = entity.Amount,
            TargetUrl = entity.TargetUrl,
            CreatedOn = entity.CreatedOn,
            IsRead = entity.IsRead,
            ReadOn = entity.ReadOn
        };

        /// <summary>Subscriber name only; avoids a Subscriber -> Subscriptions -> Subscriber loop.</summary>
        private static SubscriberDto? ToSubscriberSummary(Subscriber? entity) => entity is null ? null : new SubscriberDto
        {
            Id = entity.Id,
            FirstName = entity.FirstName,
            LastName = entity.LastName
        };

        internal static NotificationType ToEntity(NotificationKindDto kind) => kind switch
        {
            NotificationKindDto.SubscriptionExpiring => NotificationType.SubscriptionExpiring,
            NotificationKindDto.RentalOverdue => NotificationType.RentalOverdue,
            NotificationKindDto.FineApplied => NotificationType.FineApplied,
            _ => NotificationType.SubscriptionExpiring
        };

        internal static NotificationKindDto ToDto(NotificationType type) => type switch
        {
            NotificationType.SubscriptionExpiring => NotificationKindDto.SubscriptionExpiring,
            NotificationType.RentalOverdue => NotificationKindDto.RentalOverdue,
            NotificationType.FineApplied => NotificationKindDto.FineApplied,
            _ => NotificationKindDto.SubscriptionExpiring
        };
    }
}
